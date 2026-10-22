using LibVLCSharp.Shared;

namespace SimpleVideoPlayer;

public static class VlcWarmupService
{
    public const string Argument = "--warmup-vlc";
    public const string StartupArgument = "--startup-warmup";
    private const int StartupDelaySeconds = 3;
    private const int InteractiveWarmupTimeoutSeconds = 8;
    private const int StartupWarmupTimeoutSeconds = 30;

    public static bool IsWarmupRequest(string[] args)
    {
        return args.Any(arg => arg.Equals(Argument, StringComparison.OrdinalIgnoreCase));
    }

    public static void Run(string[] args)
    {
        try
        {
            var isStartupWarmup = args.Any(arg => arg.Equals(StartupArgument, StringComparison.OrdinalIgnoreCase));
            if (isStartupWarmup)
            {
                Thread.Sleep(TimeSpan.FromSeconds(StartupDelaySeconds));
            }

            WriteLog(isStartupWarmup ? "startup warmup started" : "warmup started");
            var touchedBytes = TouchVlcFiles(isStartupWarmup);
            WriteLog($"vlc files touched {touchedBytes / 1024 / 1024} MB");
            var warmupTask = Task.Run(() =>
            {
                try
                {
                    Core.Initialize();
                    using var libVlc = new LibVLC(
                        "--no-video-title-show",
                        "--quiet",
                        "--avcodec-hw=any",
                        "--input-fast-seek",
                        "--no-snapshot-preview");
                    using var mediaPlayer = new MediaPlayer(libVlc)
                    {
                        EnableMouseInput = false,
                        EnableKeyInput = false,
                        Volume = 0
                    };
                }
                catch
                {
                    // Normal playback will initialize VLC if this warmup attempt fails.
                }
            });

            var timeoutSeconds = isStartupWarmup ? StartupWarmupTimeoutSeconds : InteractiveWarmupTimeoutSeconds;
            var completed = warmupTask.Wait(TimeSpan.FromSeconds(timeoutSeconds));
            WriteLog(completed ? "warmup completed" : "warmup timeout");
        }
        catch (Exception ex)
        {
            WriteLog($"warmup failed: {ex.Message}");
            // Warmup is opportunistic; normal playback will still initialize VLC if this fails.
        }
    }

    private static long TouchVlcFiles(bool fullPluginWarmup)
    {
        long touchedBytes = 0;
        var vlcDirectory = Path.Combine(AppContext.BaseDirectory, "libvlc", "win-x64");
        touchedBytes += ReadFile(Path.Combine(vlcDirectory, "libvlc.dll"), maxBytes: 8 * 1024 * 1024);
        touchedBytes += ReadFile(Path.Combine(vlcDirectory, "libvlccore.dll"), maxBytes: 8 * 1024 * 1024);
        touchedBytes += ReadFile(Path.Combine(vlcDirectory, "plugins", "plugins.dat"), maxBytes: 512 * 1024);

        if (!fullPluginWarmup)
        {
            return touchedBytes;
        }

        var pluginsDirectory = Path.Combine(vlcDirectory, "plugins");
        if (!Directory.Exists(pluginsDirectory))
        {
            return touchedBytes;
        }

        var pluginPaths = Directory
            .EnumerateFiles(pluginsDirectory, "*.dll", SearchOption.AllDirectories)
            .OrderBy(GetPluginWarmupPriority)
            .ThenBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase);

        foreach (var pluginPath in pluginPaths)
        {
            touchedBytes += ReadFile(pluginPath, maxBytes: long.MaxValue);
        }

        return touchedBytes;
    }

    private static int GetPluginWarmupPriority(string pluginPath)
    {
        var directory = Path.GetFileName(Path.GetDirectoryName(pluginPath));
        return directory switch
        {
            "codec" => 0,
            "packetizer" => 1,
            "demux" => 2,
            "access" => 3,
            "video_output" => 4,
            "audio_output" => 5,
            "video_filter" => 6,
            "audio_filter" => 7,
            _ => 20
        };
    }

    private static long ReadFile(string filePath, long maxBytes)
    {
        try
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var buffer = new byte[64 * 1024];
            var remaining = Math.Min(maxBytes, stream.Length);
            long totalRead = 0;
            while (remaining > 0)
            {
                var read = stream.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
                if (read <= 0)
                {
                    return totalRead;
                }

                remaining -= read;
                totalRead += read;
            }

            return totalRead;
        }
        catch
        {
            // Ignore files that are locked or unavailable during install/update.
            return 0;
        }
    }

    private static void WriteLog(string message)
    {
        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var directory = Path.Combine(appData, "Tech Archive", "Video Player");
            Directory.CreateDirectory(directory);
            File.AppendAllText(
                Path.Combine(directory, "warmup.log"),
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}");
        }
        catch
        {
            // Logging must never affect startup.
        }
    }
}
