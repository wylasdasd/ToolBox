using MudBlazor;

namespace ToolBox.Components.Layout;

public static class ToolBoxTheme
{
    public static MudTheme Default { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#111111",
            Secondary = "#0E7490",
            Tertiary = "#F59E0B",
            Background = "#F6F7FB",
            Surface = "#FFFFFF",
            TextPrimary = "#0F172A",
            TextSecondary = "#475569",
            Divider = "#E2E8F0",
        }
    };
}
