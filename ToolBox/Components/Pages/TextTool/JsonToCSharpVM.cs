using Blazing.Mvvm.ComponentModel;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace ToolBox.Components.Pages;

public partial class JsonToCSharpVM : ViewModelBase
{
    private string _jsonInput = string.Empty;
    private string _cSharpOutput = string.Empty;
    private bool _isLowerCase;
    private bool _isCSharpStyle = true;
    private bool _isNullable = true;
    private string _selectedNumberType = "double";
    private string? _errorMessage;

    // New Options
    private bool _detectTypes = true; // Smart Type Detection (DateTime, Guid)
    private bool _isRecordType;       // Generate Record types
    private bool _mergeArrays = true; // Merge Array Samples (Default true per user request context)

    public string JsonInput
    {
        get => _jsonInput;
        set => SetProperty(ref _jsonInput, value);
    }

    public string CSharpOutput
    {
        get => _cSharpOutput;
        set => SetProperty(ref _cSharpOutput, value);
    }

    public bool IsLowerCase
    {
        get => _isLowerCase;
        set
        {
            if (SetProperty(ref _isLowerCase, value) && value)
            {
                IsCSharpStyle = false;
            }
        }
    }

    public bool IsCSharpStyle
    {
        get => _isCSharpStyle;
        set
        {
            if (SetProperty(ref _isCSharpStyle, value) && value)
            {
                IsLowerCase = false;
            }
        }
    }

    public bool IsNullable
    {
        get => _isNullable;
        set => SetProperty(ref _isNullable, value);
    }

    public string SelectedNumberType
    {
        get => _selectedNumberType;
        set => SetProperty(ref _selectedNumberType, value);
    }

    public bool DetectTypes
    {
        get => _detectTypes;
        set => SetProperty(ref _detectTypes, value);
    }

    public bool IsRecordType
    {
        get => _isRecordType;
        set => SetProperty(ref _isRecordType, value);
    }
    
    // We can expose this if we want it toggleable, or just keep it internal/always on.
    // User asked to "implement" it. Making it toggleable is safer.
    public bool MergeArrays
    {
        get => _mergeArrays;
        set => SetProperty(ref _mergeArrays, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public void ConvertJson()
    {
        ErrorMessage = null;
        if (string.IsNullOrWhiteSpace(JsonInput))
        {
            CSharpOutput = string.Empty;
            return;
        }

        try
        {
            var options = new JsonDocumentOptions 
            { 
                CommentHandling = JsonCommentHandling.Skip, 
                AllowTrailingCommas = true 
            };
            
            using var doc = JsonDocument.Parse(JsonInput, options);

            var sb = new StringBuilder();
            var classes = new Dictionary<string, string>();

            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                // Root is array: Generate "RootItem" from all elements
                GenerateClass("Root", EnumerateJsonArray(doc.RootElement), classes);
            }
            else
            {
                // Root is object: Generate "Root" from single element
                GenerateClass("Root", new[] { doc.RootElement }, classes);
            }

            if (classes.ContainsKey("Root"))
            {
                sb.AppendLine(classes["Root"]);
                classes.Remove("Root");
            }

            foreach (var cls in classes.Values)
            {
                sb.AppendLine(cls);
            }

            CSharpOutput = sb.ToString();
        }
        catch (JsonException ex)
        {
            ErrorMessage = $"JSON Parsing Error: {ex.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
    }

    public async Task CopyToClipboard()
    {
        if (!string.IsNullOrEmpty(CSharpOutput))
        {
            await Clipboard.Default.SetTextAsync(CSharpOutput);
        }
    }

    private IEnumerable<JsonElement> EnumerateJsonArray(JsonElement arrayElement)
    {
        if (arrayElement.ValueKind != JsonValueKind.Array) yield break;
        foreach (var item in arrayElement.EnumerateArray())
        {
            yield return item;
        }
    }

    private void GenerateClass(string className, IEnumerable<JsonElement> objects, Dictionary<string, string> classes)
    {
        string formattedClassName = FormatClassName(className);
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
        
        // If MergeArrays is false, only use first object's properties
        if (!MergeArrays && objectsList.Count > 0)
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
        string classKeyword = IsRecordType ? "record" : "class";
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

            string formattedPropName = FormatPropertyName(propName);
            string typeName = GetTypeName(propName, values, classes, isMissing);
            
            // Record types often use { get; init; }
            string accessors = IsRecordType ? "{ get; init; }" : "{ get; set; }";
            sb.AppendLine($"    public {typeName} {formattedPropName} {accessors}");
        }

        sb.AppendLine("}");
        classes[formattedClassName] = sb.ToString();
    }

    private string GetTypeName(string rawPropName, List<JsonElement> values, Dictionary<string, string> classes, bool isMissing)
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

        string suffix = (IsNullable && hasNull) ? "?" : "";

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
                return SelectedNumberType + suffix;
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
                if (DetectTypes)
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
                GenerateClass(newClassName, nonNullValues, classes);
                return FormatClassName(newClassName) + suffix;

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
                    itemType = GetTypeName(rawPropName + "Item", allItems, classes, false);
                }
                
                // If itemType already ends with ?, List<int?> is valid.
                return $"List<{itemType}>" + suffix;

            default:
                return "object" + suffix;
        }
    }

    private string FormatPropertyName(string name)
    {
        if (IsLowerCase) return name.ToLowerInvariant();
        if (IsCSharpStyle) return ToPascalCase(name);
        return name;
    }

    private string FormatClassName(string name)
    {
        // Singularize? "Users" -> "User". (Optional, not requested)
        // Just PascalCase
        return ToPascalCase(name);
    }

    private string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var chars = name.ToCharArray();
        if (char.IsLower(chars[0])) chars[0] = char.ToUpper(chars[0]);
        return new string(chars);
    }
}