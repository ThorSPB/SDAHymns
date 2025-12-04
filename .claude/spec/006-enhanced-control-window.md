# Spec 006: Enhanced Control Window UI

**Status:** 📋 Planned
**Created:** 2025-12-04
**Dependencies:** 005-basic-hymn-display.md

## Overview

Enhance the control window with full hymn search, browse, and selection capabilities. Transform the basic number-entry interface into a complete hymn management system with search, filtering, favorites, and recent history.

**Goal:** Create a professional, keyboard-driven control interface for quickly finding and displaying hymns during church services.

## Goals

1. Full-text search across hymn titles and numbers
2. Browse hymns by category with scrollable list
3. Quick access to recently used hymns
4. Favorites/bookmarks system
5. Improved layout with proper search results display
6. Keyboard-first navigation (Tab, Enter, Arrow keys)
7. Real-time search as you type
8. Hymn preview before displaying

**Non-Goals (Future Phases):**
- Audio playback controls
- Service planner integration
- Remote control API
- Statistics tracking

## Architecture

### Enhanced UI Layout

```
┌──────────────────────────────────────────────────┐
│  [Search Box]           [Category ▼]  [Settings] │
├──────────────────────────────────────────────────┤
│ Search Results / Browse List                     │
│ ┌──────────────────────────────────────────────┐ │
│ │ ⭐ 1. Spre slava Ta uniţi                    │ │
│ │    20. Aleluia! Răsună cântec             ← │ │
│ │    45. Domnul e stânca mea                   │ │
│ │    99. O, ce prieten avem în Isus            │ │
│ │   ...                                         │ │
│ └──────────────────────────────────────────────┘ │
├──────────────────────────────────────────────────┤
│ Selected: 20. Aleluia! Răsună cântec            │
│ Crestine • 5 verses                              │
├──────────────────────────────────────────────────┤
│ Recent: [1] [20] [45] [99] [285]                │
├──────────────────────────────────────────────────┤
│ Verse Preview (current verse)                    │
│ [◀ Prev] [Verse 1/5] [Next ▶]                   │
│ [4:3] [Show Display]                             │
├──────────────────────────────────────────────────┤
│ Status: Loaded 20. Aleluia! Răsună cântec       │
└──────────────────────────────────────────────────┘
```

### Data Flow

```
User Types in Search Box
  → SearchService filters hymns by title/number
  → Results displayed in ListBox
  → User selects hymn (click or Enter)
  → HymnDisplayService loads full hymn with verses
  → Preview updates
  → User clicks "Show Display" or presses F5
  → Display window shows selected verse
```

## Implementation Plan

### Step 1: Create SearchService

**File:** `src/SDAHymns.Core/Services/SearchService.cs`

```csharp
public interface ISearchService
{
    Task<List<HymnSearchResult>> SearchHymnsAsync(string query, string? categorySlug = null);
    Task<List<Hymn>> GetRecentHymnsAsync(int count = 10);
    Task<List<Hymn>> GetFavoriteHymnsAsync();
    Task AddToRecentAsync(int hymnId);
    Task ToggleFavoriteAsync(int hymnId);
}

public class HymnSearchResult
{
    public int Id { get; set; }
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public bool IsFavorite { get; set; }
    public int VerseCount { get; set; }
}

public class SearchService : ISearchService
{
    private readonly HymnsContext _context;

    public async Task<List<HymnSearchResult>> SearchHymnsAsync(string query, string? categorySlug = null)
    {
        var hymnsQuery = _context.Hymns
            .Include(h => h.Category)
            .Include(h => h.Verses)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            hymnsQuery = hymnsQuery.Where(h => h.Category.Slug == categorySlug);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            // Search by number or title
            if (int.TryParse(query, out int number))
            {
                hymnsQuery = hymnsQuery.Where(h =>
                    h.Number == number ||
                    h.Title.Contains(query));
            }
            else
            {
                hymnsQuery = hymnsQuery.Where(h => h.Title.Contains(query));
            }
        }

        var results = await hymnsQuery
            .OrderBy(h => h.Number)
            .Select(h => new HymnSearchResult
            {
                Id = h.Id,
                Number = h.Number,
                Title = h.Title,
                CategoryName = h.Category.Name,
                CategorySlug = h.Category.Slug,
                IsFavorite = h.IsFavorite,
                VerseCount = h.Verses.Count
            })
            .ToListAsync();

        return results;
    }

    public async Task<List<Hymn>> GetRecentHymnsAsync(int count = 10)
    {
        return await _context.Hymns
            .Include(h => h.Category)
            .OrderByDescending(h => h.LastAccessedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<Hymn>> GetFavoriteHymnsAsync()
    {
        return await _context.Hymns
            .Include(h => h.Category)
            .Where(h => h.IsFavorite)
            .OrderBy(h => h.Number)
            .ToListAsync();
    }

    public async Task AddToRecentAsync(int hymnId)
    {
        var hymn = await _context.Hymns.FindAsync(hymnId);
        if (hymn != null)
        {
            hymn.LastAccessedAt = DateTime.UtcNow;
            hymn.AccessCount++;
            await _context.SaveChangesAsync();
        }
    }

    public async Task ToggleFavoriteAsync(int hymnId)
    {
        var hymn = await _context.Hymns.FindAsync(hymnId);
        if (hymn != null)
        {
            hymn.IsFavorite = !hymn.IsFavorite;
            await _context.SaveChangesAsync();
        }
    }
}
```

### Step 2: Update Hymn Model

**File:** `src/SDAHymns.Core/Data/Models/Hymn.cs`

Add properties for tracking usage:

```csharp
public DateTime? LastAccessedAt { get; set; }
public int AccessCount { get; set; }
public bool IsFavorite { get; set; }
```

Create migration:
```bash
dotnet ef migrations add AddHymnUsageTracking --project src/SDAHymns.Core
dotnet ef database update --project src/SDAHymns.Core
```

### Step 3: Update ViewModel

**File:** `src/SDAHymns.Desktop/ViewModels/MainWindowViewModel.cs`

```csharp
[ObservableProperty]
private string _searchQuery = string.Empty;

[ObservableProperty]
private List<HymnSearchResult> _searchResults = new();

[ObservableProperty]
private HymnSearchResult? _selectedSearchResult;

[ObservableProperty]
private List<Hymn> _recentHymns = new();

private readonly ISearchService _searchService;

partial void OnSearchQueryChanged(string value)
{
    // Debounced search - only trigger after user stops typing
    _ = PerformSearchAsync();
}

private async Task PerformSearchAsync()
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
        StatusMessage = $"Loaded: {CurrentHymn.Number}. {CurrentHymn.Title}";
    }
}

[RelayCommand]
private async Task LoadRecentHymnsAsync()
{
    RecentHymns = await _searchService.GetRecentHymnsAsync(5);
}

[RelayCommand]
private async Task ToggleFavoriteAsync()
{
    if (CurrentHymn != null)
    {
        await _searchService.ToggleFavoriteAsync(CurrentHymn.Id);
        CurrentHymn.IsFavorite = !CurrentHymn.IsFavorite;
        OnPropertyChanged(nameof(FavoriteIcon));
    }
}

public string FavoriteIcon => CurrentHymn?.IsFavorite == true ? "⭐" : "☆";
```

### Step 4: Enhanced XAML Layout

**File:** `src/SDAHymns.Desktop/Views/MainWindow.axaml`

```xml
<Grid RowDefinitions="Auto,Auto,*,Auto,Auto,Auto">
    <!-- Row 0: Search & Controls -->
    <Grid Grid.Row="0" ColumnDefinitions="*,Auto,Auto,Auto,Auto">
        <TextBox Grid.Column="0"
                 Text="{Binding SearchQuery}"
                 Watermark="Search hymns by number or title..."
                 Margin="10"/>

        <ComboBox Grid.Column="1"
                  SelectedItem="{Binding SelectedCategory}"
                  Width="150" Margin="5"/>

        <Button Grid.Column="2"
                Content="{Binding FavoriteIcon}"
                Command="{Binding ToggleFavoriteCommand}"
                ToolTip.Tip="Toggle Favorite"/>

        <Button Grid.Column="3"
                Content="⚙"
                ToolTip.Tip="Settings"/>

        <Button Grid.Column="4"
                Content="{Binding DisplayWindowButtonLabel}"
                Command="{Binding ToggleDisplayCommand}"/>
    </Grid>

    <!-- Row 1: Recent Hymns -->
    <Border Grid.Row="1" Background="#1A1A1A" Padding="10">
        <StackPanel>
            <TextBlock Text="Recent:" Foreground="#888" FontSize="12"/>
            <ItemsRepeater ItemsSource="{Binding RecentHymns}">
                <ItemsRepeater.Layout>
                    <StackLayout Orientation="Horizontal" Spacing="5"/>
                </ItemsRepeater.Layout>
                <ItemsRepeater.ItemTemplate>
                    <DataTemplate>
                        <Button Content="{Binding Number}"
                                Command="{Binding LoadHymnCommand}"
                                CommandParameter="{Binding}"/>
                    </DataTemplate>
                </ItemsRepeater.ItemTemplate>
            </ItemsRepeater>
        </StackPanel>
    </Border>

    <!-- Row 2: Search Results List -->
    <ListBox Grid.Row="2"
             ItemsSource="{Binding SearchResults}"
             SelectedItem="{Binding SelectedSearchResult}"
             Background="#0D0D0D"
             Foreground="White">
        <ListBox.ItemTemplate>
            <DataTemplate>
                <Grid ColumnDefinitions="Auto,*,Auto">
                    <TextBlock Grid.Column="0"
                               Text="{Binding IsFavorite, Converter={StaticResource FavoriteConverter}}"
                               Margin="5,0"/>
                    <TextBlock Grid.Column="1">
                        <Run Text="{Binding Number}"/>
                        <Run Text=". "/>
                        <Run Text="{Binding Title}"/>
                    </TextBlock>
                    <TextBlock Grid.Column="2"
                               Text="{Binding VerseCount}"
                               Foreground="#888"
                               Margin="10,0"/>
                </Grid>
            </DataTemplate>
        </ListBox.ItemTemplate>
    </ListBox>

    <!-- Row 3: Selected Hymn Info -->
    <Border Grid.Row="3" Background="#1A1A1A" Padding="10">
        <StackPanel>
            <TextBlock Text="{Binding HymnTitle}" FontSize="16" FontWeight="Bold"/>
            <TextBlock Foreground="#888">
                <Run Text="{Binding CurrentHymn.Category.Name}"/>
                <Run Text=" • "/>
                <Run Text="{Binding Verses.Count}"/>
                <Run Text=" verses"/>
            </TextBlock>
        </StackPanel>
    </Border>

    <!-- Row 4: Verse Preview & Navigation -->
    <Viewbox Grid.Row="4" ...>
        <!-- Same as before -->
    </Viewbox>

    <!-- Row 5: Status Bar -->
    <Border Grid.Row="5" ...>
        <!-- Same as before -->
    </Border>
</Grid>
```

## Features

### 1. Real-Time Search
- **As-you-type filtering** - Results update instantly
- **Number search** - Type "20" to find hymn 20
- **Title search** - Type "slava" to find all hymns with "slava" in title
- **Category filtering** - Filter results by selected category

### 2. Browse Mode
- **Empty search** shows all hymns in selected category
- **Scrollable list** with hymn number, title, and verse count
- **Click to select** or use arrow keys + Enter

### 3. Recent Hymns
- **Top 5 recently used** hymns displayed as quick-access buttons
- **One-click loading** of frequently used hymns
- **Automatically tracked** when hymns are loaded

### 4. Favorites System
- **Star icon (⭐/☆)** to toggle favorite status
- **Filter to show only favorites** (future enhancement)
- **Persisted in database** (IsFavorite column)

### 5. Keyboard Navigation
- **Tab** - Navigate between search box, results, buttons
- **↑/↓** - Navigate search results
- **Enter** - Select hymn from results
- **Ctrl+F** - Focus search box
- **Esc** - Clear search
- **F5** - Toggle display window

## Database Changes

### Migration: AddHymnUsageTracking

```csharp
public partial class AddHymnUsageTracking : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "LastAccessedAt",
            table: "Hymns",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "AccessCount",
            table: "Hymns",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<bool>(
            name: "IsFavorite",
            table: "Hymns",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "LastAccessedAt", table: "Hymns");
        migrationBuilder.DropColumn(name: "AccessCount", table: "Hymns");
        migrationBuilder.DropColumn(name: "IsFavorite", table: "Hymns");
    }
}
```

## Testing Strategy

### Manual Testing
1. **Search by number**: Type "1", verify hymn 1 appears
2. **Search by title**: Type "slava", verify matching hymns appear
3. **Category filter**: Select "exploratori", verify only that category shows
4. **Recent hymns**: Load hymn, verify it appears in recent list
5. **Favorites**: Click star, verify it persists after restart
6. **Keyboard nav**: Use arrows and Enter to select hymns

### Unit Tests

```csharp
public class SearchServiceTests
{
    [Fact]
    public async Task SearchHymnsAsync_ByNumber_ReturnsMatchingHymn()
    {
        var result = await _searchService.SearchHymnsAsync("1", "crestine");
        Assert.Single(result);
        Assert.Equal(1, result[0].Number);
    }

    [Fact]
    public async Task SearchHymnsAsync_ByTitle_ReturnsMatchingHymns()
    {
        var result = await _searchService.SearchHymnsAsync("slava", null);
        Assert.NotEmpty(result);
        Assert.All(result, r => Assert.Contains("slava", r.Title, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ToggleFavoriteAsync_TogglesIsFavorite()
    {
        await _searchService.ToggleFavoriteAsync(1);
        var hymn = await _context.Hymns.FindAsync(1);
        Assert.True(hymn.IsFavorite);

        await _searchService.ToggleFavoriteAsync(1);
        hymn = await _context.Hymns.FindAsync(1);
        Assert.False(hymn.IsFavorite);
    }
}
```

## Performance Considerations

- **Debounce search** - Wait 300ms after typing stops before searching
- **Limit results** - Show max 100 results, add "Load More" if needed
- **Index database** - Add index on Title column for faster searches
- **Cache recent hymns** - Keep in memory, refresh on load

## Acceptance Criteria

- [ ] Search box filters hymns by number or title
- [ ] Results update in real-time as user types
- [ ] Can select hymn from list with mouse or keyboard
- [ ] Recent hymns displayed with quick access buttons
- [ ] Favorite star icon toggles and persists
- [ ] Keyboard navigation works (Tab, arrows, Enter)
- [ ] Category filter applies to search results
- [ ] Empty search shows all hymns in category
- [ ] Search works across all categories when "All" selected
- [ ] Loading a hymn updates recent list
- [ ] UI is responsive with large result sets (1000+ hymns)

## Future Enhancements (Phase 3)

- "Favorites only" filter toggle
- Sort options (by number, title, usage count)
- Advanced search (by lyrics content, not just title)
- Search history dropdown
- "Load All" button to show all verses at once
- Hymn statistics (times used, last used date)
- Export search results to CSV

## Related Specs

- **Previous:** 005-basic-hymn-display.md (basic UI foundation)
- **Next:** 007-display-profiles.md (customizable display styles)
- **Related:** 008-keyboard-shortcuts.md (global hotkeys)

## Notes

- Keep search simple and fast - users need to find hymns quickly during services
- Recent hymns are critical for workflow - most services reuse same hymns
- Keyboard navigation is essential for productivity
- Don't over-complicate - this is Phase 2, advanced features come later
