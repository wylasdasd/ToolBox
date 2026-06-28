using Blazing.Mvvm.ComponentModel;
using ToolBox.Tools.Network;

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

        var result = IpCalculatorService.Calculate(Input);
        if (!result.Success)
        {
            ErrorMessage = result.Error;
            return;
        }

        var value = result.Value!;
        Address = value.Address;
        IntegerValue = value.IntegerValue;
        HexValue = value.HexValue;
        SubnetMask = value.SubnetMask;
        WildcardMask = value.WildcardMask;
        NetworkAddress = value.NetworkAddress;
        BroadcastAddress = value.BroadcastAddress;
        FirstHost = value.FirstHost;
        LastHost = value.LastHost;
        HostCount = value.HostCount;
        Cidr = value.Cidr;
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
}
