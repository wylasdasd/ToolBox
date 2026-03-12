using Blazing.Mvvm.ComponentModel;
using System.Text.RegularExpressions;

namespace ToolBox.Components.Pages.Calculation;

public partial class StructLayoutVisualizerVM : ViewModelBase
{
    private const int PointerSize = 8;

    private static readonly Dictionary<string, PrimitiveTypeInfo> TypeMap = BuildTypeMap();
    private static readonly string[] Palette =
    [
        "#2F6DF6", "#0E7490", "#F59E0B", "#16A34A", "#DC2626", "#7C3AED", "#0891B2", "#BE123C", "#1D4ED8", "#0F766E"
    ];

    private string _structDefinition = """
        union Packet
        {
            uint32_t raw;
            uint32_t flags : 12;
            uint8_t bytes[4];
        };
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
        ErrorMessage = null;
        try
        {
            var normalizedSource = SourceParser.NormalizeSource(StructDefinition);
            var kind = SourceParser.DetectLayoutKind(normalizedSource);
            LayoutKindText = kind == LayoutKind.Union ? "union" : "struct/class";

            var typeAlignOverride = SourceParser.DetectTopLevelAlignas(normalizedSource);
            var pragmaPack = SourceParser.DetectPragmaPack(normalizedSource);
            var fields = SourceParser.ParseDeclarations(normalizedSource, out var customTypes);
            if (fields.Count == 0)
            {
                throw new InvalidOperationException("未识别到字段，请输入 C/C++ 结构体或 union 成员声明。");
            }

            // 页面 Pack 优先；当页面 Pack=0 时自动读取源码里的 #pragma pack。
            var effectivePack = Pack > 0 ? Pack : pragmaPack;
            var result = kind == LayoutKind.Union
                ? BuildUnionLayout(fields, effectivePack, typeAlignOverride, customTypes)
                : BuildStructLayout(fields, effectivePack, typeAlignOverride, customTypes);

            _fields = result.Fields;
            _byteRows = result.Rows;
            RawSize = result.RawSize;
            TotalSize = result.TotalSize;
            StructAlignment = result.StructAlign;

            OnPropertyChanged(nameof(Fields));
            OnPropertyChanged(nameof(ByteRows));
        }
        catch (Exception ex)
        {
            _fields = [];
            _byteRows = [];
            RawSize = 0;
            TotalSize = 0;
            StructAlignment = 1;
            ErrorMessage = ex.Message;
            OnPropertyChanged(nameof(Fields));
            OnPropertyChanged(nameof(ByteRows));
        }
    }

    public double GetFieldLeftPercent(FieldLayoutItem field)
    {
        if (TotalSize <= 0)
        {
            return 0;
        }

        return Math.Clamp((double)field.StartBit / (TotalSize * 8) * 100d, 0d, 100d);
    }

    public double GetFieldWidthPercent(FieldLayoutItem field)
    {
        if (TotalSize <= 0)
        {
            return 2d;
        }

        // 给极窄位域一个最小显示宽度，避免视觉上看不到。
        var p = (double)field.LengthBits / (TotalSize * 8) * 100d;
        return Math.Clamp(Math.Max(p, 1.4d), 1.4d, 100d);
    }

    private static LayoutResult BuildStructLayout(
        List<FieldDecl> declarations,
        int pack,
        int? typeAlignOverride,
        Dictionary<string, PrimitiveTypeInfo>? customTypes = null)
    {
        var fields = new List<FieldLayoutItem>();
        var bitOwners = new Dictionary<int, List<BitOwner>>();
        var colorMap = new Dictionary<string, string>(StringComparer.Ordinal);

        var offset = 0;
        var maxAlign = 1;
        var fieldIndex = 0;
        BitContainer? activeBitContainer = null;

        void AddBits(int startBit, int bitCount, BitOwner owner)
        {
            for (var b = startBit; b < startBit + bitCount; b++)
            {
                if (!bitOwners.TryGetValue(b, out var owners))
                {
                    owners = [];
                    bitOwners[b] = owners;
                }

                owners.Add(owner);
            }
        }

        void AddPaddingBytes(int count, string name)
        {
            if (count <= 0)
            {
                return;
            }

            AddBits(offset * 8, count * 8, BitOwner.Padding(name, "#E2E8F0"));
            offset += count;
        }

        // 位域容器关闭时，剩余位被视作位域空洞并可视化。
        void CloseBitContainer()
        {
            if (activeBitContainer is null)
            {
                return;
            }

            var remain = activeBitContainer.CapacityBits - activeBitContainer.UsedBits;
            if (remain > 0)
            {
                AddBits(
                    activeBitContainer.StartByte * 8 + activeBitContainer.UsedBits,
                    remain,
                    BitOwner.Padding("位域空洞", "#CBD5E1"));
            }

            offset = activeBitContainer.StartByte + activeBitContainer.SizeBytes;
            activeBitContainer = null;
        }

        foreach (var decl in declarations)
        {
            var type = ResolveType(decl, customTypes);
            var baseAlign = ApplyPack(type.Align, pack);
            var align = ResolveFieldAlign(baseAlign, decl.AlignAs, pack);
            maxAlign = Math.Max(maxAlign, align);

            if (decl.BitWidth is null)
            {
                CloseBitContainer();
                var aligned = AlignUp(offset, align);
                AddPaddingBytes(aligned - offset, "对齐填充");

                var sizeBytes = type.Size * decl.ArrayCount;
                var color = GetColor(colorMap, decl.Name, fieldIndex++);
                var item = new FieldLayoutItem(
                    decl.Name,
                    decl.DisplayType,
                    offset,
                    sizeBytes,
                    null,
                    null,
                    align,
                    color,
                    offset * 8,
                    sizeBytes * 8,
                    false,
                    false);
                fields.Add(item);
                AddBits(item.StartBit, item.LengthBits, BitOwner.Field(item));
                offset += sizeBytes;
                continue;
            }

            if (!type.AllowBitField)
            {
                throw new InvalidOperationException($"{decl.Name} 的类型不支持位域: {decl.BaseType}");
            }

            var width = decl.BitWidth.Value;
            var limit = type.Size * 8;
            if (width < 0 || width > limit)
            {
                throw new InvalidOperationException($"{decl.Name} 位域宽度非法（0..{limit}）。");
            }

            // 匿名 :0 强制切到新的对齐边界。
            if (width == 0)
            {
                CloseBitContainer();
                var nextAligned = AlignUp(offset, align);
                AddPaddingBytes(nextAligned - offset, "位域强制对齐");
                continue;
            }

            var requiresNewContainer =
                activeBitContainer is null ||
                !string.Equals(activeBitContainer.TypeKey, decl.BaseType, StringComparison.Ordinal) ||
                activeBitContainer.UsedBits + width > activeBitContainer.CapacityBits;

            if (requiresNewContainer)
            {
                CloseBitContainer();
                var aligned = AlignUp(offset, align);
                AddPaddingBytes(aligned - offset, "位域对齐填充");
                activeBitContainer = new BitContainer(decl.BaseType, offset, type.Size);
            }

            var container = activeBitContainer!;
            var startBit = container.StartByte * 8 + container.UsedBits;
            var colorForBit = GetColor(colorMap, decl.Name, fieldIndex++);
            var bitField = new FieldLayoutItem(
                decl.Name,
                decl.DisplayType,
                startBit / 8,
                (int)Math.Ceiling(width / 8d),
                startBit % 8,
                width,
                align,
                colorForBit,
                startBit,
                width,
                true,
                false);

            fields.Add(bitField);
            AddBits(bitField.StartBit, bitField.LengthBits, BitOwner.Field(bitField));
            container.UsedBits += width;

            if (container.UsedBits == container.CapacityBits)
            {
                CloseBitContainer();
            }
        }

        CloseBitContainer();

        var rawSize = offset;
        var structAlign = ResolveStructAlign(maxAlign, typeAlignOverride, pack);
        var totalSize = AlignUp(rawSize, structAlign);
        if (totalSize > rawSize)
        {
            AddBits(rawSize * 8, (totalSize - rawSize) * 8, BitOwner.Padding("尾部填充", "#E2E8F0"));
        }

        return new LayoutResult(fields, BuildRows(totalSize, bitOwners), rawSize, totalSize, structAlign);
    }

    private static LayoutResult BuildUnionLayout(
        List<FieldDecl> declarations,
        int pack,
        int? typeAlignOverride,
        Dictionary<string, PrimitiveTypeInfo>? customTypes = null)
    {
        var fields = new List<FieldLayoutItem>();
        var bitOwners = new Dictionary<int, List<BitOwner>>();
        var colorMap = new Dictionary<string, string>(StringComparer.Ordinal);
        var fieldIndex = 0;
        var maxSize = 0;
        var maxAlign = 1;

        void AddBits(int startBit, int bitCount, BitOwner owner)
        {
            for (var b = startBit; b < startBit + bitCount; b++)
            {
                if (!bitOwners.TryGetValue(b, out var owners))
                {
                    owners = [];
                    bitOwners[b] = owners;
                }

                owners.Add(owner);
            }
        }

        foreach (var decl in declarations)
        {
            var type = ResolveType(decl, customTypes);
            var baseAlign = ApplyPack(type.Align, pack);
            var align = ResolveFieldAlign(baseAlign, decl.AlignAs, pack);
            maxAlign = Math.Max(maxAlign, align);

            var color = GetColor(colorMap, decl.Name, fieldIndex++);
            FieldLayoutItem field;

            if (decl.BitWidth is null)
            {
                var bytes = type.Size * decl.ArrayCount;
                field = new FieldLayoutItem(
                    decl.Name,
                    decl.DisplayType,
                    0,
                    bytes,
                    null,
                    null,
                    align,
                    color,
                    0,
                    bytes * 8,
                    false,
                    true);
                maxSize = Math.Max(maxSize, bytes);
                AddBits(0, bytes * 8, BitOwner.Field(field));
            }
            else
            {
                if (!type.AllowBitField)
                {
                    throw new InvalidOperationException($"{decl.Name} 的类型不支持位域: {decl.BaseType}");
                }

                var width = decl.BitWidth.Value;
                var limit = type.Size * 8;
                if (width < 0 || width > limit)
                {
                    throw new InvalidOperationException($"{decl.Name} 位域宽度非法（0..{limit}）。");
                }

                if (width == 0)
                {
                    continue;
                }

                // union 成员共享偏移 0，位域用自身位宽显示，但占用单位按基类型。
                field = new FieldLayoutItem(
                    decl.Name,
                    decl.DisplayType,
                    0,
                    type.Size,
                    0,
                    width,
                    align,
                    color,
                    0,
                    width,
                    true,
                    true);
                maxSize = Math.Max(maxSize, type.Size);
                AddBits(0, width, BitOwner.Field(field));
            }

            fields.Add(field);
        }

        var rawSize = maxSize;
        var structAlign = ResolveStructAlign(maxAlign, typeAlignOverride, pack);
        var totalSize = AlignUp(rawSize, structAlign);
        if (totalSize > rawSize)
        {
            AddBits(rawSize * 8, (totalSize - rawSize) * 8, BitOwner.Padding("尾部填充", "#E2E8F0"));
        }

        return new LayoutResult(fields, BuildRows(totalSize, bitOwners), rawSize, totalSize, structAlign);
    }

    private static List<ByteRowItem> BuildRows(int totalBytes, Dictionary<int, List<BitOwner>> bitOwners)
    {
        var rows = new List<ByteRowItem>();
        for (var start = 0; start < totalBytes; start += 8)
        {
            var bytes = new List<ByteCellItem>();
            var rowEnd = Math.Min(totalBytes, start + 8);
            for (var b = start; b < rowEnd; b++)
            {
                var bitItems = new List<BitCellItem>();
                for (var bit = 7; bit >= 0; bit--)
                {
                    var abs = b * 8 + bit;
                    bitOwners.TryGetValue(abs, out var owners);
                    bitItems.Add(BuildBitItem(bit, owners ?? []));
                }

                bytes.Add(new ByteCellItem(b, bitItems));
            }

            rows.Add(new ByteRowItem(start, bytes));
        }

        return rows;
    }

    private static BitCellItem BuildBitItem(int bitIndex, List<BitOwner> owners)
    {
        if (owners.Count == 0)
        {
            return new BitCellItem(bitIndex, "空闲", "空闲", "#F8FAFC", true, false);
        }

        var nonPadding = owners.Where(x => x.Kind == OwnerKind.Field).DistinctBy(x => x.Name).ToList();
        if (nonPadding.Count == 0)
        {
            var pad = owners[0];
            return new BitCellItem(bitIndex, pad.Name, pad.Name, pad.Color, true, false);
        }

        if (nonPadding.Count == 1)
        {
            var one = nonPadding[0];
            return new BitCellItem(bitIndex, one.Name, one.Name, one.Color, false, false);
        }

        var top = nonPadding.Take(4).ToList();
        var tooltip = $"重叠: {string.Join(" / ", top.Select(x => x.Name))}";
        if (nonPadding.Count > 4)
        {
            tooltip += " / ...";
        }

        return new BitCellItem(bitIndex, "重叠", tooltip, BuildGradient(top.Select(x => x.Color).ToList()), false, true);
    }

    private static string BuildGradient(List<string> colors)
    {
        if (colors.Count == 0)
        {
            return "#F8FAFC";
        }

        if (colors.Count == 1)
        {
            return colors[0];
        }

        var step = 100d / colors.Count;
        var parts = new List<string>();
        for (var i = 0; i < colors.Count; i++)
        {
            var from = (i * step).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            var to = ((i + 1) * step).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            parts.Add($"{colors[i]} {from}% {to}%");
        }

        return $"linear-gradient(135deg, {string.Join(", ", parts)})";
    }

    private static PrimitiveTypeInfo ResolveType(FieldDecl decl, Dictionary<string, PrimitiveTypeInfo>? customTypes = null)
    {
        if (decl.IsPointer)
        {
            return new PrimitiveTypeInfo(PointerSize, PointerSize, true);
        }

        if (customTypes is not null && customTypes.TryGetValue(decl.BaseType, out var customType))
        {
            return customType;
        }

        if (!TypeMap.TryGetValue(decl.BaseType, out var type))
        {
            throw new InvalidOperationException($"类型不支持: {decl.RawType}");
        }

        return type;
    }

    private static int ApplyPack(int align, int pack)
    {
        if (pack <= 0)
        {
            return align;
        }

        return Math.Min(align, pack);
    }

    private static int ResolveFieldAlign(int baseAlign, int? alignAs, int pack)
    {
        var align = baseAlign;
        if (alignAs is > 0)
        {
            align = Math.Max(align, alignAs.Value);
        }

        return ApplyPack(align, pack);
    }

    private static int ResolveStructAlign(int baseAlign, int? alignAs, int pack)
    {
        var align = baseAlign;
        if (alignAs is > 0)
        {
            align = Math.Max(align, alignAs.Value);
        }

        align = ApplyPack(align, pack);
        return Math.Max(1, align);
    }

    private static int AlignUp(int value, int align)
    {
        if (align <= 1)
        {
            return value;
        }

        var remainder = value % align;
        return remainder == 0 ? value : value + align - remainder;
    }

    private static string GetColor(Dictionary<string, string> cache, string name, int index)
    {
        if (cache.TryGetValue(name, out var exists))
        {
            return exists;
        }

        var color = Palette[index % Palette.Length];
        cache[name] = color;
        return color;
    }

    private static Dictionary<string, PrimitiveTypeInfo> BuildTypeMap()
    {
        return new Dictionary<string, PrimitiveTypeInfo>(StringComparer.Ordinal)
        {
            ["bool"] = new PrimitiveTypeInfo(1, 1, true),
            ["char"] = new PrimitiveTypeInfo(1, 1, true),
            ["signed char"] = new PrimitiveTypeInfo(1, 1, true),
            ["unsigned char"] = new PrimitiveTypeInfo(1, 1, true),
            ["char8_t"] = new PrimitiveTypeInfo(1, 1, true),
            ["int8_t"] = new PrimitiveTypeInfo(1, 1, true),
            ["uint8_t"] = new PrimitiveTypeInfo(1, 1, true),
            ["std::int8_t"] = new PrimitiveTypeInfo(1, 1, true),
            ["std::uint8_t"] = new PrimitiveTypeInfo(1, 1, true),
            ["byte"] = new PrimitiveTypeInfo(1, 1, false),
            ["std::byte"] = new PrimitiveTypeInfo(1, 1, false),

            ["short"] = new PrimitiveTypeInfo(2, 2, true),
            ["short int"] = new PrimitiveTypeInfo(2, 2, true),
            ["unsigned short"] = new PrimitiveTypeInfo(2, 2, true),
            ["unsigned short int"] = new PrimitiveTypeInfo(2, 2, true),
            ["char16_t"] = new PrimitiveTypeInfo(2, 2, true),
            ["wchar_t"] = new PrimitiveTypeInfo(2, 2, true),
            ["int16_t"] = new PrimitiveTypeInfo(2, 2, true),
            ["uint16_t"] = new PrimitiveTypeInfo(2, 2, true),
            ["std::int16_t"] = new PrimitiveTypeInfo(2, 2, true),
            ["std::uint16_t"] = new PrimitiveTypeInfo(2, 2, true),

            ["int"] = new PrimitiveTypeInfo(4, 4, true),
            ["unsigned"] = new PrimitiveTypeInfo(4, 4, true),
            ["unsigned int"] = new PrimitiveTypeInfo(4, 4, true),
            ["long"] = new PrimitiveTypeInfo(4, 4, true),
            ["long int"] = new PrimitiveTypeInfo(4, 4, true),
            ["unsigned long"] = new PrimitiveTypeInfo(4, 4, true),
            ["unsigned long int"] = new PrimitiveTypeInfo(4, 4, true),
            ["float"] = new PrimitiveTypeInfo(4, 4, false),
            ["char32_t"] = new PrimitiveTypeInfo(4, 4, true),
            ["int32_t"] = new PrimitiveTypeInfo(4, 4, true),
            ["uint32_t"] = new PrimitiveTypeInfo(4, 4, true),
            ["std::int32_t"] = new PrimitiveTypeInfo(4, 4, true),
            ["std::uint32_t"] = new PrimitiveTypeInfo(4, 4, true),

            ["long long"] = new PrimitiveTypeInfo(8, 8, true),
            ["long long int"] = new PrimitiveTypeInfo(8, 8, true),
            ["unsigned long long"] = new PrimitiveTypeInfo(8, 8, true),
            ["unsigned long long int"] = new PrimitiveTypeInfo(8, 8, true),
            ["double"] = new PrimitiveTypeInfo(8, 8, false),
            ["long double"] = new PrimitiveTypeInfo(8, 8, false),
            ["int64_t"] = new PrimitiveTypeInfo(8, 8, true),
            ["uint64_t"] = new PrimitiveTypeInfo(8, 8, true),
            ["std::int64_t"] = new PrimitiveTypeInfo(8, 8, true),
            ["std::uint64_t"] = new PrimitiveTypeInfo(8, 8, true),
            ["size_t"] = new PrimitiveTypeInfo(8, 8, true),
            ["std::size_t"] = new PrimitiveTypeInfo(8, 8, true),
            ["intptr_t"] = new PrimitiveTypeInfo(PointerSize, PointerSize, true),
            ["uintptr_t"] = new PrimitiveTypeInfo(PointerSize, PointerSize, true)
        };
    }

    private static class SourceParser
    {
        private static readonly string[] PrefixKeywords =
        [
            "const", "volatile", "mutable", "register", "static", "constexpr", "extern", "inline", "thread_local", "friend", "typename"
        ];

        private static int _anonymousTypeSeed = 0;

        public static string NormalizeSource(string source)
        {
            var text = source ?? string.Empty;
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//.*$", string.Empty, RegexOptions.Multiline);
            return text;
        }

        public static LayoutKind DetectLayoutKind(string source)
        {
            var header = source;
            var braceIndex = source.IndexOf('{');
            if (braceIndex > 0)
            {
                header = source[..braceIndex];
            }

            return Regex.IsMatch(header, @"\bunion\b", RegexOptions.IgnoreCase)
                ? LayoutKind.Union
                : LayoutKind.Struct;
        }

        public static int? DetectTopLevelAlignas(string source)
        {
            var m = Regex.Match(source, @"alignas\s*\(\s*(?<n>\d+)\s*\)\s*(struct|class|union)", RegexOptions.IgnoreCase);
            return m.Success ? int.Parse(m.Groups["n"].Value) : null;
        }

        public static int DetectPragmaPack(string source)
        {
            var m = Regex.Matches(source, @"#\s*pragma\s+pack\s*\(\s*(?:push\s*,\s*)?(?<n>\d+)\s*\)", RegexOptions.IgnoreCase);
            if (m.Count > 0)
            {
                return int.Parse(m[^1].Groups["n"].Value);
            }

            if (Regex.IsMatch(source, @"__attribute__\s*\(\(\s*packed\s*\)\)", RegexOptions.IgnoreCase))
            {
                return 1;
            }

            return 0;
        }

        public static List<FieldDecl> ParseDeclarations(string source, out Dictionary<string, PrimitiveTypeInfo> customTypes)
        {
            customTypes = new Dictionary<string, PrimitiveTypeInfo>(StringComparer.Ordinal);
            var body = ExtractBody(source);
            var statements = SplitTopLevelStatements(body);
            var result = new List<FieldDecl>();

            foreach (var raw in statements)
            {
                var statement = raw.Trim();
                if (string.IsNullOrWhiteSpace(statement))
                {
                    continue;
                }

                // 允许 "public: uint8_t tag" 这种同语句写法。
                statement = StripAccessLabelPrefix(statement);
                if (string.IsNullOrWhiteSpace(statement))
                {
                    continue;
                }

                if (statement.EndsWith(":", StringComparison.Ordinal))
                {
                    continue;
                }

                var alignAs = ParseAndStripAlignAs(ref statement);
                statement = StripAttributes(statement);
                statement = StripPrefixKeywords(statement);
                if (string.IsNullOrWhiteSpace(statement))
                {
                    continue;
                }

                // 先处理内联 struct/class/union 定义，支持：
                // 1) class Meta { ... };
                // 2) union { ... } u;
                // 3) struct Node { ... } nodeArr[2];
                if (TryParseComposite(statement, alignAs, result, customTypes))
                {
                    continue;
                }

                if (statement.Contains('(') || statement.Contains(')'))
                {
                    continue;
                }

                var decl = TryParseBitField(statement, alignAs) ?? TryParseNormalField(statement, alignAs);
                if (decl is null)
                {
                    throw new InvalidOperationException($"无法解析字段声明: {raw.Trim()}");
                }

                result.Add(decl);
            }

            return result;
        }

        private static string StripAccessLabelPrefix(string statement)
        {
            var s = statement.Trim();
            while (true)
            {
                var m = Regex.Match(s, @"^(public|private|protected)\s*:\s*(?<rest>.*)$", RegexOptions.IgnoreCase);
                if (!m.Success)
                {
                    return s;
                }

                s = m.Groups["rest"].Value.Trim();
                if (string.IsNullOrWhiteSpace(s))
                {
                    return string.Empty;
                }
            }
        }

        private static bool TryParseComposite(
            string statement,
            int? alignAs,
            List<FieldDecl> outputFields,
            Dictionary<string, PrimitiveTypeInfo> customTypes)
        {
            var m = Regex.Match(
                statement,
                @"^(?<kind>struct|class|union)\s*(?<name>[A-Za-z_][A-Za-z0-9_]*)?\s*\{(?<inner>[\s\S]*)\}\s*(?<var>[A-Za-z_][A-Za-z0-9_]*)?\s*(\[\s*(?<count>\d+)\s*\])?$",
                RegexOptions.IgnoreCase);
            if (!m.Success)
            {
                return false;
            }

            var kindText = m.Groups["kind"].Value.ToLowerInvariant();
            var innerBody = m.Groups["inner"].Value;
            var typeName = m.Groups["name"].Success ? m.Groups["name"].Value : string.Empty;
            var varName = m.Groups["var"].Success ? m.Groups["var"].Value : string.Empty;
            var count = m.Groups["count"].Success ? int.Parse(m.Groups["count"].Value) : 1;
            if (count <= 0)
            {
                throw new InvalidOperationException("数组长度必须大于 0。");
            }

            // 递归解析内层成员，并计算该复合类型大小/对齐。
            var innerDecls = ParseDeclarations(innerBody, out var nestedTypes);
            foreach (var kv in nestedTypes)
            {
                customTypes[kv.Key] = kv.Value;
            }

            PrimitiveTypeInfo typeInfo;
            if (innerDecls.Count == 0)
            {
                typeInfo = new PrimitiveTypeInfo(0, 1, false);
            }
            else
            {
                var kind = kindText == "union" ? LayoutKind.Union : LayoutKind.Struct;
                var layout = kind == LayoutKind.Union
                    ? BuildUnionLayout(innerDecls, 0, null, customTypes)
                    : BuildStructLayout(innerDecls, 0, null, customTypes);
                typeInfo = new PrimitiveTypeInfo(layout.TotalSize, Math.Max(1, layout.StructAlign), false);
            }

            // 命名类型：注册到自定义类型表，供后续成员（例如 Meta meta;）使用。
            var normalizedTypeName = string.IsNullOrWhiteSpace(typeName)
                ? $"__anon_{Interlocked.Increment(ref _anonymousTypeSeed)}"
                : NormalizeType(typeName);
            customTypes[normalizedTypeName] = typeInfo;

            // 复合定义后若带变量名，则它本身也是一个成员。
            if (!string.IsNullOrWhiteSpace(varName))
            {
                var displayType = string.IsNullOrWhiteSpace(typeName)
                    ? $"{kindText}(anonymous)"
                    : NormalizeType(typeName);
                if (count > 1)
                {
                    displayType = $"{displayType}[{count}]";
                }

                outputFields.Add(new FieldDecl(
                    normalizedTypeName,
                    string.IsNullOrWhiteSpace(typeName) ? $"{kindText}(anonymous)" : typeName,
                    varName,
                    null,
                    count,
                    false,
                    alignAs,
                    displayType));
            }

            return true;
        }

        private static FieldDecl? TryParseBitField(string statement, int? alignAs)
        {
            // 先匹配“有名字”的位域，避免 anonymous 位域把类型错误截断（例如 uint32_t : 0）。
            var named = Regex.Match(statement,
                @"^(?<type>.+?)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*:\s*(?<bits>\d+)$");
            if (named.Success)
            {
                var bits = int.Parse(named.Groups["bits"].Value);
                var rawType = named.Groups["type"].Value.Trim();
                if (rawType.Contains('*', StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("指针类型不支持位域。");
                }

                var baseType = NormalizeType(rawType);
                var name = named.Groups["name"].Value;
                return new FieldDecl(baseType, rawType, name, bits, 1, false, alignAs, $"{baseType}:{bits}");
            }

            // 再匹配匿名位域（例如 uint32_t : 0）。
            var anonymous = Regex.Match(statement,
                @"^(?<type>.+?)\s*:\s*(?<bits>\d+)$");
            if (!anonymous.Success)
            {
                return null;
            }

            var bitsAnon = int.Parse(anonymous.Groups["bits"].Value);
            var rawTypeAnon = anonymous.Groups["type"].Value.Trim();
            if (rawTypeAnon.Contains('*', StringComparison.Ordinal))
            {
                throw new InvalidOperationException("指针类型不支持位域。");
            }

            var baseTypeAnon = NormalizeType(rawTypeAnon);
            return new FieldDecl(baseTypeAnon, rawTypeAnon, "_", bitsAnon, 1, false, alignAs, $"{baseTypeAnon}:{bitsAnon}");
        }

        private static FieldDecl? TryParseNormalField(string statement, int? alignAs)
        {
            var m = Regex.Match(statement,
                @"^(?<type>.+?)\s*(?<ptr>\*+)?\s*(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*(\[\s*(?<count>\d+)\s*\])?$");
            if (!m.Success)
            {
                return null;
            }

            var rawType = m.Groups["type"].Value.Trim();
            var baseType = NormalizeType(rawType);
            var isPointer = m.Groups["ptr"].Success;
            var count = m.Groups["count"].Success ? int.Parse(m.Groups["count"].Value) : 1;
            if (count <= 0)
            {
                throw new InvalidOperationException("数组长度必须大于 0。");
            }

            var typeDisplay = isPointer ? $"{baseType}*" : baseType;
            if (count > 1)
            {
                typeDisplay = $"{typeDisplay}[{count}]";
            }

            return new FieldDecl(baseType, rawType, m.Groups["name"].Value, null, count, isPointer, alignAs, typeDisplay);
        }

        private static int? ParseAndStripAlignAs(ref string statement)
        {
            var m = Regex.Match(statement, @"^alignas\s*\(\s*(?<n>\d+)\s*\)\s*(?<rest>.+)$", RegexOptions.IgnoreCase);
            if (!m.Success)
            {
                return null;
            }

            statement = m.Groups["rest"].Value.Trim();
            return int.Parse(m.Groups["n"].Value);
        }

        private static string StripAttributes(string statement)
        {
            var s = Regex.Replace(statement, @"\[\[.*?\]\]", string.Empty);
            s = Regex.Replace(s, @"__attribute__\s*\(\(.*?\)\)", string.Empty, RegexOptions.IgnoreCase);
            return s.Trim();
        }

        private static string StripPrefixKeywords(string statement)
        {
            var s = statement.Trim();
            var changed = true;
            while (changed)
            {
                changed = false;
                foreach (var kw in PrefixKeywords)
                {
                    if (s.StartsWith(kw + " ", StringComparison.OrdinalIgnoreCase))
                    {
                        s = s[(kw.Length + 1)..].TrimStart();
                        changed = true;
                    }
                }
            }

            return s;
        }

        private static string ExtractBody(string source)
        {
            var start = source.IndexOf('{');
            if (start < 0)
            {
                return source;
            }

            var depth = 0;
            for (var i = start; i < source.Length; i++)
            {
                if (source[i] == '{')
                {
                    depth++;
                }
                else if (source[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return source[(start + 1)..i];
                    }
                }
            }

            return source;
        }

        private static List<string> SplitTopLevelStatements(string body)
        {
            var list = new List<string>();
            var current = new System.Text.StringBuilder();
            var depth = 0;

            foreach (var ch in body)
            {
                if (ch == '{')
                {
                    depth++;
                    current.Append(ch);
                    continue;
                }

                if (ch == '}')
                {
                    depth = Math.Max(0, depth - 1);
                    current.Append(ch);
                    continue;
                }

                if (ch == ';' && depth == 0)
                {
                    var s = current.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        list.Add(s);
                    }

                    current.Clear();
                    continue;
                }

                current.Append(ch);
            }

            var tail = current.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(tail))
            {
                list.Add(tail);
            }

            return list;
        }

        private static string NormalizeType(string type)
        {
            var t = Regex.Replace(type.Trim(), @"\s+", " ");
            t = t.Replace("std ::", "std::");
            t = t.Replace(" ::", "::");
            t = t.Replace(":: ", "::");
            return t.ToLowerInvariant();
        }
    }

    private sealed class PrimitiveTypeInfo(int size, int align, bool allowBitField)
    {
        public int Size { get; } = size;
        public int Align { get; } = align;
        public bool AllowBitField { get; } = allowBitField;
    }

    private sealed class BitContainer(string typeKey, int startByte, int sizeBytes)
    {
        public string TypeKey { get; } = typeKey;
        public int StartByte { get; } = startByte;
        public int SizeBytes { get; } = sizeBytes;
        public int CapacityBits => SizeBytes * 8;
        public int UsedBits { get; set; }
    }

    private sealed record LayoutResult(List<FieldLayoutItem> Fields, List<ByteRowItem> Rows, int RawSize, int TotalSize, int StructAlign);

    private sealed record FieldDecl(
        string BaseType,
        string RawType,
        string Name,
        int? BitWidth,
        int ArrayCount,
        bool IsPointer,
        int? AlignAs,
        string DisplayType);

    private sealed record BitOwner(string Name, string Color, OwnerKind Kind)
    {
        public static BitOwner Field(FieldLayoutItem field) => new(field.Name, field.Color, OwnerKind.Field);
        public static BitOwner Padding(string name, string color) => new(name, color, OwnerKind.Padding);
    }

    private enum OwnerKind
    {
        Field,
        Padding
    }
}

public enum LayoutKind
{
    Struct,
    Union
}

public sealed record FieldLayoutItem(
    string Name,
    string TypeDisplay,
    int OffsetByte,
    int SizeByte,
    int? BitOffsetInByte,
    int? BitWidth,
    int Alignment,
    string Color,
    int StartBit,
    int LengthBits,
    bool IsBitField,
    bool IsUnionMember);

public sealed record ByteRowItem(int StartOffset, List<ByteCellItem> Bytes);

public sealed record ByteCellItem(int ByteOffset, List<BitCellItem> Bits);

public sealed record BitCellItem(
    int BitIndex,
    string Label,
    string Tooltip,
    string Background,
    bool IsPadding,
    bool IsOverlap);
