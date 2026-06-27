namespace ToolBox.Services;

public static class PlatformFileAccess
{
    public static Func<string, Task<Stream>>? OpenAppPackageFileHandler { get; set; }
    public static Func<string>? AppDataDirectoryProvider { get; set; }

    public static Task<Stream> OpenAppPackageFileAsync(string relativePath)
    {
        if (OpenAppPackageFileHandler is null)
        {
            throw new PlatformNotSupportedException("App package file access is not configured.");
        }

        return OpenAppPackageFileHandler(relativePath);
    }

    public static string GetAppDataDirectory()
    {
        return AppDataDirectoryProvider?.Invoke()
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ToolBox");
    }
}