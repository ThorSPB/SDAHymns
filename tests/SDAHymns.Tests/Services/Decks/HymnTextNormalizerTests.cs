using FluentAssertions;
using SDAHymns.Core.Services.Decks;

namespace SDAHymns.Tests.Services.Decks;

public class HymnTextNormalizerTests
{
    [Theory]
    [InlineData("Grăbiţi şi m-ascultaţi", "Grăbiți și m-ascultați")]   // cedilla -> comma
    [InlineData("Ţi-nalţ", "Ți-nalț")]
    public void Romanian_cedilla_letters_become_comma_letters(string input, string expected) =>
        HymnTextNormalizer.Normalize(input).Should().Be(expected);

    [Fact]
    public void Verse_numbers_glued_to_their_text_get_a_space()
    {
        // 124 hymns are typed this way.
        HymnTextNormalizer.Normalize("1.Iubirea Ta nemăsurată")
            .Should().Be("1. Iubirea Ta nemăsurată");
    }

    [Fact]
    public void A_correctly_spaced_verse_number_is_left_alone() =>
        HymnTextNormalizer.Normalize("2. Din viața de păcat,")
            .Should().Be("2. Din viața de păcat,");

    [Fact]
    public void Curly_quotes_and_dashes_are_folded() =>
        HymnTextNormalizer.Normalize("“Pentru Domnul!” – da’ nu")
            .Should().Be("\"Pentru Domnul!\" - da' nu");

    [Fact]
    public void Blank_lines_and_stray_whitespace_are_removed() =>
        HymnTextNormalizer.Normalize("  Prima   linie \n\n\n  A doua  \n ")
            .Should().Be("Prima linie\nA doua");

    /// <summary>
    /// Refrains are matched by comparing slide bodies, so the comparison form has to see
    /// through the casing and punctuation the decks disagree about.
    /// </summary>
    [Fact]
    public void Comparison_form_ignores_case_and_punctuation()
    {
        var shouty = HymnTextNormalizer.NormalizeForComparison("ROAGĂ-TE DIMINEAȚA,");
        var quiet = HymnTextNormalizer.NormalizeForComparison("Roagă-te dimineața");
        shouty.Should().Be(quiet);
    }

    [Fact]
    public void Comparison_form_sees_through_the_two_diacritic_spellings()
    {
        var cedilla = HymnTextNormalizer.NormalizeForComparison("Grăbiţi şi m-ascultaţi!");
        var comma = HymnTextNormalizer.NormalizeForComparison("Grăbiți și m-ascultați!");
        cedilla.Should().Be(comma);
    }

    [Fact]
    public void Empty_input_yields_empty_output() =>
        HymnTextNormalizer.Normalize("   \n  ").Should().BeEmpty();
}
