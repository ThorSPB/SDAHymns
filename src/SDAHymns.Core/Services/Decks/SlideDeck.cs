namespace SDAHymns.Core.Services.Decks;

/// <summary>
/// The on-disk format of a hymn deck.
/// </summary>
/// <remarks>
/// The legacy library mixes both formats behind a single <c>.PPT</c> extension:
/// "Imnuri crestine" 737-920 are OOXML packages that were never renamed.
/// The format is therefore decided by the file's magic bytes, never its extension.
/// </remarks>
public enum DeckFormat
{
    /// <summary>PowerPoint 97-2003 binary format (OLE2 compound file).</summary>
    Ppt97,

    /// <summary>OOXML presentation (ZIP package), whatever the extension says.</summary>
    Ooxml
}

/// <summary>
/// A hymn deck reduced to ordered slide text. Carries no styling: the application
/// renders verses through its own <see cref="Data.Models.DisplayProfile"/>, so the
/// deck's fonts, colours and layout are deliberately discarded at this stage.
/// </summary>
/// <param name="Path">Absolute path the deck was read from.</param>
/// <param name="Format">Format the reader detected.</param>
/// <param name="Slides">Slide text in presentation order, one entry per slide.</param>
public sealed record SlideDeck(string Path, DeckFormat Format, IReadOnlyList<string> Slides);
