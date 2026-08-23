namespace ToolBox.Tools.Format;

/// <summary>
/// JsonFormatOutputResult：用于表示和处理相关的。
/// </summary>
public sealed record JsonFormatOutputResult(string Output);

/// <summary>
/// JsonValidateResult：用于表示和处理相关的。
/// </summary>
public sealed record JsonValidateResult(string Message);

/// <summary>
/// JsonPathQueryResult：用于表示和处理相关的。
/// </summary>
public sealed record JsonPathQueryResult(string Result, bool IsInfoOnly);
