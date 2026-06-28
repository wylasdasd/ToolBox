namespace ToolBox.Tools.Format;

public sealed record JsonToCSharpOptions(
    bool IsLowerCase,
    bool IsCSharpStyle,
    bool IsNullable,
    string SelectedNumberType,
    bool DetectTypes,
    bool IsRecordType,
    bool MergeArrays);

public sealed record JsonToCSharpResult(string Output);