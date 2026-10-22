using Velopack;

namespace SimpleVideoPlayer;

public enum AppUpdateStatus
{
    NotInstalled,
    UpToDate,
    UpdateAvailable,
    Downloaded,
    Installing,
    Failed,
}

public sealed record AppUpdateResult(
    AppUpdateStatus Status,
    string? Version = null,
    string? Message = null,
    UpdateInfo? UpdateInfo = null);

public sealed class AppUpdateService
{
    private readonly string _updateUrl;

    public AppUpdateService(string updateUrl)
    {
        _updateUrl = updateUrl;
    }

    public async Task<AppUpdateResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var manager = new UpdateManager(_updateUrl);
            if (!manager.IsInstalled)
            {
                return new AppUpdateResult(AppUpdateStatus.NotInstalled);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var updateInfo = await manager.CheckForUpdatesAsync();
            if (updateInfo is null)
            {
                return new AppUpdateResult(AppUpdateStatus.UpToDate);
            }

            return new AppUpdateResult(
                AppUpdateStatus.UpdateAvailable,
                updateInfo.TargetFullRelease.Version?.ToString() ?? updateInfo.TargetFullRelease.FileName,
                UpdateInfo: updateInfo);
        }
        catch (Exception ex)
        {
            return new AppUpdateResult(AppUpdateStatus.Failed, Message: ex.Message);
        }
    }

    public async Task<AppUpdateResult> DownloadUpdateAsync(
        UpdateInfo updateInfo,
        Action<int>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var manager = new UpdateManager(_updateUrl);
            if (!manager.IsInstalled)
            {
                return new AppUpdateResult(AppUpdateStatus.NotInstalled);
            }

            await manager.DownloadUpdatesAsync(updateInfo, progressCallback, cancellationToken);
            var pendingRestart = manager.UpdatePendingRestart;
            if (pendingRestart is null)
            {
                return new AppUpdateResult(
                    AppUpdateStatus.Failed,
                    Message: "The update was downloaded, but no pending restart package was found.",
                    UpdateInfo: updateInfo);
            }

            return new AppUpdateResult(
                AppUpdateStatus.Downloaded,
                pendingRestart.Version?.ToString() ?? pendingRestart.FileName ?? updateInfo.TargetFullRelease.FileName,
                UpdateInfo: updateInfo);
        }
        catch (Exception ex)
        {
            return new AppUpdateResult(AppUpdateStatus.Failed, Message: ex.Message, UpdateInfo: updateInfo);
        }
    }

    public AppUpdateResult ApplyDownloadedUpdateAndRestart()
    {
        try
        {
            var manager = new UpdateManager(_updateUrl);
            if (!manager.IsInstalled)
            {
                return new AppUpdateResult(AppUpdateStatus.NotInstalled);
            }

            var pendingRestart = manager.UpdatePendingRestart;
            if (pendingRestart is null)
            {
                return new AppUpdateResult(AppUpdateStatus.Failed, Message: "No downloaded update is pending restart.");
            }

            manager.WaitExitThenApplyUpdates(pendingRestart, false, true, Array.Empty<string>());
            return new AppUpdateResult(
                AppUpdateStatus.Installing,
                pendingRestart.Version?.ToString() ?? pendingRestart.FileName);
        }
        catch (Exception ex)
        {
            return new AppUpdateResult(AppUpdateStatus.Failed, Message: ex.Message);
        }
    }
}
