namespace Vidi;

public sealed class PlaybackRequestGate
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task RunAsync(Func<Task> operation)
    {
        await _semaphore.WaitAsync();
        try
        {
            await operation();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
