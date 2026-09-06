using FluentAssertions;
using SDAHymns.Core.Services.Decks;

namespace SDAHymns.Tests.Services.Decks;

public class HymnStructureParserTests
{
    private static SlideDeck Deck(string fileName, params string[] slides) =>
        new(Path.Combine("ppt", fileName), DeckFormat.Ppt97, slides);

    private static ParsedHymn Parse(SlideDeck deck) => new HymnStructureParser().Parse(deck);

    // ---------- refrains ----------

    /// <summary>
    /// The rule the old parser was missing. 505 refrain slides in the library carry no
    /// label at all - the only thing marking them is that they come round again.
    /// </summary>
    [Fact]
    public void An_unlabelled_block_that_repeats_between_verses_is_a_refrain()
    {
        var refrain = "Să fii tânăr, să fii liber,\nBun prieten cu Isus!";
        var hymn = Parse(Deck("005.PPT",
            "5. Să fii tânăr",
            refrain,
            "1. Pe cărarea vieții, vesel ai pornit,",
            refrain,
            "2. Dacă încercări și griji te vor lovi,",
            refrain));

        hymn.Sections.Where(s => s.Kind == SectionKind.Refrain).Should().HaveCount(3);
        hymn.Sections.Should().OnlyContain(s => s.Kind != SectionKind.Continuation);
        hymn.Notes.Should().Contain(n => n.Contains("by repetition", StringComparison.OrdinalIgnoreCase)
                                      || n.Contains("repetition"));
    }

    [Fact]
    public void A_block_that_appears_once_is_not_a_refrain()
    {
        var hymn = Parse(Deck("100.PPT",
            "100. Un imn",
            "1. Prima strofă a imnului",
            "O linie care apare o singură dată"));

        hymn.Sections.Should().NotContain(s => s.Kind == SectionKind.Refrain);
    }

    [Theory]
    [InlineData("Refren:")]
    [InlineData("Refren :")]
    [InlineData("REFREN:")]
    [InlineData("Refren")]
    [InlineData("Refrenul:")]
    public void Every_spelling_of_the_label_is_recognised(string label)
    {
        var hymn = Parse(Deck("200.PPT",
            "200. Titlu",
            "1. Prima strofă",
            $"{label}\nCântăm cu bucurie"));

        var refrain = hymn.Sections.Single(s => s.Kind == SectionKind.Refrain);
        refrain.WasLabelled.Should().BeTrue();
        refrain.Content.Should().Be("Cântăm cu bucurie");
    }

    /// <summary>Six decks run the label straight into the first line of the refrain.</summary>
    [Fact]
    public void A_label_glued_to_the_first_line_still_splits_off()
    {
        var hymn = Parse(Deck("201.PPT",
            "201. Titlu",
            "1. Prima strofă",
            "Refren: O, minunat Cântec\nA doua linie"));

        var refrain = hymn.Sections.Single(s => s.Kind == SectionKind.Refrain);
        refrain.Content.Should().Be("O, minunat Cântec\nA doua linie");
    }

    /// <summary>An all-caps refrain and its sentence-case twin are the same refrain.</summary>
    [Fact]
    public void Repetition_is_matched_through_casing_and_punctuation()
    {
        var hymn = Parse(Deck("021.ppt",
            "21. Roagă-te!",
            "ROAGĂ-TE DIMINEAȚA,",
            "1. Cu Domnul dimineața",
            "Roagă-te dimineața"));

        hymn.Sections.Where(s => s.Kind == SectionKind.Refrain).Should().HaveCount(2);
    }

    /// <summary>
    /// "Cor" and "Refrenul" begin ordinary lyric lines. Treating them as labels split
    /// stanzas and ate the first word - "Coroane multe s-adunăm" projected as "oane
    /// multe s-adunăm".
    /// </summary>
    [Theory]
    [InlineData("Cor de îngeri cântă:")]
    [InlineData("Coroane multe s-adunăm")]
    [InlineData("Refrenul lor răsuna peste ape")]
    public void A_lyric_line_that_merely_starts_with_a_label_word_is_not_a_label(string line)
    {
        var hymn = Parse(Deck("202.PPT", "202. Titlu", $"1. Prima linie\n{line}\nUltima linie"));

        var verse = hymn.Sections.Single();
        verse.Kind.Should().Be(SectionKind.Verse);
        verse.Content.Should().Contain(line);
    }

    // ---------- slides that hold more than one section ----------

    /// <summary>125 slides in the library carry two sections.</summary>
    [Fact]
    public void One_slide_can_hold_several_sections()
    {
        var hymn = Parse(Deck("300.PPT",
            "300. Titlu",
            "1. Prima strofă\nRefren:\nRefrenul imnului\n2. A doua strofă"));

        hymn.Sections.Select(s => s.Kind).Should().Equal(
            SectionKind.Verse, SectionKind.Refrain, SectionKind.Verse);
    }

    /// <summary>A stanza split over two slides stays one stanza followed by its continuation.</summary>
    [Fact]
    public void A_stanza_split_across_slides_becomes_a_continuation()
    {
        var hymn = Parse(Deck("301.PPT",
            "301. Titlu",
            "1. Prima jumătate a strofei",
            "A doua jumătate, fără număr"));

        hymn.Sections.Select(s => s.Kind).Should().Equal(SectionKind.Verse, SectionKind.Continuation);
    }

    // ---------- numbering ----------

    [Fact]
    public void A_single_unlabelled_slide_filling_a_skipped_number_becomes_that_stanza()
    {
        var hymn = Parse(Deck("400.PPT",
            "400. Titlu",
            "1. Prima strofă",
            "Strofa care și-a pierdut numărul",
            "3. A treia strofă"));

        var verses = hymn.Sections.Where(s => s.Kind == SectionKind.Verse).ToList();
        verses.Select(v => v.Number).Should().Equal(1, 2, 3);
        verses[1].WasNumbered.Should().BeFalse();
    }

    /// <summary>
    /// When several slides could be the missing stanza, captioning one of them would be a
    /// guess. They stay continuations and the hymn records why.
    /// </summary>
    [Fact]
    public void An_ambiguous_gap_is_reported_rather_than_guessed()
    {
        var hymn = Parse(Deck("006.PPT",
            "6. Ziua trece",
            "1. Ziua trece ușor",
            "Rândunele-n cuibul lor",
            "Rugăciuni, glas de dor",
            "3. Liniștit, poți vedea"));

        hymn.Sections.Count(s => s.Kind == SectionKind.Continuation).Should().Be(2);
        hymn.Notes.Should().Contain(n => n.Contains("left unnumbered"));
    }

    [Fact]
    public void Stated_numbers_are_kept_even_when_they_do_not_start_at_one()
    {
        // 134 hymns in the library begin at something other than 1.
        var hymn = Parse(Deck("500.PPT", "500. Titlu", "4. A patra strofă", "5. A cincea strofă"));

        hymn.Sections.Select(s => s.Number).Should().Equal(4, 5);
    }

    [Fact]
    public void A_repeated_stanza_number_is_reassigned_so_ordering_stays_usable()
    {
        var hymn = Parse(Deck("501.PPT", "501. Titlu", "1. Prima", "1. A doua, greșit numerotată"));

        hymn.Sections.Select(s => s.Number).Should().OnlyHaveUniqueItems();
        hymn.Notes.Should().Contain(n => n.Contains("reassigned"));
    }

    /// <summary>248 hymns never state a number; their slides are still stanzas.</summary>
    [Fact]
    public void A_hymn_with_no_numbers_at_all_still_yields_stanzas()
    {
        var hymn = Parse(Deck("010.PPT",
            "10. Doxologie",
            "Slăvim pe Dumnezeu,\nPărintele Sfânt!",
            "A doua strofă fără număr"));

        hymn.Sections.Should().OnlyContain(s => s.Kind == SectionKind.Verse);
        hymn.Sections.Select(s => s.Number).Should().Equal(1, 2);
    }

    [Fact]
    public void Display_order_is_dense_and_starts_at_one()
    {
        var hymn = Parse(Deck("600.PPT", "600. Titlu", "1. Una", "Refren:\nCântăm mereu", "2. Două"));

        hymn.Sections.Select(s => s.DisplayOrder).Should().Equal(1, 2, 3);
    }

    // ---------- title and number ----------

    [Fact]
    public void The_number_comes_from_the_file_name()
    {
        Parse(Deck("0422.PPT", "422. Venim din Cuvântul", "1. O strofă")).Number.Should().Be(422);
    }

    [Fact]
    public void A_title_slide_is_not_mistaken_for_a_stanza()
    {
        var hymn = Parse(Deck("471.PPT", "471. Eu sunt\nun sol trimis", "1. Eu sunt un sol trimis"));

        hymn.Sections.Should().HaveCount(1);
        hymn.Title.Should().Be("Eu sunt un sol trimis");
        hymn.Source.Should().Be(TitleSource.TitleSlide);
    }

    /// <summary>The 184 newest decks title themselves "Imnul N" instead of "N.".</summary>
    [Fact]
    public void The_Imnul_title_style_is_understood()
    {
        var hymn = Parse(Deck("737.PPT", "Imnul 737\nBunul nostru Salvator", "1. Bunul nostru Salvator,"));

        hymn.Number.Should().Be(737);
        hymn.Title.Should().Be("Bunul nostru Salvator");
    }

    [Fact]
    public void A_long_first_slide_is_lyrics_not_a_title()
    {
        var hymn = Parse(Deck("700.PPT",
            "1. Linia unu\nLinia doi\nLinia trei\nLinia patru\nLinia cinci"));

        hymn.Sections.Should().HaveCount(1);
        hymn.Notes.Should().Contain(n => n.Contains("no title slide"));
    }

    [Fact]
    public void The_legacy_index_wins_over_the_title_slide()
    {
        var deck = Deck("001.PPT", "1. Ceva scris greșit pe slide", "1. O strofă");
        var index = LegacyHymnIndex.Load(WriteIndex(1, "Spre slava Ta uniți"));

        var hymn = new HymnStructureParser(index).Parse(deck);

        hymn.Title.Should().Be("Spre slava Ta uniți");
        hymn.Source.Should().Be(TitleSource.LegacyIndex);
    }

    [Fact]
    public void Titles_from_the_index_are_normalised_too()
    {
        var index = LegacyHymnIndex.Load(WriteIndex(1, "Spre slava Ta uniţi"));   // cedilla
        var hymn = new HymnStructureParser(index).Parse(Deck("001.PPT", "1. Titlu", "1. Strofă"));

        hymn.Title.Should().Be("Spre slava Ta uniți");                            // comma
    }

    private static string WriteIndex(int number, string title)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"hymn-index-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "index.xml"),
            $"<Imnuri><Imn><Numar>{number}</Numar><Titlu>{title}</Titlu></Imn></Imnuri>");
        return directory;
    }
}
