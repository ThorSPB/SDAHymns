using Velopack;
using Velopack.Sources;

namespace SDAHymns.Core.Services;

public interface IUpdateService
{
    Task<UpdateInfo?> CheckForUpdatesAsync();
    Task<bool> DownloadUpdatesAsync(UpdateInfo updateInfo, IProgress<int>? progress = null);
    void ApplyUpdatesAndRestart(UpdateInfo updateInfo);
    bool IsUpdateAvailable { get; }
    string? LatestVersion { get; }
}

public class UpdateService : IUpdateService
{
    private readonly UpdateManager _updateManager;
    private UpdateInfo? _pendingUpdate;

    public bool IsUpdateAvailable => _pendingUpdate != null;
    public string? LatestVersion => _pendingUpdate?.TargetFullRelease.Version.ToString();

    public UpdateService()
    {
        // Configure GitHub Releases source
        // TODO: Replace with actual GitHub repo URL
        _updateManager = new UpdateManager(
            new GithubSource("https://github.com/ThorSPB/SDAHymns", null, false)
        );
    }

    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        try
        {
            _pendingUpdate = await _updateManager.CheckForUpdatesAsync();
            return _pendingUpdate;
        }
        catch (Exception)
        {
            // Log error (no internet, GitHub down, etc.)
            // Silently fail - app continues normally
            return null;
        }
    }

    public async Task<bool> DownloadUpdatesAsync(UpdateInfo updateInfo, IProgress<int>? progress = null)
    {
        try
        {
            // Convert IProgress<int> to Action<int> for Velopack
            Action<int>? progressAction = progress != null
                ? (p => progress.Report(p))
                : null;

            await _updateManager.DownloadUpdatesAsync(updateInfo, progressAction);
            return true;
        }
        catch (Exception)
        {
            // Log download failure
            return false;
        }
    }

    public void ApplyUpdatesAndRestart(UpdateInfo updateInfo)
    {
        _updateManager.ApplyUpdatesAndRestart(updateInfo);
    }
}
