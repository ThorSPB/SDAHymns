using SDAHymns.Core.Services.Decks;

namespace SDAHymns.Core.Services;

/// <summary>
/// Data structure for extracted verse information
/// </summary>
public class VerseData
{
    public int VerseNumber { get; set; }
    public required string Content { get; set; }
    public string? Label { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsInline { get; set; }
    public bool IsContinuation { get; set; }
}

/// <summary>
/// Parses hymn titles and verses from the legacy PowerPoint library.
/// </summary>
/// <remarks>
/// <para>
/// Now a thin adapter over <see cref="DeckReader"/> and <see cref="HymnStructureParser"/>.
/// It used to convert every file to .pptx with a headless LibreOffice subprocess, which
/// meant an external installation was required just to construct the service, a 45s
/// timeout per file, and a silent skip whenever a conversion stalled.
/// </para>
/// <para>
/// The public shape is unchanged so <see cref="VerseImportService"/> and the CLI commands
/// did not have to move with it.
/// </para>
/// </remarks>
public class PowerPointParserService
{
    /// <summary>Label written for refrains; the display layer keys off it.</summary>
    public const string RefrainLabel = "Refren";

    private readonly DeckReader _reader = new();

    /// <summary>Reads a deck's hymn number and title.</summary>
    /// <returns>The number and title, or null if the deck cannot be read.</returns>
    public Task<(int hymnNumber, string title)?> ExtractHymnInfoAsync(string pptFilePath)
    {
        if (!File.Exists(pptFilePath))
        {
            throw new FileNotFoundException($"PPT file not found: {pptFilePath}");
        }

        try
        {
            var parsed = ParseDeck(pptFilePath);
            return Task.FromResult<(int, string)?>(
                parsed.Number > 0 ? (parsed.Number, parsed.Title) : null);
        }
        catch (DeckReadException)
        {
            return Task.FromResult<(int, string)?>(null);
        }
    }

    /// <summary>Reads a deck's verses and refrains, in singing order.</summary>
    /// <remarks>
    /// Every refrain occurrence is returned, not just the first. The importer used to drop
    /// repeats because the database made (HymnId, VerseNumber) unique and refrains all
    /// carry number 0; that constraint now applies to DisplayOrder instead.
    /// </remarks>
    public Task<List<VerseData>> ExtractVersesAsync(string pptFilePath)
    {
        if (!File.Exists(pptFilePath))
        {
            throw new FileNotFoundException($"PPT file not found: {pptFilePath}");
        }

        try
        {
            var parsed = ParseDeck(pptFilePath);
            var verses = parsed.Sections.Select(section => new VerseData
            {
                VerseNumber = section.Number,
                Content = section.Content,
                Label = section.Kind == SectionKind.Refrain ? RefrainLabel : null,
                DisplayOrder = section.DisplayOrder,
                IsInline = false,
                IsContinuation = section.Kind == SectionKind.Continuation,
            }).ToList();

            return Task.FromResult(verses);
        }
        catch (DeckReadException)
        {
            return Task.FromResult(new List<VerseData>());
        }
    }

    /// <summary>Reads and classifies a deck, using its category's index.xml when there is one.</summary>
    public ParsedHymn ParseDeck(string pptFilePath)
    {
        var deck = _reader.Read(pptFilePath);
        var parser = new HymnStructureParser(LoadIndexFor(pptFilePath));
        return parser.Parse(deck);
    }

    /// <summary>
    /// Decks live in <c>&lt;category&gt;/ppt/NNN.PPT</c>, so the index sits one level above
    /// the file's own folder.
    /// </summary>
    private static LegacyHymnIndex LoadIndexFor(string pptFilePath)
    {
        var categoryPath = Directory.GetParent(Path.GetFullPath(pptFilePath))?.Parent?.FullName;
        return categoryPath is null ? LegacyHymnIndex.Empty : LegacyHymnIndex.Load(categoryPath);
    }

    /// <summary>Reads many decks, reporting progress.</summary>
    public async Task<Dictionary<string, (int hymnNumber, string title)?>> ExtractBatchAsync(
        IEnumerable<string> pptFiles,
        IProgress<(int current, int total, string fileName)>? progress = null)
    {
        var results = new Dictionary<string, (int hymnNumber, string title)?>();
        var files = pptFiles.ToList();

        for (var i = 0; i < files.Count; i++)
        {
            progress?.Report((i + 1, files.Count, Path.GetFileName(files[i])));
            results[files[i]] = await ExtractHymnInfoAsync(files[i]);
        }

        return results;
    }
}
