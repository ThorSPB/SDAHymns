using FluentAssertions;
using Moq;
using SDAHymns.Core.Data.Models;
using SDAHymns.Core.Services;
using SDAHymns.Desktop.ViewModels;

namespace SDAHymns.Tests.ViewModels;

/// <summary>
/// How a hymn reaches the screen: an opening title slide, then stanzas captioned with
/// their number - matching the decks the congregation is used to.
/// </summary>
public class HymnPresentationTests
{
    private readonly Mock<IHymnDisplayService> _hymns = new();
    private readonly Mock<IDisplayProfileService> _profiles = new();

    private static Hymn TheHymn => new()
    {
        Id = 1,
        Number = 471,
        Title = "Eu sunt un sol trimis",
        Category = new HymnCategory { Name = "Imnuri crestine", Slug = "crestine" },
    };

    private static List<Verse> TheVerses =>
    [
        new() { HymnId = 1, VerseNumber = 1, DisplayOrder = 1, Content = "Eu sunt un sol trimis" },
        new() { HymnId = 1, VerseNumber = 0, DisplayOrder = 2, Content = "V-aduc Cuvântul", Label = "Refren" },
        new() { HymnId = 1, VerseNumber = 2, DisplayOrder = 3, Content = "Din viața de păcat" },
        new() { HymnId = 1, VerseNumber = 0, DisplayOrder = 4, Content = "O continuare", IsContinuation = true },
    ];

    private async Task<MainWindowViewModel> LoadedAsync(bool showHymnTitle = true)
    {
        _hymns.Setup(s => s.GetHymnByNumberAsync(471, It.IsAny<string>())).ReturnsAsync(TheHymn);
        _hymns.Setup(s => s.GetVersesForHymnAsync(1)).ReturnsAsync(TheVerses);
        _profiles.Setup(s => s.GetAllProfilesAsync()).ReturnsAsync([]);

        var viewModel = new MainWindowViewModel(
            _hymns.Object, Mock.Of<IUpdateService>(), Mock.Of<ISearchService>(),
            _profiles.Object, Mock.Of<IAudioPlayerService>(), Mock.Of<ISettingsService>())
        {
            ActiveProfile = new DisplayProfile { Name = "Test", ShowHymnTitle = showHymnTitle },
            HymnNumber = 471,
        };

        await viewModel.LoadHymnAsync();
        return viewModel;
    }

    [Fact]
    public async Task A_hymn_opens_on_a_title_slide()
    {
        var viewModel = await LoadedAsync();

        viewModel.IsTitleSlide.Should().BeTrue();
        viewModel.HymnTitle.Should().Be("471. Eu sunt un sol trimis");
        viewModel.CurrentVerseContent.Should().BeNull("the opening slide carries no lyrics");
        viewModel.VerseIndicator.Should().Be("Title");
    }

    /// <summary>
    /// The regression that prompted this: the title band stayed up behind every stanza.
    /// </summary>
    [Fact]
    public async Task The_title_slide_gives_way_to_the_lyrics()
    {
        var viewModel = await LoadedAsync();

        viewModel.NextVerse();

        viewModel.IsTitleSlide.Should().BeFalse();
        viewModel.CurrentVerseContent.Should().Be("Eu sunt un sol trimis");
        viewModel.CurrentVerseIndex.Should().Be(0, "the title slide sits before stanza 1, not on it");
    }

    [Fact]
    public async Task Going_back_from_the_first_stanza_returns_to_the_title()
    {
        var viewModel = await LoadedAsync();
        viewModel.NextVerse();

        viewModel.PreviousVerse();

        viewModel.IsTitleSlide.Should().BeTrue();
        viewModel.CurrentVerseIndex.Should().Be(0);
    }

    [Fact]
    public async Task There_is_nothing_before_the_title_slide()
    {
        var viewModel = await LoadedAsync();

        viewModel.CanGoPrevious.Should().BeFalse();
    }

    [Fact]
    public async Task A_profile_can_drop_the_title_slide()
    {
        var viewModel = await LoadedAsync(showHymnTitle: false);

        viewModel.IsTitleSlide.Should().BeFalse();
        viewModel.CurrentVerseContent.Should().Be("Eu sunt un sol trimis");
    }

    /// <summary>
    /// The other regression: stanza numbers used to be part of the lyrics text, so moving
    /// them into a field of their own made them disappear from the screen.
    /// </summary>
    [Fact]
    public async Task A_stanza_is_captioned_with_its_number()
    {
        var viewModel = await LoadedAsync();
        viewModel.NextVerse();

        viewModel.CurrentVerseLabel.Should().Be("1.");
    }

    [Fact]
    public async Task A_refrain_is_captioned_Refren_not_a_number()
    {
        var viewModel = await LoadedAsync();
        viewModel.NextVerse();
        viewModel.NextVerse();

        viewModel.CurrentVerseLabel.Should().Be("Refren");
    }

    [Fact]
    public async Task Stanza_numbers_follow_the_deck_rather_than_the_slide_position()
    {
        var viewModel = await LoadedAsync();
        viewModel.NextVerse();
        viewModel.NextVerse();
        viewModel.NextVerse();

        // Third slide of lyrics, but stanza 2 - the refrain in between is not a stanza.
        viewModel.CurrentVerseLabel.Should().Be("2.");
    }

    [Fact]
    public async Task A_continuation_carries_no_caption()
    {
        var viewModel = await LoadedAsync();
        for (var i = 0; i < 4; i++)
        {
            viewModel.NextVerse();
        }

        viewModel.CurrentVerseLabel.Should().BeNull("it carries on from the slide before");
    }

    [Fact]
    public async Task Opening_a_hymn_from_the_remote_starts_at_the_title()
    {
        var viewModel = await LoadedAsync();

        await viewModel.LoadHymnDirectlyAsync(TheHymn);

        viewModel.IsTitleSlide.Should().BeTrue();
    }

    [Fact]
    public async Task Asking_the_remote_for_a_stanza_goes_straight_to_it()
    {
        var viewModel = await LoadedAsync();

        await viewModel.LoadHymnDirectlyAsync(TheHymn, verseIndex: 2);

        viewModel.IsTitleSlide.Should().BeFalse();
        viewModel.CurrentVerseContent.Should().Be("Din viața de păcat");
    }

    [Fact]
    public async Task The_title_slide_shows_no_caption()
    {
        var viewModel = await LoadedAsync();

        viewModel.CurrentVerseLabel.Should().BeNull();
    }
}
