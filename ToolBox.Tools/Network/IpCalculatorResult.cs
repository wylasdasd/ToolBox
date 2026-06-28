namespace ToolBox.Tools.Network;

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
