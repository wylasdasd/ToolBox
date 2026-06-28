namespace ToolBox.Tools.Format;

public sealed record JsonFormatOutputResult(string Output);

public sealed record JsonValidateResult(string Message);

public sealed record JsonPathQueryResult(string Result, bool IsInfoOnly);
