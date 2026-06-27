using System.Text;
using Blazing.Mvvm.ComponentModel;

namespace ToolBox.Components.Pages;

public class UuidGeneratorVM : ViewModelBase
{
    private int _count = 10;
    public int Count
    {
        get => _count;
        set { SetProperty(ref _count, value); }
    }

    private bool _includeHyphens = true;
    public bool IncludeHyphens
    {
        get => _includeHyphens;
        set { SetProperty(ref _includeHyphens, value); }
    }

    private bool _upperCase;
    public bool UpperCase
    {
        get => _upperCase;
        set { SetProperty(ref _upperCase, value); }
    }

    private string _generatedUuids = string.Empty;
    public string GeneratedUuids
    {
        get => _generatedUuids;
        set { SetProperty(ref _generatedUuids, value); }
    }

    public void GenerateUuids()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < Count; i++)
        {
            var uuid = Guid.NewGuid();
            var uuidString = IncludeHyphens ? uuid.ToString() : uuid.ToString("N");
            if (UpperCase)
            {
                uuidString = uuidString.ToUpper();
            }
            sb.AppendLine(uuidString);
        }
        GeneratedUuids = sb.ToString();
    }
}
