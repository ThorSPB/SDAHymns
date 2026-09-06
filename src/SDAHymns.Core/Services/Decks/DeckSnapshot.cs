using System.Text;

namespace SDAHymns.Core.Services.Decks;

/// <summary>
/// Renders decks as stable text so parser changes can be reviewed as a diff.
/// </summary>
/// <remarks>
/// The hymn library has 1256 decks and no two of them are quite alike. Without a frozen
/// baseline, fixing one edge case silently changes dozens of other hymns and nobody
/// notices until a service. Regenerating this snapshot and reading <c>git diff</c> is
/// the check that makes the classifier safe to touch.
/// </remarks>
public static class DeckSnapshot
{
    /// <summary>Bumped whenever the snapshot layout itself changes, so stale files are obvious.</summary>
    public const string FormatVersion = "1";

    /// <summary>
    /// Renders the classified structure of one hymn: what the parser decided each block
    /// of text is, which is the part that actually reaches the screen.
    /// </summary>
    public static string RenderStructure(string name, ParsedHymn hymn)
    {
        var builder = new StringBuilder();
        builder.Append("### ").Append(name)
               .Append(" | #").Append(hymn.Number)
               .Append(" | ").Append(hymn.Title)
               .Append(" | title-from=").Append(hymn.Source)
               .Append('\n');

        foreach (var note in hymn.Notes)
        {
            builder.Append("note: ").Append(note).Append('\n');
        }

        foreach (var section in hymn.Sections)
        {
            builder.Append("--- ").Append(section.DisplayOrder).Append(' ').Append(section.Kind);
            if (section.Kind == SectionKind.Verse)
            {
                builder.Append(' ').Append(section.Number);
                if (!section.WasNumbered)
                {
                    builder.Append(" (inferred)");
                }
            }
            else if (section.Kind == SectionKind.Refrain && !section.WasLabelled)
            {
                builder.Append(" (by repetition)");
            }

            builder.Append('\n').Append(section.Content).Append('\n');
        }

        return builder.ToString();
    }

    /// <summary>Renders the classified structure of every deck in one category.</summary>
    public static string RenderStructureCategory(string categoryPath, DeckReader reader)
    {
        var parser = new HymnStructureParser(LegacyHymnIndex.Load(categoryPath));
        var builder = new StringBuilder();
        builder.Append("# snapshot-format ").Append(FormatVersion.AsSpan()).Append('\n');
        builder.Append("# category ").Append(Path.GetFileName(categoryPath)).Append('\n');

        foreach (var file in DeckFiles(categoryPath))
        {
            var name = Path.GetFileName(file);
            try
            {
                builder.Append(RenderStructure(name, parser.Parse(reader.Read(file))));
            }
            catch (Exception ex)
            {
                builder.Append(RenderFailure(name, ex));
            }
        }

        return builder.ToString();
    }

    /// <summary>Renders one deck. <paramref name="name"/> identifies it in the snapshot.</summary>
    public static string Render(string name, SlideDeck deck)
    {
        var builder = new StringBuilder();
        builder.Append("### ").Append(name)
               .Append(" | format=").Append(deck.Format)
               .Append(" | slides=").Append(deck.Slides.Count)
               .Append('\n');

        for (var i = 0; i < deck.Slides.Count; i++)
        {
            builder.Append("--- slide ").Append(i + 1).Append('\n');
            var text = HymnTextNormalizer.Normalize(deck.Slides[i]);
            if (text.Length > 0)
            {
                builder.Append(text).Append('\n');
            }
        }

        return builder.ToString();
    }

    /// <summary>Renders a failure the same way, so an unreadable deck shows up in the diff too.</summary>
    public static string RenderFailure(string name, Exception error) =>
        $"### {name} | ERROR\n{error.GetType().Name}: {error.Message}\n";

    /// <summary>
    /// Renders every deck under one category directory, ordered by file name so the
    /// output does not depend on the filesystem's enumeration order.
    /// </summary>
    public static string RenderCategory(string categoryPath, DeckReader reader)
    {
        var builder = new StringBuilder();
        builder.Append("# snapshot-format ").Append(FormatVersion.AsSpan()).Append('\n');
        builder.Append("# category ").Append(Path.GetFileName(categoryPath)).Append('\n');

        foreach (var file in DeckFiles(categoryPath))
        {
            var name = Path.GetFileName(file);
            try
            {
                builder.Append(Render(name, reader.Read(file)));
            }
            catch (Exception ex)
            {
                builder.Append(RenderFailure(name, ex));
            }
        }

        return builder.ToString();
    }

    /// <summary>Decks in a stable order, so output never depends on filesystem enumeration.</summary>
    private static IEnumerable<string> DeckFiles(string categoryPath)
    {
        var pptDirectory = Path.Combine(categoryPath, "ppt");
        var directory = Directory.Exists(pptDirectory) ? pptDirectory : categoryPath;

        return Directory.EnumerateFiles(directory)
            .Where(IsDeck)
            .OrderBy(Path.GetFileName, StringComparer.Ordinal);
    }

    private static bool IsDeck(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".ppt", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".pps", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".pptx", StringComparison.OrdinalIgnoreCase);
    }
}
