namespace SDAHymns.Core.Services.Decks;

/// <summary>What a block of hymn text is.</summary>
public enum SectionKind
{
    /// <summary>A numbered stanza, or one recognised as a stanza and numbered afterwards.</summary>
    Verse,

    /// <summary>A refrain - either labelled "Refren", or identified by repeating between verses.</summary>
    Refrain,

    /// <summary>Text that carries on the section before it, typically a stanza split over two slides.</summary>
    Continuation
}

/// <summary>Where a hymn's number and title came from, so a bad import can be traced.</summary>
public enum TitleSource
{
    /// <summary>The category's index.xml - authoritative where it exists.</summary>
    LegacyIndex,

    /// <summary>The deck's own title slide.</summary>
    TitleSlide,

    /// <summary>The file name, which is always the hymn number.</summary>
    FileName
}

/// <param name="Kind">What this block is.</param>
/// <param name="Number">Stanza number; 0 for refrains and continuations.</param>
/// <param name="Content">The text, with any leading number or "Refren:" label removed.</param>
/// <param name="DisplayOrder">Position in the hymn as it is sung, starting at 1.</param>
/// <param name="WasNumbered">True if the deck stated the number rather than it being inferred.</param>
/// <param name="WasLabelled">True if the deck carried an explicit "Refren" label.</param>
public sealed record HymnSection(
    SectionKind Kind,
    int Number,
    string Content,
    int DisplayOrder,
    bool WasNumbered = false,
    bool WasLabelled = false);

/// <param name="Number">Hymn number.</param>
/// <param name="Title">Hymn title.</param>
/// <param name="Source">Where number and title came from.</param>
/// <param name="Sections">Sections in singing order.</param>
/// <param name="Notes">Anything the parser had to guess at, for review.</param>
public sealed record ParsedHymn(
    int Number,
    string Title,
    TitleSource Source,
    IReadOnlyList<HymnSection> Sections,
    IReadOnlyList<string> Notes);
