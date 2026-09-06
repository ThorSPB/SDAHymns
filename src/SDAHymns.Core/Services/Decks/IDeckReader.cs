namespace SDAHymns.Core.Services.Decks;

/// <summary>
/// Reads a hymn deck into ordered slide text.
/// </summary>
/// <remarks>
/// Implementations do structural extraction only - no interpretation of verses,
/// refrains or numbering. That is <see cref="HymnStructureParser"/>'s job, and keeping
/// the two apart is what makes the classification rules testable against fixed input.
/// </remarks>
public interface IDeckReader
{
    /// <summary>True if this reader recognises the file from its leading bytes.</summary>
    bool CanRead(ReadOnlySpan<byte> header);

    /// <summary>Reads the deck. Throws <see cref="DeckReadException"/> on malformed input.</summary>
    SlideDeck Read(string path);
}

/// <summary>Raised when a deck exists but cannot be understood.</summary>
public sealed class DeckReadException : Exception
{
    public DeckReadException(string message) : base(message) { }
    public DeckReadException(string message, Exception inner) : base(message, inner) { }
}
