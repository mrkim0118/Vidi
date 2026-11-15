using System.IO.Pipes;

namespace Vidi;

public sealed class SingleInstanceServer : IDisposable
{
    public const string MutexName = "Vidi.SingleInstance";
    public const string PipeName = "Vidi.OpenFile";
    public const string ShowCommand = "SHOW";
    public const string OpenCommandPrefix = "OPEN\t";

    private readonly Form1 _mainForm;
    private readonly CancellationTokenSource _cancellation = new();
    private Task? _serverTask;

    public SingleInstanceServer(Form1 mainForm)
    {
        _mainForm = mainForm;
    }

    public void Start()
    {
        _serverTask ??= Task.Run(ListenAsync);
    }

    public void Dispose()
    {
        _cancellation.Cancel();
        _cancellation.Dispose();
    }

    private async Task ListenAsync()
    {
        while (!_cancellation.IsCancellationRequested)
        {
            try
            {
                await using var pipe = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.In,
                    4,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous);

                await pipe.WaitForConnectionAsync(_cancellation.Token);
                using var reader = new StreamReader(pipe);
                var message = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(message))
                {
                    continue;
                }

                if (message.Equals(ShowCommand, StringComparison.Ordinal))
                {
                    _mainForm.BeginInvoke(_mainForm.ShowMainWindow);
                    continue;
                }

                var filePath = message.StartsWith(OpenCommandPrefix, StringComparison.Ordinal)
                    ? message[OpenCommandPrefix.Length..]
                    : message;
                if (File.Exists(filePath))
                {
                    _mainForm.BeginInvoke(new Action(() => _ = OpenExternalFileSafelyAsync(filePath)));
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch
            {
                await Task.Delay(200, _cancellation.Token).ContinueWith(_ => { });
            }
        }
    }

    private async Task OpenExternalFileSafelyAsync(string filePath)
    {
        try
        {
            await _mainForm.OpenExternalFileAsync(filePath);
        }
        catch
        {
            // Playback errors are handled by the form. Keep the pipe listener alive.
        }
    }
}

public static class SingleInstanceClient
{
    public static bool SendFilePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return false;
        }

        return SendMessage(SingleInstanceServer.OpenCommandPrefix + filePath);
    }

    public static bool SendShowCommand()
    {
        return SendMessage(SingleInstanceServer.ShowCommand);
    }

    private static bool SendMessage(string message)
    {
        for (var attempt = 0; attempt < 80; attempt++)
        {
            try
            {
                using var pipe = new NamedPipeClientStream(
                    ".",
                    SingleInstanceServer.PipeName,
                    PipeDirection.Out);
                pipe.Connect(250);
                using var writer = new StreamWriter(pipe) { AutoFlush = true };
                writer.WriteLine(message);
                return true;
            }
            catch
            {
                Thread.Sleep(150);
            }
        }

        return false;
    }
}
