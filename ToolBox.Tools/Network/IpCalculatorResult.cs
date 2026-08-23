namespace ToolBox.Tools.Network;

/// <summary>
/// IpCalculatorResult：用于表示和处理相关的。
/// </summary>
public sealed record IpCalculatorResult(
    string Address,
    string IntegerValue,
    string HexValue,
    string SubnetMask,
    string WildcardMask,
    string NetworkAddress,
    string BroadcastAddress,
    string FirstHost,
    string LastHost,
    string HostCount,
    string Cidr);
