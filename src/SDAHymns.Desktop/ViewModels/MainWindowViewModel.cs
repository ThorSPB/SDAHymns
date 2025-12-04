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
    private readonly ISearchService _searchService;

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

    // Search properties
    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private List<HymnSearchResult> _searchResults = new();

    [ObservableProperty]
    private HymnSearchResult? _selectedSearchResult;

    [ObservableProperty]
    private List<Hymn> _recentHymns = new();

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

    public MainWindowViewModel(IHymnDisplayService hymnService, IUpdateService updateService, ISearchService searchService)
    {
        _hymnService = hymnService;
        _updateService = updateService;
        _searchService = searchService;

        // Initialize search results and recent hymns
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            // Load initial search results (all hymns in default category)
            await PerformSearchAsync();

            // Load recent hymns
            await LoadRecentHymnsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Initialization error: {ex.Message}";
        }
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

    // Search methods
    partial void OnSearchQueryChanged(string value)
    {
        _ = PerformSearchAsync();
    }

    partial void OnSelectedCategoryChanged(string value)
    {
        _ = PerformSearchAsync();
    }

    private async Task PerformSearchAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                // Show all hymns in category or recent
                SearchResults = await _searchService.SearchHymnsAsync("", SelectedCategory);
            }
            else
            {
                SearchResults = await _searchService.SearchHymnsAsync(SearchQuery, SelectedCategory);
            }

            StatusMessage = $"Found {SearchResults.Count} hymns";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Search error: {ex.Message}";
        }
    }

    partial void OnSelectedSearchResultChanged(HymnSearchResult? value)
    {
        if (value != null)
        {
            _ = LoadHymnFromSearchResultAsync(value);
        }
    }

    private async Task LoadHymnFromSearchResultAsync(HymnSearchResult result)
    {
        try
        {
            CurrentHymn = await _hymnService.GetHymnByNumberAsync(result.Number, result.CategorySlug);

            if (CurrentHymn != null)
            {
                Verses = await _hymnService.GetVersesForHymnAsync(CurrentHymn.Id);
                CurrentVerseIndex = 0;

                // Track recent access
                await _searchService.AddToRecentAsync(CurrentHymn.Id);
                await LoadRecentHymnsAsync();

                OnPropertyChanged(nameof(CurrentVerseContent));
                OnPropertyChanged(nameof(CurrentVerseLabel));
                OnPropertyChanged(nameof(HymnTitle));
                OnPropertyChanged(nameof(FavoriteIcon));
                StatusMessage = $"Loaded: {CurrentHymn.Number}. {CurrentHymn.Title}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading hymn: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task LoadRecentHymnsAsync()
    {
        try
        {
            RecentHymns = await _searchService.GetRecentHymnsAsync(5);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading recent hymns: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync()
    {
        if (CurrentHymn != null)
        {
            try
            {
                await _searchService.ToggleFavoriteAsync(CurrentHymn.Id);
                CurrentHymn.IsFavorite = !CurrentHymn.IsFavorite;
                OnPropertyChanged(nameof(FavoriteIcon));

                // Refresh search results to update favorite icon in list
                await PerformSearchAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error toggling favorite: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private async Task LoadHymnFromRecentAsync(Hymn hymn)
    {
        try
        {
            CurrentHymn = await _hymnService.GetHymnByNumberAsync(hymn.Number, hymn.Category.Slug);

            if (CurrentHymn != null)
            {
                Verses = await _hymnService.GetVersesForHymnAsync(CurrentHymn.Id);
                CurrentVerseIndex = 0;

                await _searchService.AddToRecentAsync(CurrentHymn.Id);
                await LoadRecentHymnsAsync();

                OnPropertyChanged(nameof(CurrentVerseContent));
                OnPropertyChanged(nameof(CurrentVerseLabel));
                OnPropertyChanged(nameof(HymnTitle));
                OnPropertyChanged(nameof(FavoriteIcon));
                StatusMessage = $"Loaded: {CurrentHymn.Number}. {CurrentHymn.Title}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading hymn: {ex.Message}";
        }
    }

    public string FavoriteIcon => CurrentHymn?.IsFavorite == true ? "⭐" : "☆";

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
            // Show error - keep banner visible so user can retry
            StatusMessage = "Failed to download update. Please try again later.";
            IsDownloadingUpdate = false;
            // Note: Keep IsUpdateAvailable = true so the banner stays visible for retry
        }
    }

    [RelayCommand]
    private void DismissUpdate()
    {
        IsUpdateAvailable = false;
    }
}
