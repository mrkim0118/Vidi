using Microsoft.Win32;

namespace Vidi;

internal static class StartupWarmupService
{
    private const string ExeName = "Vidi.exe";
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "VidiVlcWarmup";

    public static void Register()
    {
        var exePath = Path.Combine(AppContext.BaseDirectory, ExeName);
        if (!File.Exists(exePath))
        {
            return;
        }

        using var runKey = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        runKey?.SetValue(RunValueName, $"\"{exePath}\" {VlcWarmupService.Argument} {VlcWarmupService.StartupArgument}");
    }

    public static void Unregister()
    {
        using var runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        runKey?.DeleteValue(RunValueName, throwOnMissingValue: false);
    }
}
