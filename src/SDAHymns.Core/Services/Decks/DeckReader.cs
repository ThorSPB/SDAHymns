namespace SDAHymns.Core.Services.Decks;

/// <summary>
/// Reads any hymn deck by dispatching on its magic bytes.
/// </summary>
/// <remarks>
/// Extension-based dispatch is not an option here: 184 of the 1256 decks in the legacy
/// library are OOXML packages named <c>.PPT</c>.
/// </remarks>
public sealed class DeckReader
{
    private const int HeaderSize = 8;

    private readonly IReadOnlyList<IDeckReader> _readers;

    public DeckReader() : this([new Ppt97Reader(), new OoxmlDeckReader()]) { }

    public DeckReader(IReadOnlyList<IDeckReader> readers) => _readers = readers;

    /// <summary>Reads a deck, choosing the reader that recognises the file.</summary>
    /// <exception cref="FileNotFoundException">The path does not exist.</exception>
    /// <exception cref="DeckReadException">No reader recognises it, or it is malformed.</exception>
    public SlideDeck Read(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Deck not found: {path}", path);
        }

        var header = new byte[HeaderSize];
        using (var stream = File.OpenRead(path))
        {
            var read = stream.ReadAtLeast(header, HeaderSize, throwOnEndOfStream: false);
            if (read < HeaderSize)
            {
                throw new DeckReadException($"File is too small to be a presentation: {path}");
            }
        }

        foreach (var reader in _readers)
        {
            if (reader.CanRead(header))
            {
                return reader.Read(path);
            }
        }

        var signature = Convert.ToHexString(header);
        throw new DeckReadException($"Unrecognised presentation format (starts {signature}): {path}");
    }
}
