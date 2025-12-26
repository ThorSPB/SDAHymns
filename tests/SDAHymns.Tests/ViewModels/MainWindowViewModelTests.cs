using FluentAssertions;
using Moq;
using NuGet.Versioning;
using SDAHymns.Core.Data.Models;
using SDAHymns.Core.Services;
using SDAHymns.Desktop.ViewModels;
using Velopack;
using Velopack.Sources;
using Xunit;

namespace SDAHymns.Tests.ViewModels;

public class MainWindowViewModelTests
{
    private readonly Mock<IHymnDisplayService> _mockHymnService;
    private readonly Mock<IUpdateService> _mockUpdateService;
    private readonly Mock<ISearchService> _mockSearchService;
    private readonly Mock<IDisplayProfileService> _mockProfileService;
    private readonly Mock<IAudioPlayerService> _mockAudioPlayer;
    private readonly Mock<ISettingsService> _mockSettingsService;

    public MainWindowViewModelTests()
    {
        _mockHymnService = new Mock<IHymnDisplayService>();
        _mockUpdateService = new Mock<IUpdateService>();
        _mockSearchService = new Mock<ISearchService>();
        _mockProfileService = new Mock<IDisplayProfileService>();
        _mockAudioPlayer = new Mock<IAudioPlayerService>();
        _mockSettingsService = new Mock<ISettingsService>();
    }

    [Fact]
    public void Constructor_InitializesWithNoUpdateAvailable()
    {
        // Arrange & Act
        var viewModel = new MainWindowViewModel(_mockHymnService.Object, _mockUpdateService.Object, _mockSearchService.Object, _mockProfileService.Object, _mockAudioPlayer.Object, _mockSettingsService.Object);

        // Assert
        viewModel.IsUpdateAvailable.Should().BeFalse();
        viewModel.LatestVersion.Should().BeNull();
        viewModel.IsDownloadingUpdate.Should().BeFalse();
        viewModel.DownloadProgress.Should().Be(0);
    }

    [Fact]
    public void ShowUpdateNotification_SetsPropertiesCorrectly()
    {
        // Arrange
        var viewModel = new MainWindowViewModel(_mockHymnService.Object, _mockUpdateService.Object, _mockSearchService.Object, _mockProfileService.Object, _mockAudioPlayer.Object, _mockSettingsService.Object);
        var mockUpdateInfo = CreateMockUpdateInfo("1.2.3");

        // Act
        viewModel.ShowUpdateNotification(mockUpdateInfo);

        // Assert
        viewModel.IsUpdateAvailable.Should().BeTrue();
        viewModel.LatestVersion.Should().Be("1.2.3");
    }

    [Fact]
    public void DismissUpdate_HidesBanner()
    {
        // Arrange
        var viewModel = new MainWindowViewModel(_mockHymnService.Object, _mockUpdateService.Object, _mockSearchService.Object, _mockProfileService.Object, _mockAudioPlayer.Object, _mockSettingsService.Object);
        var mockUpdateInfo = CreateMockUpdateInfo("1.2.3");
        viewModel.ShowUpdateNotification(mockUpdateInfo);

        // Act
        viewModel.DismissUpdateCommand.Execute(null);

        // Assert
        viewModel.IsUpdateAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateNowAsync_WithSuccessfulDownload_CallsApplyUpdatesAndRestart()
    {
        // Arrange
        var viewModel = new MainWindowViewModel(_mockHymnService.Object, _mockUpdateService.Object, _mockSearchService.Object, _mockProfileService.Object, _mockAudioPlayer.Object, _mockSettingsService.Object);
        var mockUpdateInfo = CreateMockUpdateInfo("1.2.3");
        viewModel.ShowUpdateNotification(mockUpdateInfo);

        _mockUpdateService
            .Setup(x => x.DownloadUpdatesAsync(It.IsAny<UpdateInfo>(), It.IsAny<IProgress<int>>()))
            .ReturnsAsync(true);

        _mockUpdateService
            .Setup(x => x.ApplyUpdatesAndRestart(It.IsAny<UpdateInfo>()));

        // Act
        await viewModel.UpdateNowCommand.ExecuteAsync(null);

        // Assert
        _mockUpdateService.Verify(
            x => x.DownloadUpdatesAsync(mockUpdateInfo, It.IsAny<IProgress<int>>()),
            Times.Once);

        _mockUpdateService.Verify(
            x => x.ApplyUpdatesAndRestart(mockUpdateInfo),
            Times.Once);
    }

    [Fact]
    public async Task UpdateNowAsync_WithFailedDownload_KeepsBannerVisible()
    {
        // Arrange
        var viewModel = new MainWindowViewModel(_mockHymnService.Object, _mockUpdateService.Object, _mockSearchService.Object, _mockProfileService.Object, _mockAudioPlayer.Object, _mockSettingsService.Object);
        var mockUpdateInfo = CreateMockUpdateInfo("1.2.3");
        viewModel.ShowUpdateNotification(mockUpdateInfo);

        _mockUpdateService
            .Setup(x => x.DownloadUpdatesAsync(It.IsAny<UpdateInfo>(), It.IsAny<IProgress<int>>()))
            .ReturnsAsync(false);

        // Act
        await viewModel.UpdateNowCommand.ExecuteAsync(null);

        // Assert - Banner should stay visible for retry (our UX fix!)
        viewModel.IsUpdateAvailable.Should().BeTrue();
        viewModel.IsDownloadingUpdate.Should().BeFalse();
        viewModel.StatusMessage.Should().Contain("Failed to download update");
    }

    [Fact]
    public async Task UpdateNowAsync_WithFailedDownload_ResetsDownloadingState()
    {
        // Arrange
        var viewModel = new MainWindowViewModel(_mockHymnService.Object, _mockUpdateService.Object, _mockSearchService.Object, _mockProfileService.Object, _mockAudioPlayer.Object, _mockSettingsService.Object);
        var mockUpdateInfo = CreateMockUpdateInfo("1.2.3");
        viewModel.ShowUpdateNotification(mockUpdateInfo);

        _mockUpdateService
            .Setup(x => x.DownloadUpdatesAsync(It.IsAny<UpdateInfo>(), It.IsAny<IProgress<int>>()))
            .ReturnsAsync(false);

        // Act
        await viewModel.UpdateNowCommand.ExecuteAsync(null);

        // Assert
        viewModel.IsDownloadingUpdate.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateNowAsync_WithNoPendingUpdate_DoesNothing()
    {
        // Arrange
        var viewModel = new MainWindowViewModel(_mockHymnService.Object, _mockUpdateService.Object, _mockSearchService.Object, _mockProfileService.Object, _mockAudioPlayer.Object, _mockSettingsService.Object);
        // Don't call ShowUpdateNotification - no pending update

        // Act
        await viewModel.UpdateNowCommand.ExecuteAsync(null);

        // Assert
        _mockUpdateService.Verify(
            x => x.DownloadUpdatesAsync(It.IsAny<UpdateInfo>(), It.IsAny<IProgress<int>>()),
            Times.Never);
    }

    [Fact]
    public void ShowUpdateNotification_WithDifferentVersions_UpdatesLatestVersion()
    {
        // Arrange
        var viewModel = new MainWindowViewModel(_mockHymnService.Object, _mockUpdateService.Object, _mockSearchService.Object, _mockProfileService.Object, _mockAudioPlayer.Object, _mockSettingsService.Object);

        // Act
        var update1 = CreateMockUpdateInfo("1.0.0");
        viewModel.ShowUpdateNotification(update1);
        var version1 = viewModel.LatestVersion;

        var update2 = CreateMockUpdateInfo("2.0.0");
        viewModel.ShowUpdateNotification(update2);
        var version2 = viewModel.LatestVersion;

        // Assert
        version1.Should().Be("1.0.0");
        version2.Should().Be("2.0.0");
    }

    [Fact]
    public void IsUpdateAvailable_AfterDismiss_BecomesFalse()
    {
        // Arrange
        var viewModel = new MainWindowViewModel(_mockHymnService.Object, _mockUpdateService.Object, _mockSearchService.Object, _mockProfileService.Object, _mockAudioPlayer.Object, _mockSettingsService.Object);
        var mockUpdateInfo = CreateMockUpdateInfo("1.2.3");
        viewModel.ShowUpdateNotification(mockUpdateInfo);
        viewModel.IsUpdateAvailable.Should().BeTrue();

        // Act
        viewModel.DismissUpdateCommand.Execute(null);

        // Assert
        viewModel.IsUpdateAvailable.Should().BeFalse();
    }

    // ========================================
    // Enhanced Slide Formatting Tests (Spec 018)
    // ========================================

    [Fact]
    public void NextVerse_OnLastVerse_WithEndingSlideEnabled_MovesToEndingSlide()
    {
        // Arrange
        var viewModel = CreateViewModelWithHymn(verseCount: 3);
        var profile = CreateProfileWithEndingSlide(enabled: true);
        _mockProfileService.Setup(x => x.GetActiveProfileAsync()).ReturnsAsync(profile);
        viewModel.ActiveProfile = profile;

        // Move to last verse
        viewModel.NextVerseCommand.Execute(null);
        viewModel.NextVerseCommand.Execute(null);
        viewModel.CurrentVerseIndex.Should().Be(2); // On last verse (index 2)

        // Act - Move past last verse
        viewModel.NextVerseCommand.Execute(null);

        // Assert
        viewModel.IsOnEndingSlide.Should().BeTrue();
        viewModel.CurrentVerseIndex.Should().Be(2); // Still on last verse index
    }

    [Fact]
    public void NextVerse_OnLastVerse_WithEndingSlideDisabled_DoesNotMoveToEndingSlide()
    {
        // Arrange
        var viewModel = CreateViewModelWithHymn(verseCount: 3);
        var profile = CreateProfileWithEndingSlide(enabled: false);
        viewModel.ActiveProfile = profile;

        // Move to last verse
        viewModel.NextVerseCommand.Execute(null);
        viewModel.NextVerseCommand.Execute(null);
        viewModel.CurrentVerseIndex.Should().Be(2);

        // Act - Try to move past last verse
        viewModel.NextVerseCommand.Execute(null);

        // Assert
        viewModel.IsOnEndingSlide.Should().BeFalse();
        viewModel.CurrentVerseIndex.Should().Be(2); // Still on last verse
    }

    [Fact]
    public void PreviousVerse_FromEndingSlide_ReturnsToLastVerse()
    {
        // Arrange
        var viewModel = CreateViewModelWithHymn(verseCount: 3);
        var profile = CreateProfileWithEndingSlide(enabled: true);
        viewModel.ActiveProfile = profile;

        // Move to ending slide
        viewModel.NextVerseCommand.Execute(null);
        viewModel.NextVerseCommand.Execute(null);
        viewModel.NextVerseCommand.Execute(null);
        viewModel.IsOnEndingSlide.Should().BeTrue();

        // Act - Go back from ending slide
        viewModel.PreviousVerseCommand.Execute(null);

        // Assert
        viewModel.IsOnEndingSlide.Should().BeFalse();
        viewModel.CurrentVerseIndex.Should().Be(2); // Back on last verse
    }

    [Fact]
    public void CanGoNext_OnLastVerse_WithEndingSlideEnabled_ReturnsTrue()
    {
        // Arrange
        var viewModel = CreateViewModelWithHymn(verseCount: 3);
        var profile = CreateProfileWithEndingSlide(enabled: true);
        viewModel.ActiveProfile = profile;

        // Move to last verse
        viewModel.NextVerseCommand.Execute(null);
        viewModel.NextVerseCommand.Execute(null);

        // Act & Assert
        viewModel.CanGoNext.Should().BeTrue(); // Can move to ending slide
    }

    [Fact]
    public void CanGoNext_OnEndingSlide_ReturnsFalse()
    {
        // Arrange
        var viewModel = CreateViewModelWithHymn(verseCount: 3);
        var profile = CreateProfileWithEndingSlide(enabled: true);
        viewModel.ActiveProfile = profile;

        // Move to ending slide
        viewModel.NextVerseCommand.Execute(null);
        viewModel.NextVerseCommand.Execute(null);
        viewModel.NextVerseCommand.Execute(null);

        // Act & Assert
        viewModel.CanGoNext.Should().BeFalse();
    }

    [Fact]
    public void CanGoPrevious_OnEndingSlide_ReturnsTrue()
    {
        // Arrange
        var viewModel = CreateViewModelWithHymn(verseCount: 3);
        var profile = CreateProfileWithEndingSlide(enabled: true);
        viewModel.ActiveProfile = profile;

        // Move to ending slide
        viewModel.NextVerseCommand.Execute(null);
        viewModel.NextVerseCommand.Execute(null);
        viewModel.NextVerseCommand.Execute(null);

        // Act & Assert
        viewModel.CanGoPrevious.Should().BeTrue();
    }

    [Fact]
    public void ShowTitleSlide_WithShowTitleOnFirstVerseOnlyFalse_NeverShowsTitleSlide()
    {
        // Arrange
        var viewModel = CreateViewModelWithHymn(verseCount: 3);
        var profile = new DisplayProfile { ShowTitleOnFirstVerseOnly = false, EnableBlackEndingSlide = false };
        viewModel.ActiveProfile = profile;

        // Act & Assert - Title slide never shown, go straight to verses
        viewModel.ShowTitleSlide.Should().BeFalse(); // No title slide
        viewModel.ShowVerseContent.Should().BeTrue(); // Show verse content immediately

        viewModel.NextVerseCommand.Execute(null);
        viewModel.ShowTitleSlide.Should().BeFalse();

        viewModel.NextVerseCommand.Execute(null);
        viewModel.ShowTitleSlide.Should().BeFalse();
    }

    [Fact]
    public void ShowTitleSlide_WithShowTitleOnFirstVerseOnlyTrue_ShowsTitleThenVerses()
    {
        // Arrange - Create profile first, then load hymn
        var profile = new DisplayProfile { ShowTitleOnFirstVerseOnly = true, EnableBlackEndingSlide = false };
        var viewModel = CreateViewModelWithHymnAndProfile(verseCount: 3, profile: profile);

        // Act & Assert - Title slide shown as separate first slide
        viewModel.ShowTitleSlide.Should().BeTrue(); // On title slide
        viewModel.ShowVerseContent.Should().BeFalse(); // No verse content yet

        viewModel.NextVerseCommand.Execute(null);
        viewModel.ShowTitleSlide.Should().BeFalse(); // First verse - no title
        viewModel.ShowVerseContent.Should().BeTrue();

        viewModel.NextVerseCommand.Execute(null);
        viewModel.ShowTitleSlide.Should().BeFalse(); // Second verse - no title
        viewModel.ShowVerseContent.Should().BeTrue();
    }

    [Fact]
    public void ShowTitleSlide_OnEndingSlide_AlwaysReturnsFalse()
    {
        // Arrange
        var viewModel = CreateViewModelWithHymn(verseCount: 3);
        var profile = CreateProfileWithEndingSlide(enabled: true);
        profile.ShowTitleOnFirstVerseOnly = false; // Title on all verses normally
        viewModel.ActiveProfile = profile;

        // Move to ending slide
        viewModel.NextVerseCommand.Execute(null);
        viewModel.NextVerseCommand.Execute(null);
        viewModel.NextVerseCommand.Execute(null);

        // Act & Assert
        viewModel.ShowTitleSlide.Should().BeFalse(); // No title on ending slide
    }

    [Fact]
    public void IsOnFirstVerse_OnFirstVerse_ReturnsTrue()
    {
        // Arrange
        var viewModel = CreateViewModelWithHymn(verseCount: 3);

        // Act & Assert
        viewModel.IsOnFirstVerse.Should().BeTrue();
    }

    [Fact]
    public void IsOnFirstVerse_OnSecondVerse_ReturnsFalse()
    {
        // Arrange
        var viewModel = CreateViewModelWithHymn(verseCount: 3);

        // Act
        viewModel.NextVerseCommand.Execute(null);

        // Assert
        viewModel.IsOnFirstVerse.Should().BeFalse();
    }

    [Fact]
    public void IsOnFirstVerse_OnEndingSlide_ReturnsFalse()
    {
        // Arrange
        var viewModel = CreateViewModelWithHymn(verseCount: 3);
        var profile = CreateProfileWithEndingSlide(enabled: true);
        viewModel.ActiveProfile = profile;

        // Move to ending slide
        viewModel.NextVerseCommand.Execute(null);
        viewModel.NextVerseCommand.Execute(null);
        viewModel.NextVerseCommand.Execute(null);

        // Act & Assert
        viewModel.IsOnFirstVerse.Should().BeFalse();
    }

    [Fact]
    public void OnVersesChanged_ResetsEndingSlideState()
    {
        // Arrange
        var viewModel = CreateViewModelWithHymn(verseCount: 3);
        var profile = CreateProfileWithEndingSlide(enabled: true);
        viewModel.ActiveProfile = profile;

        // Move to ending slide
        viewModel.NextVerseCommand.Execute(null);
        viewModel.NextVerseCommand.Execute(null);
        viewModel.NextVerseCommand.Execute(null);
        viewModel.IsOnEndingSlide.Should().BeTrue();

        // Act - Load a new hymn (changes Verses)
        var newHymn = new Hymn { Id = 2, Number = 2, Title = "Hymn 2" };
        var newVerses = new List<Verse>
        {
            new Verse { Id = 4, HymnId = 2, Label = "1.", Content = "Verse 1" },
            new Verse { Id = 5, HymnId = 2, Label = "2.", Content = "Verse 2" }
        };
        _mockHymnService.Setup(x => x.GetVersesForHymnAsync(2)).ReturnsAsync(newVerses);
        viewModel.LoadHymnDirectlyAsync(newHymn, 0).Wait();

        // Assert
        viewModel.IsOnEndingSlide.Should().BeFalse();
    }

    // ========================================
    // Helper Methods
    // ========================================

    /// <summary>
    /// Creates a ViewModel with a hymn loaded (for testing verse navigation)
    /// </summary>
    private MainWindowViewModel CreateViewModelWithHymn(int verseCount)
    {
        return CreateViewModelWithHymnAndProfile(verseCount, null);
    }

    /// <summary>
    /// Creates a ViewModel with a hymn loaded and a specific profile set
    /// </summary>
    private MainWindowViewModel CreateViewModelWithHymnAndProfile(int verseCount, DisplayProfile? profile)
    {
        var hymn = new Hymn { Id = 1, Number = 1, Title = "Test Hymn" };
        var verses = new List<Verse>();
        for (int i = 0; i < verseCount; i++)
        {
            verses.Add(new Verse { Id = i + 1, HymnId = 1, Label = $"{i + 1}.", Content = $"Verse {i + 1}" });
        }

        _mockHymnService.Setup(x => x.GetVersesForHymnAsync(1)).ReturnsAsync(verses);
        _mockProfileService.Setup(x => x.GetActiveProfileAsync()).ReturnsAsync(profile);

        var viewModel = new MainWindowViewModel(
            _mockHymnService.Object,
            _mockUpdateService.Object,
            _mockSearchService.Object,
            _mockProfileService.Object,
            _mockAudioPlayer.Object,
            _mockSettingsService.Object);

        // Set profile BEFORE loading hymn (so OnVersesChanged sees it)
        if (profile != null)
        {
            viewModel.ActiveProfile = profile;
        }

        // Load the hymn
        viewModel.LoadHymnDirectlyAsync(hymn, 0).Wait();

        return viewModel;
    }

    /// <summary>
    /// Creates a DisplayProfile with ending slide configuration
    /// </summary>
    private static DisplayProfile CreateProfileWithEndingSlide(bool enabled)
    {
        return new DisplayProfile
        {
            EnableBlackEndingSlide = enabled,
            EndingSlideAutoCloseDuration = enabled ? 10 : 0
        };
    }

    /// <summary>
    /// Helper method to create a mock UpdateInfo for testing
    /// </summary>
    private static UpdateInfo CreateMockUpdateInfo(string version)
    {
        // Create a minimal UpdateInfo for testing purposes
        // In real scenarios, this comes from Velopack's GitHub check
        var nugetVersion = NuGetVersion.Parse(version);
        var release = new VelopackAsset
        {
            PackageId = "SDAHymns",
            Version = nugetVersion,
            Type = VelopackAssetType.Full,
            FileName = $"SDAHymns-{version}-full.nupkg",
            SHA1 = "mock-sha1",
            Size = 1024
        };

        return new UpdateInfo(release, false);
    }
}
