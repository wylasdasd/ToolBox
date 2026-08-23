namespace ToolBox.Tools.Format;

/// <summary>
/// JsonToCSharpOptions：用于表示和处理相关的。
/// </summary>
public sealed record JsonToCSharpOptions(
    bool IsLowerCase,
    bool IsCSharpStyle,
    bool IsNullable,
    string SelectedNumberType,
    bool DetectTypes,
    bool IsRecordType,
    bool MergeArrays);

/// <summary>
/// JsonToCSharpResult：用于表示和处理相关的。
/// </summary>
public sealed record JsonToCSharpResult(string Output);
