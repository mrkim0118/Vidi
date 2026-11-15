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
    AssertPlaylistIndexMath();
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

static void AssertPlaylistIndexMath()
{
    // [a,b,c] playing b (1).
    AssertEqual(0, PlaylistIndex.AfterMove(1, 0, 2), "Moving an earlier item past current shifts current left.");
    AssertEqual(2, PlaylistIndex.AfterMove(1, 2, 0), "Moving a later item before current shifts current right.");
    AssertEqual(2, PlaylistIndex.AfterMove(1, 1, 2), "Moving the current item follows it.");
    AssertEqual(1, PlaylistIndex.AfterMove(1, 2, 2), "Moves after current leave it alone.");

    AssertEqual(0, PlaylistIndex.AfterRemove(1, 0, 2), "Removing an earlier item shifts current left.");
    AssertEqual(1, PlaylistIndex.AfterRemove(1, 2, 2), "Removing a later item leaves current alone.");
    AssertEqual(1, PlaylistIndex.AfterRemove(1, 1, 2), "Removing current selects the item that took its place.");
    AssertEqual(0, PlaylistIndex.AfterRemove(1, 1, 1), "Removing the current last item selects the new last one.");
    AssertEqual(-1, PlaylistIndex.AfterRemove(0, 0, 0), "Removing the only item clears the selection.");
}

static void AssertEqual<T>(T expected, T actual, string message)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{message} Expected '{expected}', got '{actual}'.");
    }
}
