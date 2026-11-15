using Velopack;

namespace SimpleVideoPlayer;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        VelopackApp.Build()
            .OnAfterInstallFastCallback(_ => RunAfterInstallOrUpdate())
            .OnAfterUpdateFastCallback(_ => RunAfterInstallOrUpdate())
            .OnBeforeUninstallFastCallback(_ => RunBeforeUninstall())
            .Run();

        if (VlcWarmupService.IsWarmupRequest(args))
        {
            VlcWarmupService.Run(args);
            Environment.Exit(0);
            return;
        }

        var startupFile = ResolveStartupFile(args);
        using var mutex = new Mutex(true, SingleInstanceServer.MutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            var deliveredToRunningInstance = startupFile is not null
                ? SingleInstanceClient.SendFilePath(startupFile)
                : SingleInstanceClient.SendShowCommand();

            if (deliveredToRunningInstance)
            {
                return;
            }
        }

        ApplicationConfiguration.Initialize();
        using var form = new Form1(startupFile);
        _ = form.Handle; // BeginInvoke from the pipe listener fails until the handle exists.
        using var singleInstanceServer = new SingleInstanceServer(form);
        singleInstanceServer.Start();
        Application.Run(form);
    }

    private static void RunAfterInstallOrUpdate()
    {
        FileAssociationService.RegisterOpenWith();
        StartupWarmupService.Register();
        StartVlcWarmupProcess();
    }

    private static void RunBeforeUninstall()
    {
        FileAssociationService.UnregisterOpenWith();
        StartupWarmupService.Unregister();
    }

    private static void StartVlcWarmupProcess()
    {
        try
        {
            var exePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            {
                return;
            }

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = exePath,
                Arguments = VlcWarmupService.Argument,
                WorkingDirectory = AppContext.BaseDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
            });
        }
        catch
        {
            // Normal app startup still warms VLC if the post-install warmup cannot be launched.
        }
    }

    private static string? ResolveStartupFile(string[] args)
    {
        if (args.Length == 0)
        {
            return null;
        }

        var candidate = args[0].Trim('"');
        if (File.Exists(candidate))
        {
            return candidate;
        }

        candidate = string.Join(' ', args).Trim('"');
        if (File.Exists(candidate))
        {
            return candidate;
        }

        return Uri.TryCreate(candidate, UriKind.Absolute, out var uri) && uri.IsFile && File.Exists(uri.LocalPath)
            ? uri.LocalPath
            : null;
    }
}
