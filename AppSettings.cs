namespace Vidi;

public sealed class AppSettings
{
    public bool UseKorean { get; set; }
    public int SkipSeconds { get; set; } = 10;
    public int Volume { get; set; } = 100;
    public ScreenCaptureOptions CaptureOptions { get; set; } = ScreenCaptureOptions.CreateDefault();

    public static string FilePath
    {
        get
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "Vidi", "settings.ini");
        }
    }

    // Settings saved before the rename to Vidi; read once when the new file does not exist yet.
    private static string LegacyFilePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Tech Archive", "Video Player", "settings.ini");

    public static AppSettings Load()
    {
        var settings = new AppSettings();
        var path = File.Exists(FilePath) ? FilePath : LegacyFilePath;
        if (!File.Exists(path))
        {
            return settings;
        }

        string[] lines;
        try
        {
            lines = File.ReadAllLines(path);
        }
        catch
        {
            return settings;
        }

        var section = string.Empty;
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith(';') || line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                section = line[1..^1].Trim();
                continue;
            }

            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();
            ApplyValue(settings, section, key, value);
        }

        settings.SkipSeconds = Math.Clamp(settings.SkipSeconds, 1, 3600);
        settings.Volume = Math.Clamp(settings.Volume, 0, 100);
        settings.CaptureOptions = NormalizeCaptureOptions(settings.CaptureOptions);
        return settings;
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

        var lines = new[]
        {
            "[Language]",
            $"Value={(UseKorean ? "ko" : "en")}",
            string.Empty,
            "[Skip]",
            $"Seconds={Math.Clamp(SkipSeconds, 1, 3600)}",
            string.Empty,
            "[Audio]",
            $"Volume={Math.Clamp(Volume, 0, 100)}",
            string.Empty,
            "[ScreenCapture]",
            $"Format={CaptureOptions.Extension}",
            $"SaveFolder={CaptureOptions.SaveFolder}"
        };

        File.WriteAllLines(FilePath, lines);
    }

    private static void ApplyValue(AppSettings settings, string section, string key, string value)
    {
        if (section.Equals("Language", StringComparison.OrdinalIgnoreCase)
            && key.Equals("Value", StringComparison.OrdinalIgnoreCase))
        {
            settings.UseKorean = value.Equals("ko", StringComparison.OrdinalIgnoreCase)
                || value.Equals("korean", StringComparison.OrdinalIgnoreCase)
                || value.Equals("true", StringComparison.OrdinalIgnoreCase);
            return;
        }

        if (section.Equals("Skip", StringComparison.OrdinalIgnoreCase)
            && key.Equals("Seconds", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(value, out var seconds))
        {
            settings.SkipSeconds = seconds;
            return;
        }

        if (section.Equals("Audio", StringComparison.OrdinalIgnoreCase)
            && key.Equals("Volume", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(value, out var volume))
        {
            settings.Volume = volume;
            return;
        }

        if (!section.Equals("ScreenCapture", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (key.Equals("Format", StringComparison.OrdinalIgnoreCase))
        {
            settings.CaptureOptions = settings.CaptureOptions with { Format = value };
        }
        else if (key.Equals("SaveFolder", StringComparison.OrdinalIgnoreCase))
        {
            settings.CaptureOptions = settings.CaptureOptions with { SaveFolder = value };
        }
    }

    private static ScreenCaptureOptions NormalizeCaptureOptions(ScreenCaptureOptions options)
    {
        var defaultOptions = ScreenCaptureOptions.CreateDefault();
        var folder = string.IsNullOrWhiteSpace(options.SaveFolder)
            ? defaultOptions.SaveFolder
            : options.SaveFolder.Trim();

        return new ScreenCaptureOptions(options.Extension, folder);
    }
}
