using Microsoft.Win32;

namespace Vidi;

internal static class FileAssociationService
{
    private const string AppName = "Vidi";
    private const string ExeName = "Vidi.exe";
    private const string ApplicationKeyPath = @"Software\Classes\Applications\Vidi.exe";

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
