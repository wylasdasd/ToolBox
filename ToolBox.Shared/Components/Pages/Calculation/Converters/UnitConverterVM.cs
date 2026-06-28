using Blazing.Mvvm.ComponentModel;
using ToolBox.Tools.Calculation;

namespace ToolBox.Components.Pages.Converters;

public partial class UnitConverterVM : ViewModelBase
{
    private string _categoryId = "storage-iec";
    private string _input = "1";
    private string _fromUnitId = "mib";
    private string? _errorMessage;

    private string _fileSize = "100";
    private string _fileSizeUnitId = "mb";
    private string _bandwidth = "100";
    private string _bandwidthUnitId = "mbps";
    private string _transferTime = string.Empty;

    public static IReadOnlyList<UnitCategory> Categories => UnitConvertService.Categories;

    public IReadOnlyList<UnitCategory> CategoryList => Categories;

    public string CategoryId
    {
        get => _categoryId;
        set
        {
            if (!SetProperty(ref _categoryId, value))
                return;
            var cat = UnitConvertService.GetCategory(value);
            if (cat.Units.All(u => u.Id != _fromUnitId))
                FromUnitId = cat.Units[0].Id;
            OnPropertyChanged(nameof(CurrentCategory));
            OnPropertyChanged(nameof(ConversionRows));
            OnPropertyChanged(nameof(NetworkHint));
        }
    }

    public UnitCategory CurrentCategory => UnitConvertService.GetCategory(CategoryId);

    public string Input
    {
        get => _input;
        set
        {
            if (SetProperty(ref _input, value))
            {
                OnPropertyChanged(nameof(ConversionRows));
                OnPropertyChanged(nameof(NetworkHint));
            }
        }
    }

    public string FromUnitId
    {
        get => _fromUnitId;
        set
        {
            if (SetProperty(ref _fromUnitId, value))
            {
                OnPropertyChanged(nameof(ConversionRows));
                OnPropertyChanged(nameof(NetworkHint));
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public IEnumerable<UnitConversionRow> ConversionRows
    {
        get
        {
            var result = UnitConvertService.Convert(Input, CategoryId, FromUnitId);
            ErrorMessage = result.ErrorMessage;
            return result.Rows;
        }
    }

    public string? NetworkHint =>
        UnitConvertService.Convert(Input, CategoryId, FromUnitId).NetworkHint;

    public IEnumerable<UnitDef> FileSizeUnits => UnitConvertService.FileSizeUnits;

    public IEnumerable<UnitDef> BandwidthUnits => UnitConvertService.BandwidthUnits;

    public string FileSize
    {
        get => _fileSize;
        set
        {
            if (SetProperty(ref _fileSize, value))
                RecalculateTransferTime();
        }
    }

    public string FileSizeUnitId
    {
        get => _fileSizeUnitId;
        set
        {
            if (SetProperty(ref _fileSizeUnitId, value))
                RecalculateTransferTime();
        }
    }

    public string Bandwidth
    {
        get => _bandwidth;
        set
        {
            if (SetProperty(ref _bandwidth, value))
                RecalculateTransferTime();
        }
    }

    public string BandwidthUnitId
    {
        get => _bandwidthUnitId;
        set
        {
            if (SetProperty(ref _bandwidthUnitId, value))
                RecalculateTransferTime();
        }
    }

    public string TransferTime
    {
        get => _transferTime;
        private set => SetProperty(ref _transferTime, value);
    }

    public void RecalculateTransferTime()
    {
        var result = UnitConvertService.CalculateTransferTime(FileSize, FileSizeUnitId, Bandwidth, BandwidthUnitId);
        TransferTime = result.Success ? result.Value ?? string.Empty : string.Empty;
    }

    public void ClearInput()
    {
        Input = string.Empty;
        ErrorMessage = null;
    }
}
