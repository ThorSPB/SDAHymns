using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SDAHymns.Core.Services.Decks;

/// <summary>
/// Normalises hymn text so that decks written across three decades of PowerPoint
/// compare and display consistently.
/// </summary>
/// <remarks>
/// Nothing here touches meaning - it only removes the incidental differences that
/// otherwise make identical text look different, which matters because refrains are
/// detected by comparing slide bodies to each other.
/// </remarks>
public static partial class HymnTextNormalizer
{
    /// <summary>
    /// Romanian s-comma and t-comma were widely typed as the Turkish cedilla forms
    /// before the 2007 orthography settled. Both spellings appear in this library,
    /// sometimes inside the same hymn.
    /// </summary>
    private static readonly (char Cedilla, char Comma)[] RomanianLetters =
    [
        ('ş', 'ș'),   // s-cedilla  -> s-comma
        ('Ş', 'Ș'),   // S-cedilla  -> S-comma
        ('ţ', 'ț'),   // t-cedilla  -> t-comma
        ('Ţ', 'Ț'),   // T-cedilla  -> T-comma
    ];

    /// <summary>Full normalisation, used for storage and display.</summary>
    public static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var value = text.Normalize(NormalizationForm.FormC);

        foreach (var (cedilla, comma) in RomanianLetters)
        {
            value = value.Replace(cedilla, comma);
        }

        value = QuotesAndDashes(value);
        value = NonBreakingSpace().Replace(value, " ");
        value = VerseNumberSpacing().Replace(value, "$1. ");

        var lines = value
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Replace('\v', '\n')
            .Split('\n')
            .Select(line => CollapseSpaces().Replace(line, " ").Trim())
            .Where(line => line.Length > 0);

        return string.Join("\n", lines);
    }

    /// <summary>
    /// A stricter form used only to decide whether two slides carry the same words.
    /// Case and punctuation are dropped so that an all-caps refrain still matches its
    /// sentence-case twin.
    /// </summary>
    public static string NormalizeForComparison(string text)
    {
        var value = Normalize(text).ToLower(RomanianCulture);
        value = Punctuation().Replace(value, "");
        return CollapseSpaces().Replace(value, " ").Trim();
    }

    private static readonly CultureInfo RomanianCulture = CultureInfo.GetCultureInfo("ro-RO");

    private static string QuotesAndDashes(string value) => value
        .Replace('‘', '\'').Replace('’', '\'')     // curly single quotes
        .Replace('“', '"').Replace('”', '"')       // curly double quotes
        .Replace('„', '"').Replace('‟', '"')       // low/high double quotes
        .Replace('–', '-').Replace('—', '-')       // en/em dash
        .Replace("…", "...");                           // ellipsis

    /// <summary>Matches "1.Text" written without a space, which 124 hymns do.</summary>
    [GeneratedRegex(@"^(\d+)\.(?=\S)", RegexOptions.Multiline)]
    private static partial Regex VerseNumberSpacing();

    [GeneratedRegex(@"[ \t ]+")]
    private static partial Regex CollapseSpaces();

    [GeneratedRegex(@" ")]
    private static partial Regex NonBreakingSpace();

    [GeneratedRegex(@"[\p{P}\p{S}]")]
    private static partial Regex Punctuation();
}
