using Blazing.Mvvm.ComponentModel;
using ToolBox.Tools.Calculation;

namespace ToolBox.Components.Pages.Calculation;

public partial class StructLayoutVisualizerVM : ViewModelBase
{
    private string _structDefinition = """
        #pragma pack(push, 2)

        alignas(8) struct PacketHeader
        {
            // struct + const/volatile + 位域（含 :0）
            const uint16_t version : 4;
            volatile uint16_t type : 6;
            uint16_t flags : 6;
            uint16_t : 0; // 强制到下一个对齐单元

            // union（重叠存储）
            union
            {
                uint32_t raw;
                struct
                {
                    uint32_t length : 12;
                    uint32_t opcode : 8;
                    uint32_t reserved : 12;
                } bits;
                uint8_t bytes[4];
            } u;

            // class（作为成员类型）
            class Meta
            {
            public:
                uint8_t tag;
                uint8_t level;
            };

            Meta meta;

            // 数组
            uint8_t payload[16];

            // 指针 + 关键字
            const char* namePtr;
            volatile uint32_t* dataPtr;

            // static 成员（不计入对象内存布局，放这里用于语法覆盖）
            static uint32_t globalCounter;
        };

        #pragma pack(pop)
        """;
    private int _pack;
    private string _layoutKind = "struct";
    private int _rawSize;
    private int _totalSize;
    private int _structAlignment = 1;
    private string? _errorMessage;
    private List<FieldLayoutItem> _fields = [];
    private List<ByteRowItem> _byteRows = [];

    public string StructDefinition
    {
        get => _structDefinition;
        set => SetProperty(ref _structDefinition, value);
    }

    public int Pack
    {
        get => _pack;
        set
        {
            var normalized = value < 0 ? 0 : value;
            SetProperty(ref _pack, normalized);
        }
    }

    public string LayoutKindText
    {
        get => _layoutKind;
        private set => SetProperty(ref _layoutKind, value);
    }

    public int RawSize
    {
        get => _rawSize;
        private set => SetProperty(ref _rawSize, value);
    }

    public int TotalSize
    {
        get => _totalSize;
        private set => SetProperty(ref _totalSize, value);
    }

    public int StructAlignment
    {
        get => _structAlignment;
        private set => SetProperty(ref _structAlignment, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public IReadOnlyList<FieldLayoutItem> Fields => _fields;

    public IReadOnlyList<ByteRowItem> ByteRows => _byteRows;

    public StructLayoutVisualizerVM()
    {
        Calculate();
    }

    public void Calculate()
    {
        var result = StructLayoutService.Calculate(StructDefinition, Pack);
        if (!result.Success)
        {
            _fields = [];
            _byteRows = [];
            RawSize = 0;
            TotalSize = 0;
            StructAlignment = 1;
            ErrorMessage = result.Error;
            OnPropertyChanged(nameof(Fields));
            OnPropertyChanged(nameof(ByteRows));
            return;
        }

        var value = result.Value!;
        LayoutKindText = value.LayoutKindText;
        _fields = value.Fields.ToList();
        _byteRows = value.ByteRows.ToList();
        RawSize = value.RawSize;
        TotalSize = value.TotalSize;
        StructAlignment = value.StructAlignment;
        ErrorMessage = null;
        OnPropertyChanged(nameof(Fields));
        OnPropertyChanged(nameof(ByteRows));
    }

    public double GetFieldLeftPercent(FieldLayoutItem field)
    {
        if (TotalSize <= 0)
            return 0;

        return Math.Clamp((double)field.StartBit / (TotalSize * 8) * 100d, 0d, 100d);
    }

    public double GetFieldWidthPercent(FieldLayoutItem field)
    {
        if (TotalSize <= 0)
            return 2d;

        var p = (double)field.LengthBits / (TotalSize * 8) * 100d;
        return Math.Clamp(Math.Max(p, 1.4d), 1.4d, 100d);
    }
}
