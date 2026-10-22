using Microsoft.Win32;

namespace SimpleVideoPlayer;

internal static class FileAssociationService
{
    private const string AppName = "Tech Archive Video Player";
    private const string ExeName = "TechArchiveVideoPlayer.exe";
    private const string ApplicationKeyPath = @"Software\Classes\Applications\TechArchiveVideoPlayer.exe";

    public static void RegisterOpenWith()
    {
        var exePath = Path.Combine(AppContext.BaseDirectory, ExeName);

        using var applicationKey = Registry.CurrentUser.CreateSubKey(ApplicationKeyPath);
        applicationKey?.SetValue("FriendlyAppName", AppName);

        using var commandKey = Registry.CurrentUser.CreateSubKey(ApplicationKeyPath + @"\shell\open\command");
        commandKey?.SetValue("", $"\"{exePath}\" \"%1\"");

        using var supportedTypesKey = Registry.CurrentUser.CreateSubKey(ApplicationKeyPath + @"\SupportedTypes");
        foreach (var extension in VideoFileTypes.Extensions)
        {
            supportedTypesKey?.SetValue(extension, "");
        }
    }

    public static void UnregisterOpenWith()
    {
        Registry.CurrentUser.DeleteSubKeyTree(ApplicationKeyPath, throwOnMissingSubKey: false);
    }
}
