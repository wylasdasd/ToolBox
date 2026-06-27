using Blazing.Mvvm.ComponentModel;
using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace ToolBox.Components.Pages.Network;

public sealed class WebSocketToolVM : ViewModelBase, IDisposable
{
    public WebSocketClientVM Client { get; } = new();
    public WebSocketServerVM Server { get; } = new();

    public new void Dispose()
    {
        Client.Dispose();
        Server.Dispose();
        base.Dispose();
    }
}

public abstract class WebSocketSessionVMBase : ViewModelBase, IDisposable
{
    private string _messageToSend = string.Empty;
    private string _receivedLog = string.Empty;

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

    protected void AddLog(string type, string content)
    {
        ReceivedLog += $"[{DateTime.Now:HH:mm:ss}] [{type}] {content}{Environment.NewLine}";
    }

    public abstract Task SendMessage();

    public new void Dispose()
    {
        base.Dispose();
    }
}

public sealed class WebSocketClientVM : WebSocketSessionVMBase
{
    private string _url = "ws://127.0.0.1:8181/ws/";
    private bool _isConnected;
    private string _headersInput = string.Empty;
    private string _authMode = "None";
    private string _authHeaderName = "X-API-Key";
    private string _authToken = "test-token";
    private string _queryTokenName = "access_token";
    private bool _ignoreServerCertificateErrors;

    private ClientWebSocket? _client;
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;

    public IReadOnlyList<string> AuthModes { get; } = ["None", "Bearer", "ApiKeyHeader", "QueryToken"];

    public string Url
    {
        get => _url;
        set => SetProperty(ref _url, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        private set
        {
            if (SetProperty(ref _isConnected, value))
            {
                OnPropertyChanged(nameof(ConnectionStatusText));
                OnPropertyChanged(nameof(ActionName));
                OnPropertyChanged(nameof(CanEditSettings));
            }
        }
    }

    public string HeadersInput
    {
        get => _headersInput;
        set => SetProperty(ref _headersInput, value);
    }

    public string AuthMode
    {
        get => _authMode;
        set => SetProperty(ref _authMode, value);
    }

    public string AuthHeaderName
    {
        get => _authHeaderName;
        set => SetProperty(ref _authHeaderName, value);
    }

    public string AuthToken
    {
        get => _authToken;
        set => SetProperty(ref _authToken, value);
    }

    public string QueryTokenName
    {
        get => _queryTokenName;
        set => SetProperty(ref _queryTokenName, value);
    }

    public bool IgnoreServerCertificateErrors
    {
        get => _ignoreServerCertificateErrors;
        set => SetProperty(ref _ignoreServerCertificateErrors, value);
    }

    public string ConnectionStatusText => IsConnected ? "Connected" : "Disconnected";
    public string ActionName => IsConnected ? "Disconnect" : "Connect";
    public bool CanEditSettings => !IsConnected;

    public async Task ToggleConnection()
    {
        if (IsConnected)
        {
            await Disconnect();
        }
        else
        {
            await Connect();
        }
    }

    private async Task Connect()
    {
        try
        {
            var uri = BuildTargetUri();
            _client = new ClientWebSocket();
            _cts = new CancellationTokenSource();

            if (IgnoreServerCertificateErrors)
            {
                _client.Options.RemoteCertificateValidationCallback = (_, _, _, _) => true;
            }

            AddAuthToClientOptions(_client.Options);
            AddCustomHeaders(_client.Options);

            AddLog("System", $"Connecting to {uri} ...");
            await _client.ConnectAsync(uri, _cts.Token);
            IsConnected = true;
            AddLog("System", "Connected.");

            _receiveTask = ReceiveLoop(_cts.Token);
        }
        catch (Exception ex)
        {
            AddLog("Error", $"Connect failed: {ex.Message}");
            await Disconnect();
        }
    }

    private async Task Disconnect()
    {
        try
        {
            _cts?.Cancel();

            if (_client != null && (_client.State == WebSocketState.Open || _client.State == WebSocketState.CloseReceived))
            {
                await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnect", CancellationToken.None);
            }
        }
        catch
        {
            // Ignore close exceptions.
        }
        finally
        {
            _client?.Dispose();
            _client = null;
            _cts?.Dispose();
            _cts = null;
            IsConnected = false;
            AddLog("System", "Disconnected.");
        }
    }

    public override async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(MessageToSend))
        {
            return;
        }

        if (_client == null || _client.State != WebSocketState.Open)
        {
            AddLog("Error", "Client is not connected.");
            return;
        }

        try
        {
            var payload = Encoding.UTF8.GetBytes(MessageToSend);
            await _client.SendAsync(payload, WebSocketMessageType.Text, true, CancellationToken.None);
            AddLog("Sent", MessageToSend);
            MessageToSend = string.Empty;
        }
        catch (Exception ex)
        {
            AddLog("Error", $"Send failed: {ex.Message}");
        }
    }

    private async Task ReceiveLoop(CancellationToken token)
    {
        if (_client == null)
        {
            return;
        }

        var buffer = new byte[4096];

        try
        {
            while (!token.IsCancellationRequested && _client.State == WebSocketState.Open)
            {
                using var ms = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await _client.ReceiveAsync(buffer, token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        AddLog("System", $"Server closed connection: {_client.CloseStatus} {_client.CloseStatusDescription}");
                        await Disconnect();
                        return;
                    }

                    ms.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                var message = Encoding.UTF8.GetString(ms.ToArray());
                AddLog("Received", message);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            AddLog("Error", $"Receive failed: {ex.Message}");
            await Disconnect();
        }
    }

    private Uri BuildTargetUri()
    {
        var raw = Url?.Trim() ?? string.Empty;
        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("URL 不合法。示例：ws://127.0.0.1:8181/ws/");
        }

        if (!string.Equals(uri.Scheme, "ws", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Scheme, "wss", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("URL 协议必须是 ws 或 wss。");
        }

        if (AuthMode == "QueryToken")
        {
            var tokenName = (QueryTokenName ?? string.Empty).Trim();
            var token = AuthToken ?? string.Empty;
            if (string.IsNullOrWhiteSpace(tokenName))
            {
                throw new InvalidOperationException("Query 鉴权模式下 Query Name 不能为空。");
            }

            var baseUri = uri.GetLeftPart(UriPartial.Path);
            var existing = uri.Query;
            var separator = string.IsNullOrEmpty(existing) ? "?" : "&";
            var appended = $"{baseUri}{existing}{separator}{Uri.EscapeDataString(tokenName)}={Uri.EscapeDataString(token)}";
            return new Uri(appended);
        }

        return uri;
    }

    private void AddAuthToClientOptions(ClientWebSocketOptions options)
    {
        if (AuthMode == "None")
        {
            return;
        }

        if (AuthMode == "Bearer")
        {
            options.SetRequestHeader("Authorization", $"Bearer {AuthToken}");
            return;
        }

        if (AuthMode == "ApiKeyHeader")
        {
            options.SetRequestHeader(AuthHeaderName, AuthToken);
        }
    }

    private void AddCustomHeaders(ClientWebSocketOptions options)
    {
        if (string.IsNullOrWhiteSpace(HeadersInput))
        {
            return;
        }

        var lines = HeadersInput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var line in lines)
        {
            var idx = line.IndexOf(':');
            if (idx <= 0)
            {
                continue;
            }

            var key = line[..idx].Trim();
            var value = line[(idx + 1)..].Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            options.SetRequestHeader(key, value);
        }
    }

    public new void Dispose()
    {
        _ = Disconnect();
        base.Dispose();
    }
}

public sealed class WebSocketServerVM : WebSocketSessionVMBase
{
    private string _prefix = "http://127.0.0.1:8181/ws/";
    private bool _isRunning;
    private int _clientCount;
    private string _authMode = "None";
    private string _authHeaderName = "X-API-Key";
    private string _authToken = "test-token";
    private string _queryTokenName = "access_token";

    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private readonly object _clientLock = new();
    private readonly List<ServerSocketClient> _clients = [];

    public IReadOnlyList<string> AuthModes { get; } = ["None", "Bearer", "ApiKeyHeader", "QueryToken"];

    public string Prefix
    {
        get => _prefix;
        set => SetProperty(ref _prefix, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
            {
                OnPropertyChanged(nameof(ConnectionStatusText));
                OnPropertyChanged(nameof(ActionName));
                OnPropertyChanged(nameof(CanEditSettings));
            }
        }
    }

    public int ClientCount
    {
        get => _clientCount;
        private set => SetProperty(ref _clientCount, value);
    }

    public string AuthMode
    {
        get => _authMode;
        set => SetProperty(ref _authMode, value);
    }

    public string AuthHeaderName
    {
        get => _authHeaderName;
        set => SetProperty(ref _authHeaderName, value);
    }

    public string AuthToken
    {
        get => _authToken;
        set => SetProperty(ref _authToken, value);
    }

    public string QueryTokenName
    {
        get => _queryTokenName;
        set => SetProperty(ref _queryTokenName, value);
    }

    public string ConnectionStatusText => IsRunning ? "Listening" : "Stopped";
    public string ActionName => IsRunning ? "Stop Listening" : "Start Listening";
    public bool CanEditSettings => !IsRunning;

    public async Task ToggleServer()
    {
        if (IsRunning)
        {
            await StopServer();
        }
        else
        {
            await StartServer();
        }
    }

    private async Task StartServer()
    {
        try
        {
            if (!Prefix.EndsWith('/'))
            {
                throw new InvalidOperationException("Prefix 必须以 / 结尾。");
            }

            if (!Uri.TryCreate(Prefix, UriKind.Absolute, out var uri))
            {
                throw new InvalidOperationException("Prefix 不合法。示例：http://127.0.0.1:8181/ws/");
            }

            if (!string.Equals(uri.Scheme, "http", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Prefix 仅支持 http 或 https。");
            }

            _listener = new HttpListener();
            _listener.Prefixes.Add(Prefix);
            _listener.Start();
            _cts = new CancellationTokenSource();
            IsRunning = true;

            AddLog("System", $"Server listening on {Prefix}");
            if (uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                AddLog("System", "当前为 https（对应客户端请使用 wss）。若启动失败请先绑定证书。");
            }

            _ = AcceptLoop(_cts.Token);
        }
        catch (Exception ex)
        {
            AddLog("Error", $"Start failed: {ex.Message}");
            await StopServer();
        }
    }

    private async Task StopServer()
    {
        try
        {
            _cts?.Cancel();
            _listener?.Stop();
            _listener?.Close();
        }
        catch
        {
            // Ignore stop exceptions.
        }

        _listener = null;
        _cts?.Dispose();
        _cts = null;

        List<ServerSocketClient> snapshot;
        lock (_clientLock)
        {
            snapshot = _clients.ToList();
            _clients.Clear();
            ClientCount = 0;
        }

        foreach (var item in snapshot)
        {
            try
            {
                if (item.Socket.State == WebSocketState.Open || item.Socket.State == WebSocketState.CloseReceived)
                {
                    await item.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server stopping", CancellationToken.None);
                }
            }
            catch
            {
            }
            finally
            {
                item.Socket.Dispose();
            }
        }

        IsRunning = false;
        AddLog("System", "Server stopped.");
    }

    public override async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(MessageToSend))
        {
            return;
        }

        if (!IsRunning)
        {
            AddLog("Error", "Server not running.");
            return;
        }

        ServerSocketClient[] targets;
        lock (_clientLock)
        {
            targets = _clients.Where(c => c.Socket.State == WebSocketState.Open).ToArray();
        }

        if (targets.Length == 0)
        {
            AddLog("Info", "No connected clients.");
            return;
        }

        var payload = Encoding.UTF8.GetBytes(MessageToSend);
        foreach (var client in targets)
        {
            try
            {
                await client.Socket.SendAsync(payload, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                AddLog("Error", $"Send to {client.Id} failed: {ex.Message}");
            }
        }

        AddLog("Sent", $"Broadcast to {targets.Length} client(s): {MessageToSend}");
        MessageToSend = string.Empty;
    }

    private async Task AcceptLoop(CancellationToken token)
    {
        if (_listener == null)
        {
            return;
        }

        while (!token.IsCancellationRequested && _listener.IsListening)
        {
            HttpListenerContext? context = null;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (HttpListenerException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                AddLog("Error", $"Accept failed: {ex.Message}");
                continue;
            }

            _ = Task.Run(() => HandleContextAsync(context, token), token);
        }
    }

    private async Task HandleContextAsync(HttpListenerContext context, CancellationToken token)
    {
        try
        {
            if (!context.Request.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                await WriteTextResponse(context.Response, "Only WebSocket upgrade requests are allowed.");
                return;
            }

            if (!IsAuthorized(context.Request))
            {
                context.Response.StatusCode = 401;
                if (AuthMode == "Bearer")
                {
                    context.Response.AddHeader("WWW-Authenticate", "Bearer");
                }

                await WriteTextResponse(context.Response, "Unauthorized");
                AddLog("Auth", $"Rejected: {context.Request.RemoteEndPoint} ({AuthMode})");
                return;
            }

            var wsContext = await context.AcceptWebSocketAsync(null);
            var socket = wsContext.WebSocket;
            var id = Guid.NewGuid().ToString("N")[..8];
            var remote = context.Request.RemoteEndPoint?.ToString() ?? "unknown";

            var client = new ServerSocketClient(id, socket, remote);
            lock (_clientLock)
            {
                _clients.Add(client);
                ClientCount = _clients.Count;
            }

            AddLog("System", $"Client connected: {id} ({remote})");
            await ReceiveLoop(client, token);
        }
        catch (Exception ex)
        {
            AddLog("Error", $"Handle request failed: {ex.Message}");
        }
    }

    private async Task ReceiveLoop(ServerSocketClient client, CancellationToken token)
    {
        var socket = client.Socket;
        var buffer = new byte[4096];

        try
        {
            while (!token.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                using var ms = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await socket.ReceiveAsync(buffer, token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None);
                        return;
                    }

                    ms.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                var message = Encoding.UTF8.GetString(ms.ToArray());
                AddLog("Received", $"[{client.Id}] {message}");
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            AddLog("Error", $"Client {client.Id} receive failed: {ex.Message}");
        }
        finally
        {
            RemoveClient(client);
        }
    }

    private void RemoveClient(ServerSocketClient client)
    {
        lock (_clientLock)
        {
            _clients.Remove(client);
            ClientCount = _clients.Count;
        }

        try
        {
            client.Socket.Dispose();
        }
        catch
        {
        }

        AddLog("System", $"Client disconnected: {client.Id} ({client.RemoteEndPoint})");
    }

    private bool IsAuthorized(HttpListenerRequest request)
    {
        if (AuthMode == "None")
        {
            return true;
        }

        if (AuthMode == "Bearer")
        {
            var header = request.Headers["Authorization"];
            return string.Equals(header, $"Bearer {AuthToken}", StringComparison.Ordinal);
        }

        if (AuthMode == "ApiKeyHeader")
        {
            var value = request.Headers[AuthHeaderName];
            return string.Equals(value, AuthToken, StringComparison.Ordinal);
        }

        if (AuthMode == "QueryToken")
        {
            var key = (QueryTokenName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            var token = request.QueryString[key];
            return string.Equals(token, AuthToken, StringComparison.Ordinal);
        }

        return false;
    }

    private static async Task WriteTextResponse(HttpListenerResponse response, string content)
    {
        try
        {
            response.ContentType = "text/plain; charset=utf-8";
            var payload = Encoding.UTF8.GetBytes(content);
            response.ContentLength64 = payload.Length;
            await response.OutputStream.WriteAsync(payload);
        }
        finally
        {
            response.Close();
        }
    }

    public new void Dispose()
    {
        _ = StopServer();
        base.Dispose();
    }

    private sealed record ServerSocketClient(string Id, WebSocket Socket, string RemoteEndPoint);
}
