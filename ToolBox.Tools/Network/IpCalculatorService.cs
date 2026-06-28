using System.Globalization;
using System.Net;
using System.Net.Sockets;
using ToolBox.Tools.Common;

namespace ToolBox.Tools.Network;

public static class IpCalculatorService
{
    public static ToolResult<IpCalculatorResult> Calculate(string? input)
    {
        var text = (input ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(text))
            return ToolResult<IpCalculatorResult>.Fail("请输入 IPv4 地址，如 192.168.1.10/24 或 192.168.1.10 255.255.255.0。");

        try
        {
            if (!TryParseInput(text, out var ip, out var prefix))
                return ToolResult<IpCalculatorResult>.Fail("无法解析 IPv4 或子网掩码。");

            var mask = PrefixToMask(prefix);
            var wildcard = ~mask;
            var network = ip & mask;
            var broadcast = ip | wildcard;

            var address = UInt32ToIp(ip);
            var integerValue = ip.ToString(CultureInfo.InvariantCulture);
            var hexValue = "0x" + ip.ToString("X8", CultureInfo.InvariantCulture);
            var subnetMask = UInt32ToIp(mask);
            var wildcardMask = UInt32ToIp(wildcard);
            var networkAddress = UInt32ToIp(network);
            var broadcastAddress = UInt32ToIp(broadcast);
            var cidr = $"/{prefix}";

            string firstHost;
            string lastHost;
            string hostCount;
            var total = prefix >= 31 ? (uint)(1u << (32 - prefix)) : (uint)((1L << (32 - prefix)) - 2);
            if (prefix >= 31)
            {
                firstHost = prefix == 32 ? address : UInt32ToIp(network);
                lastHost = prefix == 32 ? address : UInt32ToIp(broadcast);
                hostCount = prefix == 32 ? "1（单主机）" : "2（/31 点对点）";
            }
            else
            {
                firstHost = UInt32ToIp(network + 1);
                lastHost = UInt32ToIp(broadcast - 1);
                hostCount = total.ToString(CultureInfo.InvariantCulture);
            }

            return ToolResult<IpCalculatorResult>.Ok(new IpCalculatorResult(
                address, integerValue, hexValue, subnetMask, wildcardMask,
                networkAddress, broadcastAddress, firstHost, lastHost, hostCount, cidr));
        }
        catch (Exception ex)
        {
            return ToolResult<IpCalculatorResult>.Fail(ex.Message);
        }
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
            return int.TryParse(tokens[1][1..], out prefix) && prefix is >= 0 and <= 32;

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
