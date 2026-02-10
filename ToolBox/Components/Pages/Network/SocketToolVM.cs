using Blazing.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ToolBox.Components.Pages.Network;

public class SocketToolVM : ViewModelBase, IDisposable
{
    public SocketClientVM Client { get; } = new();
    public SocketServerVM Server { get; } = new();

    public new void Dispose()
    {
        Client.Dispose();
        Server.Dispose();
        base.Dispose();
    }
}

public abstract class SocketSessionVM : ViewModelBase, IDisposable
{
    protected string _protocol = "TCP";
    protected string _ipAddress = "127.0.0.1";
    protected int _port = 8080;
    protected bool _isConnected;
    protected string _messageToSend = "";
    protected string _receivedLog = "";
    protected CancellationTokenSource? _cts;

    public string Protocol
    {
        get => _protocol;
        set => SetProperty(ref _protocol, value);
    }

    public string IpAddress
    {
        get => _ipAddress;
        set => SetProperty(ref _ipAddress, value);
    }

    public int Port
    {
        get => _port;
        set => SetProperty(ref _port, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            if (SetProperty(ref _isConnected, value))
            {
                OnPropertyChanged(nameof(ConnectionStatusText));
                OnPropertyChanged(nameof(StatusColor));
                OnPropertyChanged(nameof(ActionName));
                OnPropertyChanged(nameof(CanEditSettings));
            }
        }
    }

    public string MessageToSend
    {
        get => _messageToSend;
        set => SetProperty(ref _messageToSend, value);
    }

    public string ReceivedLog
    {
        get => _receivedLog;
        set => SetProperty(ref _receivedLog, value);
    }

    public ObservableCollection<LogEntry> Logs { get; } = new();

    public virtual string ConnectionStatusText => IsConnected ? "Connected" : "Disconnected";
    public virtual string StatusColor => IsConnected ? "Color.Success" : "Color.Error";
    public virtual string ActionName => IsConnected ? "Stop" : "Start";
    public bool CanEditSettings => !IsConnected;

    public async Task ToggleConnection()
    {
        if (IsConnected) await Stop();
        else await Start();
    }

    protected abstract Task Start();
    protected abstract Task Stop();
    public abstract Task SendMessage();

    protected void AddLog(string type, string content)
    {
        var entry = new LogEntry { Timestamp = DateTime.Now, Type = type, Message = content };
        // We use MainThread invoke if strictly necessary, but for Blazor binding often works directly.
        // If issues arise with background threads updating UI, we might need dispatching.
        Logs.Add(entry);
        ReceivedLog += $"[{entry.Timestamp:HH:mm:ss}] [{type}] {content}{Environment.NewLine}";
    }

    public new void Dispose()
    {
        _cts?.Cancel();
        base.Dispose();
    }
}

public class SocketClientVM : SocketSessionVM
{
    private TcpClient? _tcpClient;
    private NetworkStream? _tcpStream;
    private UdpClient? _udpClient;

    public override string ActionName => IsConnected ? "Disconnect" : "Connect";

    protected override async Task Start()
    {
        try
        {
            _cts = new CancellationTokenSource();
            AddLog("System", $"Connecting to {IpAddress}:{Port} via {Protocol}...");

            if (Protocol == "TCP")
            {
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(IpAddress, Port);
                _tcpStream = _tcpClient.GetStream();
                _ = ReceiveLoopTcp(_cts.Token);
            }
            else // UDP
            {
                _udpClient = new UdpClient();
                _udpClient.Connect(IpAddress, Port);
                _ = ReceiveLoopUdp(_cts.Token);
            }

            IsConnected = true;
            AddLog("System", "Connected.");
        }
        catch (Exception ex)
        {
            AddLog("Error", $"Connection failed: {ex.Message}");
            await Stop();
        }
    }

    protected override async Task Stop()
    {
        _cts?.Cancel();
        _tcpClient?.Close();
        _udpClient?.Close();
        _tcpClient = null;
        _tcpStream = null;
        _udpClient = null;
        IsConnected = false;
        AddLog("System", "Disconnected.");
    }

    public override async Task SendMessage()
    {
        if (string.IsNullOrEmpty(MessageToSend)) return;
        if (!IsConnected && Protocol == "TCP")
        {
             AddLog("Error", "Not connected.");
             return;
        }

        try
        {
            byte[] data = Encoding.UTF8.GetBytes(MessageToSend);
            if (Protocol == "TCP" && _tcpStream != null)
            {
                await _tcpStream.WriteAsync(data);
                AddLog("Sent", MessageToSend);
            }
            else if (Protocol == "UDP" && _udpClient != null)
            {
                await _udpClient.SendAsync(data, data.Length);
                AddLog("Sent", MessageToSend);
            }
            MessageToSend = "";
        }
        catch (Exception ex)
        {
            AddLog("Error", $"Send failed: {ex.Message}");
            IsConnected = false;
        }
    }

    private async Task ReceiveLoopTcp(CancellationToken token)
    {
        try
        {
            byte[] buffer = new byte[4096];
            while (!token.IsCancellationRequested && _tcpStream != null)
            {
                int bytesRead = await _tcpStream.ReadAsync(buffer, token);
                if (bytesRead == 0) break; 
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                AddLog("Received", message);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { if (!token.IsCancellationRequested) AddLog("Error", ex.Message); }
        finally 
        { 
            if (IsConnected) 
            {
                IsConnected = false;
                AddLog("System", "Connection closed."); 
            }
        }
    }

    private async Task ReceiveLoopUdp(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested && _udpClient != null)
            {
                var result = await _udpClient.ReceiveAsync(token);
                string message = Encoding.UTF8.GetString(result.Buffer);
                AddLog("Received", message);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { if (!token.IsCancellationRequested) AddLog("Error", ex.Message); }
    }
}

public class SocketServerVM : SocketSessionVM
{
    private TcpListener? _tcpListener;
    private List<TcpClient> _clients = new();
    private UdpClient? _udpServer;
    private IPEndPoint? _lastUdpClient;

    public SocketServerVM()
    {
        IpAddress = "0.0.0.0"; // Default bind
    }

    public override string ActionName => IsConnected ? "Stop Listening" : "Start Listening";
    public override string ConnectionStatusText => IsConnected ? "Listening" : "Stopped";

    protected override async Task Start()
    {
        try
        {
            _cts = new CancellationTokenSource();
            var localIp = IPAddress.Parse(IpAddress);
            AddLog("System", $"Starting Server on {IpAddress}:{Port} via {Protocol}...");

            if (Protocol == "TCP")
            {
                _tcpListener = new TcpListener(localIp, Port);
                _tcpListener.Start();
                _ = AcceptLoopTcp(_cts.Token);
            }
            else // UDP
            {
                _udpServer = new UdpClient(new IPEndPoint(localIp, Port));
                _ = ReceiveLoopUdp(_cts.Token);
            }

            IsConnected = true;
            AddLog("System", "Server Started.");
        }
        catch (Exception ex)
        {
            AddLog("Error", $"Start failed: {ex.Message}");
            await Stop();
        }
    }

    protected override async Task Stop()
    {
        _cts?.Cancel();
        _tcpListener?.Stop();
        _tcpListener = null;
        
        lock(_clients)
        {
            foreach(var c in _clients) try { c.Close(); } catch {}
            _clients.Clear();
        }

        _udpServer?.Close();
        _udpServer = null;
        _lastUdpClient = null;

        IsConnected = false;
        AddLog("System", "Server Stopped.");
    }

    public override async Task SendMessage()
    {
        if (string.IsNullOrEmpty(MessageToSend)) return;
        if (!IsConnected)
        {
            AddLog("Error", "Server not running.");
            return;
        }

        try
        {
            byte[] data = Encoding.UTF8.GetBytes(MessageToSend);
            if (Protocol == "TCP")
            {
                TcpClient[] currentClients;
                lock (_clients) currentClients = _clients.ToArray();

                if (currentClients.Length == 0)
                {
                    AddLog("Info", "No connected clients to broadcast to.");
                    return;
                }

                foreach (var client in currentClients)
                {
                    try { await client.GetStream().WriteAsync(data); } catch { }
                }
                AddLog("Sent", $"Broadcast: {MessageToSend}");
            }
            else // UDP
            {
                if (_udpServer != null && _lastUdpClient != null)
                {
                    await _udpServer.SendAsync(data, data.Length, _lastUdpClient);
                    AddLog("Sent", $"To {_lastUdpClient}: {MessageToSend}");
                }
                else
                {
                    AddLog("Error", "No UDP client has contacted us yet to reply to.");
                }
            }
            MessageToSend = "";
        }
        catch (Exception ex)
        {
            AddLog("Error", $"Send failed: {ex.Message}");
        }
    }

    private async Task AcceptLoopTcp(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested && _tcpListener != null)
            {
                var client = await _tcpListener.AcceptTcpClientAsync(token);
                lock (_clients) _clients.Add(client);
                AddLog("System", $"Client connected: {client.Client.RemoteEndPoint}");
                _ = ReceiveLoopTcpClient(client, token);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { if (!token.IsCancellationRequested) AddLog("Error", $"Accept error: {ex.Message}"); }
    }

    private async Task ReceiveLoopTcpClient(TcpClient client, CancellationToken token)
    {
        try
        {
            using var stream = client.GetStream();
            byte[] buffer = new byte[4096];
            while (!token.IsCancellationRequested && client.Connected)
            {
                int bytesRead = await stream.ReadAsync(buffer, token);
                if (bytesRead == 0) break;
                string msg = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                AddLog("Received", $"[{client.Client.RemoteEndPoint}] {msg}");
            }
        }
        catch { }
        finally
        {
            lock (_clients) _clients.Remove(client);
            try 
            { 
                AddLog("System", $"Client disconnected: {client.Client.RemoteEndPoint}");
                client.Close(); 
            } catch {}
        }
    }

    private async Task ReceiveLoopUdp(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested && _udpServer != null)
            {
                var result = await _udpServer.ReceiveAsync(token);
                _lastUdpClient = result.RemoteEndPoint;
                string msg = Encoding.UTF8.GetString(result.Buffer);
                AddLog("Received", $"[{result.RemoteEndPoint}] {msg}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { if (!token.IsCancellationRequested) AddLog("Error", $"UDP Receive error: {ex.Message}"); }
    }
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Type { get; set; } = "";
    public string Message { get; set; } = "";
}
