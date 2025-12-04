using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SDAHymns.Core.Data.Models;
using SDAHymns.Core.Services;
using Velopack;

namespace SDAHymns.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IHymnDisplayService _hymnService;
    private readonly IUpdateService _updateService;

    [ObservableProperty]
    private Hymn? _currentHymn;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextVerseCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousVerseCommand))]
    private List<Verse> _verses = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextVerseCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousVerseCommand))]
    private int _currentVerseIndex = 0;

    [ObservableProperty]
    private int _hymnNumber = 1;

    [ObservableProperty]
    private string _selectedCategory = "crestine";

    [ObservableProperty]
    private string _statusMessage = "Enter a hymn number and click Load";

    [ObservableProperty]
    private bool _isAspectRatio43 = true;

    [ObservableProperty]
    private bool _isDisplayWindowOpen = false;

    // Update notification properties
    [ObservableProperty]
    private bool _isUpdateAvailable = false;

    [ObservableProperty]
    private string? _latestVersion;

    [ObservableProperty]
    private bool _isDownloadingUpdate = false;

    [ObservableProperty]
    private int _downloadProgress = 0;

    private UpdateInfo? _pendingUpdate;

    public MainWindowViewModel(IHymnDisplayService hymnService, IUpdateService updateService)
    {
        _hymnService = hymnService;
        _updateService = updateService;
    }

    public string DisplayWindowButtonLabel => IsDisplayWindowOpen ? "Hide Display" : "Show Display";

    // Display dimensions based on aspect ratio
    public double DisplayWidth => IsAspectRatio43 ? 800 : 1920;
    public double DisplayHeight => IsAspectRatio43 ? 600 : 1080;

    public string AspectRatioLabel => IsAspectRatio43 ? "4:3" : "16:9";

    public string VerseIndicator =>
        Verses.Any()
            ? $"Verse {CurrentVerseIndex + 1} of {Verses.Count}"
            : "No verses loaded";

    partial void OnIsAspectRatio43Changed(bool value)
    {
        OnPropertyChanged(nameof(DisplayWidth));
        OnPropertyChanged(nameof(DisplayHeight));
        OnPropertyChanged(nameof(AspectRatioLabel));
    }

    partial void OnIsDisplayWindowOpenChanged(bool value)
    {
        OnPropertyChanged(nameof(DisplayWindowButtonLabel));
    }

    partial void OnCurrentVerseIndexChanged(int value)
    {
        OnPropertyChanged(nameof(CurrentVerseContent));
        OnPropertyChanged(nameof(CurrentVerseLabel));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(VerseIndicator));
    }

    partial void OnVersesChanged(List<Verse> value)
    {
        OnPropertyChanged(nameof(CurrentVerseContent));
        OnPropertyChanged(nameof(CurrentVerseLabel));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(VerseIndicator));
    }

    public string? CurrentVerseContent =>
        Verses.Any() && CurrentVerseIndex >= 0 && CurrentVerseIndex < Verses.Count
            ? Verses[CurrentVerseIndex].Content
            : null;

    public string? CurrentVerseLabel =>
        Verses.Any() && CurrentVerseIndex >= 0 && CurrentVerseIndex < Verses.Count
            ? Verses[CurrentVerseIndex].Label
            : null;

    public string? HymnTitle => CurrentHymn != null
        ? $"{CurrentHymn.Number}. {CurrentHymn.Title}"
        : null;

    public bool CanGoNext => CurrentVerseIndex < Verses.Count - 1;
    public bool CanGoPrevious => CurrentVerseIndex > 0;

    [RelayCommand]
    public async Task LoadHymnAsync()
    {
        try
        {
            StatusMessage = $"Loading hymn {HymnNumber} from {SelectedCategory}...";

            CurrentHymn = await _hymnService.GetHymnByNumberAsync(HymnNumber, SelectedCategory);

            if (CurrentHymn != null)
            {
                Verses = await _hymnService.GetVersesForHymnAsync(CurrentHymn.Id);
                CurrentVerseIndex = 0;

                OnPropertyChanged(nameof(CurrentVerseContent));
                OnPropertyChanged(nameof(CurrentVerseLabel));
                OnPropertyChanged(nameof(HymnTitle));
                OnPropertyChanged(nameof(CanGoNext));
                OnPropertyChanged(nameof(CanGoPrevious));
                OnPropertyChanged(nameof(VerseIndicator));

                StatusMessage = $"Loaded: {CurrentHymn.Title} ({Verses.Count} verses)";
            }
            else
            {
                StatusMessage = $"Hymn {HymnNumber} not found in {SelectedCategory}";
                Verses = new();
                CurrentHymn = null;

                OnPropertyChanged(nameof(CurrentVerseContent));
                OnPropertyChanged(nameof(CurrentVerseLabel));
                OnPropertyChanged(nameof(HymnTitle));
                OnPropertyChanged(nameof(CanGoNext));
                OnPropertyChanged(nameof(CanGoPrevious));
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading hymn: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    public void NextVerse()
    {
        if (CanGoNext)
        {
            CurrentVerseIndex++;
        }
    }

    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    public void PreviousVerse()
    {
        if (CanGoPrevious)
        {
            CurrentVerseIndex--;
        }
    }

    // Update notification methods
    public void ShowUpdateNotification(UpdateInfo updateInfo)
    {
        _pendingUpdate = updateInfo;
        LatestVersion = updateInfo.TargetFullRelease.Version.ToString();
        IsUpdateAvailable = true;
    }

    [RelayCommand]
    private async Task UpdateNowAsync()
    {
        if (_pendingUpdate == null) return;

        IsDownloadingUpdate = true;
        DownloadProgress = 0;

        var progress = new Progress<int>(p => DownloadProgress = p);
        var success = await _updateService.DownloadUpdatesAsync(_pendingUpdate, progress);

        if (success)
        {
            // Apply and restart
            _updateService.ApplyUpdatesAndRestart(_pendingUpdate);
        }
        else
        {
            // Show error
            StatusMessage = "Failed to download update. Please try again later.";
            IsDownloadingUpdate = false;
            IsUpdateAvailable = false;
        }
    }

    [RelayCommand]
    private void DismissUpdate()
    {
        IsUpdateAvailable = false;
    }
}
