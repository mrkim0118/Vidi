using Vidi;

var tempAppData = Path.Combine(Path.GetTempPath(), "VidiTests", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(tempAppData);
AppSettings.DataDirectory = tempAppData;

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
    AssertResumeStore(Path.Combine(tempAppData, "resume.txt"));
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

static void AssertResumeStore(string file)
{
    var store = new ResumeStore(file, capacity: 2);
    store.Record(@"C:\a.mp4", 60_000, 600_000);
    store.Record(@"C:\b.mp4", 5_000, 600_000);
    store.Record(@"C:\c.mp4", 595_000, 600_000);
    store.Save();

    var loaded = new ResumeStore(file, capacity: 2);
    AssertEqual(@"C:\c.mp4,C:\b.mp4", string.Join(',', loaded.RecentFiles(10)), "Most recent first, trimmed to capacity.");
    AssertEqual(0L, loaded.GetTime(@"C:\b.mp4"), "Positions near the start resume from the beginning.");
    AssertEqual(0L, loaded.GetTime(@"C:\c.mp4"), "Positions near the end resume from the beginning.");

    loaded.Record(@"C:\B.MP4", 120_000, 600_000);
    AssertEqual(120_000L, loaded.GetTime(@"c:\b.mp4"), "Paths match case-insensitively and keep the last position.");
    AssertEqual(@"C:\B.MP4,C:\c.mp4", string.Join(',', loaded.RecentFiles(10)), "Re-recording moves a file to the front without duplicates.");
}

static void AssertEqual<T>(T expected, T actual, string message)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{message} Expected '{expected}', got '{actual}'.");
    }
}
