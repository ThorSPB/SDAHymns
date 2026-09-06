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
        var pptDirectory = Path.Combine(categoryPath, "ppt");
        var directory = Directory.Exists(pptDirectory) ? pptDirectory : categoryPath;

        var files = Directory.EnumerateFiles(directory)
            .Where(IsDeck)
            .OrderBy(Path.GetFileName, StringComparer.Ordinal);

        var builder = new StringBuilder();
        builder.Append("# snapshot-format ").Append(FormatVersion).Append('\n');
        builder.Append("# category ").Append(Path.GetFileName(categoryPath)).Append('\n');

        foreach (var file in files)
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

    private static bool IsDeck(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".ppt", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".pps", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".pptx", StringComparison.OrdinalIgnoreCase);
    }
}
