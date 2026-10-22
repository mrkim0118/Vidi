using SimpleVideoPlayer;

var tempAppData = Path.Combine(Path.GetTempPath(), "TechArchiveVideoPlayerTests", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(tempAppData);
Environment.SetEnvironmentVariable("APPDATA", tempAppData);

try
{
    var original = new AppSettings
    {
        UseKorean = true,
        SkipSeconds = 17,
        Volume = 23,
        CaptureOptions = ScreenCaptureOptions.CreateDefault()
    };

    original.Save();
    var loaded = AppSettings.Load();

    AssertEqual(23, loaded.Volume, "Volume should persist to settings.ini.");
    AssertEqual(17, loaded.SkipSeconds, "Skip seconds should keep persisting.");
    AssertEqual(true, loaded.UseKorean, "Language should keep persisting.");

    await AssertPlaybackRequestsAreSerializedAsync();
}
finally
{
    try
    {
        Directory.Delete(tempAppData, recursive: true);
    }
    catch
    {
    }
}

async Task AssertPlaybackRequestsAreSerializedAsync()
{
    var gate = new PlaybackRequestGate();
    var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var order = new List<int>();

    var first = gate.RunAsync(async () =>
    {
        order.Add(1);
        firstStarted.SetResult();
        await releaseFirst.Task;
    });

    await firstStarted.Task;
    var second = gate.RunAsync(() =>
    {
        order.Add(2);
        return Task.CompletedTask;
    });

    await Task.Delay(50);
    AssertEqual(false, second.IsCompleted, "A second playback request must wait for the first request.");

    releaseFirst.SetResult();
    await Task.WhenAll(first, second);
    AssertEqual("1,2", string.Join(',', order), "Playback requests should be processed in order.");
}

static void AssertEqual<T>(T expected, T actual, string message)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{message} Expected '{expected}', got '{actual}'.");
    }
}
