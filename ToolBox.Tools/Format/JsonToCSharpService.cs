using System.Text;
using System.Text.Json;
using ToolBox.Tools.Common;

namespace ToolBox.Tools.Format;

public static class JsonToCSharpService
{
    public static ToolResult<JsonToCSharpResult> Convert(string? jsonInput, JsonToCSharpOptions options)
    {
        if (string.IsNullOrWhiteSpace(jsonInput))
            return ToolResult<JsonToCSharpResult>.Ok(new JsonToCSharpResult(string.Empty));

        try
        {
            var parseOptions = new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            using var doc = JsonDocument.Parse(jsonInput, parseOptions);

            var sb = new StringBuilder();
            var classes = new Dictionary<string, string>();

            if (doc.RootElement.ValueKind == JsonValueKind.Array)
                GenerateClass("Root", EnumerateJsonArray(doc.RootElement), classes, options);
            else
                GenerateClass("Root", new[] { doc.RootElement }, classes, options);

            if (classes.ContainsKey("Root"))
            {
                sb.AppendLine(classes["Root"]);
                classes.Remove("Root");
            }

            foreach (var cls in classes.Values)
                sb.AppendLine(cls);

            return ToolResult<JsonToCSharpResult>.Ok(new JsonToCSharpResult(sb.ToString()));
        }
        catch (JsonException ex)
        {
            return ToolResult<JsonToCSharpResult>.Fail($"JSON Parsing Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ToolResult<JsonToCSharpResult>.Fail($"Error: {ex.Message}");
        }
    }
private static IEnumerable<JsonElement> EnumerateJsonArray(JsonElement arrayElement)
    {
        if (arrayElement.ValueKind != JsonValueKind.Array) yield break;
        foreach (var item in arrayElement.EnumerateArray())
        {
            yield return item;
        }
    }

    private static void GenerateClass(string className, IEnumerable<JsonElement> objects, Dictionary<string, string> classes, JsonToCSharpOptions options)
    {
        string formattedClassName = FormatClassName(className, options);
        if (classes.ContainsKey(formattedClassName)) return;

        // Collect all unique property names from all objects
        var distinctProperties = new HashSet<string>();
        var objectsList = objects.ToList(); // Materialize to iterate multiple times
        
        // If the "objects" collection is actually empty or contains non-objects, we can't generate a class.
        // But for root array of scalars, GetTypeName handles it. 
        // Here we assume we are generating a CLASS, so we expect Objects.
        // If we get scalars here, it's an edge case (e.g. root array of ints).
        // Let's check first element.
        
        var firstObj = objectsList.FirstOrDefault();
        if (firstObj.ValueKind != JsonValueKind.Object && firstObj.ValueKind != JsonValueKind.Undefined)
        {
            // Edge case: Root is array of scalars (e.g. [1, 2, 3]). 
            // We can't generate a "class Root { ... }" for this in the standard way.
            // But usually this method is called for Objects.
            // If Root is array of ints, GenerateClass is called with "Root". 
            // We should probably just produce a wrapper or a typedef? 
            // For now, let's ignore or handle gracefully.
            return;
        }

        foreach (var obj in objectsList)
        {
            if (obj.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in obj.EnumerateObject())
                {
                    distinctProperties.Add(prop.Name);
                }
            }
        }
        
        // If options.MergeArrays is false, only use first object's properties
        if (!options.MergeArrays && objectsList.Count > 0)
        {
            distinctProperties.Clear();
            if (objectsList[0].ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in objectsList[0].EnumerateObject())
                {
                    distinctProperties.Add(prop.Name);
                }
            }
        }

        var sb = new StringBuilder();
        string classKeyword = options.IsRecordType ? "record" : "class";
        sb.AppendLine($"public {classKeyword} {formattedClassName}");
        sb.AppendLine("{");

        foreach (var propName in distinctProperties)
        {
            // Collect all values for this property across all objects
            var values = new List<JsonElement>();
            foreach (var obj in objectsList)
            {
                if (obj.ValueKind == JsonValueKind.Object && obj.TryGetProperty(propName, out var val))
                {
                    values.Add(val);
                }
                else
                {
                    // Property missing in this object -> explicitly add Undefined or treat as null?
                    // We need to know it's missing to force nullable.
                    // But GetTypeName logic will see "count < objectsList.Count" if we pass list?
                    // Or we just add a "Null" JsonElement?
                    // JsonElement default is Undefined.
                }
            }

            // If a property is present in some objects but not others, it implies Nullable.
            // We pass the total count of objects to GetTypeName to let it decide?
            // Or easier: we determine "isMissing" here.
            bool isMissing = values.Count < objectsList.Count;

            string formattedPropName = FormatPropertyName(propName, options);
            string typeName = GetTypeName(propName, values, classes, isMissing, options);
            
            // Record types often use { get; init; }
            string accessors = options.IsRecordType ? "{ get; init; }" : "{ get; set; }";
            sb.AppendLine($"    public {typeName} {formattedPropName} {accessors}");
        }

        sb.AppendLine("}");
        classes[formattedClassName] = sb.ToString();
    }

    private static string GetTypeName(string rawPropName, List<JsonElement> values, Dictionary<string, string> classes, bool isMissing, JsonToCSharpOptions options)
    {
        // 1. Analyze all values to determine the common type
        bool hasNull = isMissing;
        var nonNullValues = new List<JsonElement>();

        foreach (var val in values)
        {
            if (val.ValueKind == JsonValueKind.Null || val.ValueKind == JsonValueKind.Undefined)
            {
                hasNull = true;
            }
            else
            {
                nonNullValues.Add(val);
            }
        }

        string suffix = (options.IsNullable && hasNull) ? "?" : "";

        if (nonNullValues.Count == 0)
        {
            return "object" + suffix; // All nulls or missing
        }

        // Check for mixed types
        JsonValueKind firstKind = nonNullValues[0].ValueKind;
        bool mixedTypes = nonNullValues.Any(v => v.ValueKind != firstKind);
        
        // Special handling: Int and Float are both Number, but we might want to upgrade Int to Float/Double if mixed.
        if (!mixedTypes && firstKind == JsonValueKind.Number)
        {
            // Check if any value requires floating point
            bool needsFloat = nonNullValues.Any(v => !v.TryGetInt64(out _));
            if (needsFloat)
            {
                return options.SelectedNumberType + suffix;
            }
            
            // All fit in Int64. Fit in Int32?
            bool needsLong = nonNullValues.Any(v => !v.TryGetInt32(out _));
            return (needsLong ? "long" : "int") + suffix;
        }

        if (mixedTypes)
        {
             // If mixed Number (e.g. int and float), it's fine, handled above? 
             // No, ValueKind is same (Number). 
             // If mixed String and Number -> object.
             return "object" + suffix;
        }

        switch (firstKind)
        {
            case JsonValueKind.String:
                if (options.DetectTypes)
                {
                    // Check DateTime
                    if (nonNullValues.All(v => DateTime.TryParse(v.GetString(), out _)))
                        return "DateTime" + suffix;
                    
                    // Check Guid
                    if (nonNullValues.All(v => Guid.TryParse(v.GetString(), out _)))
                        return "Guid" + suffix;
                    
                    // Could check Uri, TimeSpan etc.
                }
                return "string" + suffix;

            case JsonValueKind.True:
            case JsonValueKind.False:
                return "bool" + suffix;

            case JsonValueKind.Object:
                string newClassName = rawPropName;
                GenerateClass(newClassName, nonNullValues, classes, options);
                return FormatClassName(newClassName, options) + suffix;

            case JsonValueKind.Array:
                // We have a list of Arrays. We want to find the type of the items.
                // Flatten all arrays into one collection of items.
                var allItems = new List<JsonElement>();
                foreach (var arr in nonNullValues)
                {
                    foreach (var item in arr.EnumerateArray())
                    {
                        allItems.Add(item);
                    }
                }
                
                string itemType = "object";
                if (allItems.Count > 0)
                {
                    // Recursive call to determine item type
                    // Note: 'isMissing' is false for the *items* themselves inside the array context usually, 
                    // unless we want to detect if *some* arrays had nulls? 
                    // Arrays containing nulls: [null, {}] -> itemType should be nullable.
                    // But GetTypeName handles the list.
                    
                    // Warning: Infinite recursion possible if we pass specific prop name?
                    // rawPropName is "Users", we want "User" or "UsersItem".
                    itemType = GetTypeName(rawPropName + "Item", allItems, classes, false, options);
                }
                
                // If itemType already ends with ?, List<int?> is valid.
                return $"List<{itemType}>" + suffix;

            default:
                return "object" + suffix;
        }
    }

    private static string FormatPropertyName(string name, JsonToCSharpOptions options)
    {
        if (options.IsLowerCase) return name.ToLowerInvariant();
        if (options.IsCSharpStyle) return ToPascalCase(name);
        return name;
    }

    private static string FormatClassName(string name, JsonToCSharpOptions options)
    {
        // Singularize? "Users" -> "User". (Optional, not requested)
        // Just PascalCase
        return ToPascalCase(name);
    }

    private static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var chars = name.ToCharArray();
        if (char.IsLower(chars[0])) chars[0] = char.ToUpper(chars[0]);
        return new string(chars);
    }
}
