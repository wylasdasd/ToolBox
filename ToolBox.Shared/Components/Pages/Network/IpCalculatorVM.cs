using Blazing.Mvvm.ComponentModel;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ToolBox.Components.Pages.Network;

public sealed class IpCalculatorVM : ViewModelBase
{
    private string _input = "192.168.1.10/24";
    private string _address = string.Empty;
    private string _integerValue = string.Empty;
    private string _hexValue = string.Empty;
    private string _subnetMask = string.Empty;
    private string _wildcardMask = string.Empty;
    private string _networkAddress = string.Empty;
    private string _broadcastAddress = string.Empty;
    private string _firstHost = string.Empty;
    private string _lastHost = string.Empty;
    private string _hostCount = string.Empty;
    private string _cidr = string.Empty;
    private string? _errorMessage;

    public string Input
    {
        get => _input;
        set => SetProperty(ref _input, value);
    }

    public string Address
    {
        get => _address;
        private set => SetProperty(ref _address, value);
    }

    public string IntegerValue
    {
        get => _integerValue;
        private set => SetProperty(ref _integerValue, value);
    }

    public string HexValue
    {
        get => _hexValue;
        private set => SetProperty(ref _hexValue, value);
    }

    public string SubnetMask
    {
        get => _subnetMask;
        private set => SetProperty(ref _subnetMask, value);
    }

    public string WildcardMask
    {
        get => _wildcardMask;
        private set => SetProperty(ref _wildcardMask, value);
    }

    public string NetworkAddress
    {
        get => _networkAddress;
        private set => SetProperty(ref _networkAddress, value);
    }

    public string BroadcastAddress
    {
        get => _broadcastAddress;
        private set => SetProperty(ref _broadcastAddress, value);
    }

    public string FirstHost
    {
        get => _firstHost;
        private set => SetProperty(ref _firstHost, value);
    }

    public string LastHost
    {
        get => _lastHost;
        private set => SetProperty(ref _lastHost, value);
    }

    public string HostCount
    {
        get => _hostCount;
        private set => SetProperty(ref _hostCount, value);
    }

    public string Cidr
    {
        get => _cidr;
        private set => SetProperty(ref _cidr, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public void Calculate()
    {
        ErrorMessage = null;
        ClearResults();

        var text = (Input ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(text))
        {
            ErrorMessage = "请输入 IPv4 地址，如 192.168.1.10/24 或 192.168.1.10 255.255.255.0。";
            return;
        }

        try
        {
            if (!TryParseInput(text, out var ip, out var prefix))
                throw new FormatException("无法解析 IPv4 或子网掩码。");

            var mask = PrefixToMask(prefix);
            var wildcard = ~mask;
            var network = ip & mask;
            var broadcast = ip | wildcard;

            Address = UInt32ToIp(ip);
            IntegerValue = ip.ToString(CultureInfo.InvariantCulture);
            HexValue = "0x" + ip.ToString("X8", CultureInfo.InvariantCulture);
            SubnetMask = UInt32ToIp(mask);
            WildcardMask = UInt32ToIp(wildcard);
            NetworkAddress = UInt32ToIp(network);
            BroadcastAddress = UInt32ToIp(broadcast);
            Cidr = $"/{prefix}";

            var total = prefix >= 31 ? (uint)(1u << (32 - prefix)) : (uint)((1L << (32 - prefix)) - 2);
            if (prefix >= 31)
            {
                FirstHost = prefix == 32 ? Address : UInt32ToIp(network);
                LastHost = prefix == 32 ? Address : UInt32ToIp(broadcast);
                HostCount = prefix == 32 ? "1（单主机）" : "2（/31 点对点）";
            }
            else
            {
                FirstHost = UInt32ToIp(network + 1);
                LastHost = UInt32ToIp(broadcast - 1);
                HostCount = total.ToString(CultureInfo.InvariantCulture);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    public void Clear()
    {
        Input = string.Empty;
        ClearResults();
        ErrorMessage = null;
    }

    private void ClearResults()
    {
        Address = IntegerValue = HexValue = SubnetMask = WildcardMask = string.Empty;
        NetworkAddress = BroadcastAddress = FirstHost = LastHost = HostCount = Cidr = string.Empty;
    }

    private static bool TryParseInput(string text, out uint ip, out int prefix)
    {
        ip = 0;
        prefix = 0;

        if (text.Contains('/'))
        {
            var parts = text.Split('/', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || !TryParseIp(parts[0], out ip) || !int.TryParse(parts[1], out prefix))
                return false;
            return prefix is >= 0 and <= 32;
        }

        var tokens = text.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 1)
        {
            if (!TryParseIp(tokens[0], out ip)) return false;
            prefix = 32;
            return true;
        }

        if (tokens.Length != 2 || !TryParseIp(tokens[0], out ip))
            return false;

        if (tokens[1].StartsWith('/'))
        {
            return int.TryParse(tokens[1][1..], out prefix) && prefix is >= 0 and <= 32;
        }

        if (!TryParseIp(tokens[1], out var maskIp))
            return false;

        prefix = MaskToPrefix(maskIp);
        return prefix >= 0;
    }

    private static bool TryParseIp(string text, out uint value)
    {
        value = 0;
        if (!IPAddress.TryParse(text, out var addr) || addr.AddressFamily != AddressFamily.InterNetwork)
            return false;

        var bytes = addr.GetAddressBytes();
        value = ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
        return true;
    }

    private static uint PrefixToMask(int prefix) =>
        prefix == 0 ? 0u : uint.MaxValue << (32 - prefix);

    private static int MaskToPrefix(uint mask)
    {
        var seenZero = false;
        var prefix = 0;
        for (var i = 31; i >= 0; i--)
        {
            var bit = (mask >> i) & 1;
            if (bit == 1)
            {
                if (seenZero) throw new FormatException("子网掩码无效。");
                prefix++;
            }
            else seenZero = true;
        }
        return prefix;
    }

    private static string UInt32ToIp(uint value)
    {
        var bytes = new byte[]
        {
            (byte)(value >> 24),
            (byte)(value >> 16),
            (byte)(value >> 8),
            (byte)value
        };
        return new IPAddress(bytes).ToString();
    }
}
