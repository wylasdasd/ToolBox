using Microsoft.Maui.Storage;
using ToolBox.Services;

namespace ToolBox.Services.MauiPlatform;

public static class MauiPlatformFileAccess
{
    public static void Register()
    {
        PlatformFileAccess.OpenAppPackageFileHandler = relativePath =>
            FileSystem.Current.OpenAppPackageFileAsync(relativePath);
        PlatformFileAccess.AppDataDirectoryProvider = () =>
            FileSystem.Current.AppDataDirectory;
    }
}