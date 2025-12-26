using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SDAHymns.Core.Models;
using SDAHymns.Core.Services;

namespace SDAHymns.Desktop.ViewModels;

public partial class SettingsWindowViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IAudioLibraryService _libraryService;
    private readonly IAudioDownloadService _downloadService;

    [ObservableProperty]
    private string _audioLibraryPath = string.Empty;

    [ObservableProperty]
    private int _autoPlayDelay = 5;

    [ObservableProperty]
    private double _globalVolume = 80;

    [ObservableProperty]
    private ObservableCollection<InstalledPackage> _installedPackages = new();

    [ObservableProperty]
    private ObservableCollection<CategoryPackage> _availablePackages = new();

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isDownloading = false;

    [ObservableProperty]
    private int _downloadProgress = 0;

    [ObservableProperty]
    private string _downloadStatusText = string.Empty;

    [ObservableProperty]
    private long _totalLibrarySize = 0;

    public SettingsWindowViewModel(ISettingsService settingsService, IAudioLibraryService libraryService, IAudioDownloadService downloadService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _libraryService = libraryService ?? throw new ArgumentNullException(nameof(libraryService));
        _downloadService = downloadService ?? throw new ArgumentNullException(nameof(downloadService));

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            // Load current settings
            AudioLibraryPath = await _settingsService.GetAudioLibraryPathAsync();
            AutoPlayDelay = await _settingsService.GetAutoPlayDelayAsync();
            var volume = await _settingsService.GetGlobalVolumeAsync();
            GlobalVolume = volume * 100; // Convert 0-1 to 0-100

            // Load installed packages
            await RefreshInstalledPackagesAsync();

            // Load available packages
            await RefreshAvailablePackagesAsync();

            StatusMessage = "Settings loaded";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading settings: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task BrowseLibraryPathAsync()
    {
        // TODO: Open folder picker dialog
        // For now, this would need platform-specific code or a file picker library
        StatusMessage = "Folder picker not yet implemented";
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        try
        {
            await _settingsService.SetAudioLibraryPathAsync(AudioLibraryPath);
            await _settingsService.SetAutoPlayDelayAsync(AutoPlayDelay);
            await _settingsService.SetGlobalVolumeAsync((float)(GlobalVolume / 100.0));

            StatusMessage = "Settings saved successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving settings: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RefreshInstalledPackagesAsync()
    {
        try
        {
            var packages = await _downloadService.GetInstalledPackagesAsync();
            InstalledPackages.Clear();
            foreach (var package in packages)
            {
                InstalledPackages.Add(package);
            }

            TotalLibrarySize = await _libraryService.GetTotalLibrarySizeAsync();
            StatusMessage = $"Found {packages.Count} installed packages";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading installed packages: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RefreshAvailablePackagesAsync()
    {
        try
        {
            var packages = await _downloadService.GetAvailablePackagesAsync();
            AvailablePackages.Clear();
            foreach (var package in packages)
            {
                AvailablePackages.Add(package);
            }

            StatusMessage = $"Found {packages.Count} available packages";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading available packages: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DownloadCategoryAsync(string categorySlug)
    {
        if (IsDownloading) return;

        try
        {
            IsDownloading = true;
            StatusMessage = $"Downloading {categorySlug}...";

            var progress = new Progress<DownloadProgress>(p =>
            {
                DownloadProgress = p.PercentComplete;
                DownloadStatusText = $"{p.State}: {p.CurrentFile} ({p.PercentComplete}%)";
            });

            await _downloadService.DownloadCategoryAsync(categorySlug, progress);

            StatusMessage = $"Downloaded {categorySlug} successfully";
            await RefreshInstalledPackagesAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Download failed: {ex.Message}";
        }
        finally
        {
            IsDownloading = false;
            DownloadProgress = 0;
            DownloadStatusText = string.Empty;
        }
    }

    [RelayCommand]
    private async Task DeleteCategoryAsync(string categorySlug)
    {
        try
        {
            var success = await _libraryService.DeleteCategoryAsync(categorySlug);
            if (success)
            {
                StatusMessage = $"Deleted {categorySlug}";
                await RefreshInstalledPackagesAsync();
            }
            else
            {
                StatusMessage = $"Failed to delete {categorySlug}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deleting category: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task VerifyCategoryAsync(string categorySlug)
    {
        try
        {
            StatusMessage = $"Verifying {categorySlug}...";

            var progress = new Progress<int>(p =>
            {
                DownloadProgress = p;
            });

            var isValid = await _downloadService.VerifyCategoryAsync(categorySlug, progress);

            StatusMessage = isValid
                ? $"{categorySlug} verified successfully"
                : $"{categorySlug} has issues";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Verification failed: {ex.Message}";
        }
        finally
        {
            DownloadProgress = 0;
        }
    }

    [RelayCommand]
    private async Task MigrateLibraryAsync()
    {
        // TODO: Open folder picker to select new location
        // Then call _libraryService.MigrateLibraryAsync
        StatusMessage = "Migration not yet implemented";
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ScanLibraryHealthAsync()
    {
        try
        {
            StatusMessage = "Scanning library health...";
            var health = await _libraryService.ScanLibraryHealthAsync();

            StatusMessage = health.IsHealthy
                ? $"Library is healthy ({health.TotalFiles} files)"
                : $"Found {health.CorruptedFiles} corrupted files, {health.MissingMetadata} missing metadata";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Health scan failed: {ex.Message}";
        }
    }

    public string FormattedLibrarySize => FormatBytes(TotalLibrarySize);

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
