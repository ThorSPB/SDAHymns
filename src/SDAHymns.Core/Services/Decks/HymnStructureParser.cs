using System.Text.RegularExpressions;

namespace SDAHymns.Core.Services.Decks;

/// <summary>
/// Turns a deck's slide text into a hymn: number, title, and sections in singing order.
/// </summary>
/// <remarks>
/// <para>
/// The rules here are shaped by what the 1256 decks in the library actually do, which is
/// not what a hymnbook would lead you to expect:
/// </para>
/// <list type="bullet">
///   <item>
///     Only 543 refrain slides say "Refren". Another 505 are the refrain repeated between
///     verses with no label at all, so <b>repetition is the primary signal and the label
///     is the secondary one</b> - the reverse of the obvious design.
///   </item>
///   <item>
///     134 hymns do not start at verse 1, 10 repeat a number and 4 skip one, so stated
///     numbers are a hint, not a source of truth.
///   </item>
///   <item>
///     125 slides hold more than one section, so a slide is not a unit of meaning.
///   </item>
/// </list>
/// </remarks>
public sealed partial class HymnStructureParser
{
    /// <summary>A title slide is short. Verse slides in this library run to eight lines or more.</summary>
    private const int MaxTitleSlideLines = 4;

    private readonly LegacyHymnIndex _index;

    public HymnStructureParser(LegacyHymnIndex? index = null) => _index = index ?? LegacyHymnIndex.Empty;

    public ParsedHymn Parse(SlideDeck deck)
    {
        var notes = new List<string>();
        var slides = deck.Slides.Select(HymnTextNormalizer.Normalize).ToList();

        var number = NumberFromFileName(deck.Path);
        var (title, source, bodyStart) = ResolveTitle(slides, number, notes);
        var segments = SplitIntoBlocks(slides.Skip(bodyStart));
        var sections = Classify(segments, notes);

        // The same observation can arise from several slides; report it once.
        return new ParsedHymn(number, title, source, sections, notes.Distinct().ToList());
    }

    // ---------- number and title ----------

    /// <summary>The file name is the one identifier every deck agrees on.</summary>
    private static int NumberFromFileName(string path)
    {
        var match = LeadingDigits().Match(Path.GetFileNameWithoutExtension(path));
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }

    /// <summary>
    /// Prefers index.xml, falls back to the title slide, then to the number alone.
    /// Also reports how many leading slides are titles rather than lyrics.
    /// </summary>
    private (string Title, TitleSource Source, int BodyStart) ResolveTitle(
        List<string> slides, int number, List<string> notes)
    {
        var first = slides.Count > 0 ? slides[0] : "";
        var isTitleSlide = LooksLikeTitleSlide(first);
        var bodyStart = isTitleSlide ? 1 : 0;

        if (!isTitleSlide && slides.Count > 0)
        {
            notes.Add("no title slide; first slide treated as lyrics");
        }

        if (number > 0 && _index.TryGetTitle(number, out var indexed))
        {
            return (indexed, TitleSource.LegacyIndex, bodyStart);
        }

        if (isTitleSlide)
        {
            var fromSlide = TitleFromSlide(first);
            if (fromSlide.Length > 0)
            {
                notes.Add("title taken from the deck; not in index.xml");
                return (fromSlide, TitleSource.TitleSlide, bodyStart);
            }
        }

        notes.Add("no usable title; falling back to the hymn number");
        return (number > 0 ? $"Imnul {number}" : "(fără titlu)", TitleSource.FileName, bodyStart);
    }

    private static bool LooksLikeTitleSlide(string slide)
    {
        if (slide.Length == 0)
        {
            return true;   // an empty leading slide is a title slide with nothing on it
        }

        var lines = slide.Split('\n');
        if (lines.Length > MaxTitleSlideLines)
        {
            return false;
        }

        // Both title conventions in the library, plus decks whose first slide is the
        // bare hymn number.
        return ImnulTitle().IsMatch(slide) || NumberedLine().IsMatch(lines[0]) || BareNumber().IsMatch(lines[0]);
    }

    /// <summary>Strips "Imnul 737" / "422." from the title slide, leaving the name.</summary>
    private static string TitleFromSlide(string slide)
    {
        var text = ImnulTitle().Replace(slide, "");
        text = NumberedLine().Replace(text, "");
        return string.Join(" ", text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(l => l.Trim())).Trim();
    }

    // ---------- segmentation ----------

    private sealed record Block(string Head, List<string> Lines, int? Number, bool Labelled);

    /// <summary>
    /// Splits slides into sections. A slide can hold several, and one section can run
    /// across two slides, so slide boundaries only start a new segment when the text
    /// itself does not say otherwise.
    /// </summary>
    private static List<Block> SplitIntoBlocks(IEnumerable<string> slides)
    {
        var segments = new List<Block>();

        foreach (var slide in slides)
        {
            var lines = slide.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                             .Select(l => l.Trim())
                             .Where(l => l.Length > 0)
                             .ToList();
            if (lines.Count == 0)
            {
                continue;
            }

            Block? current = null;
            foreach (var line in lines)
            {
                var verse = NumberedLine().Match(line);
                var refrain = RefrainLabel().Match(line);

                if (verse.Success || refrain.Success)
                {
                    if (current is not null)
                    {
                        segments.Add(current);
                    }

                    var remainder = line[(verse.Success ? verse.Length : refrain.Length)..].Trim();
                    current = new Block(
                        line,
                        remainder.Length > 0 ? [remainder] : [],
                        verse.Success ? int.Parse(verse.Groups[1].Value) : null,
                        refrain.Success);
                }
                else if (current is null)
                {
                    current = new Block(line, [line], null, false);
                }
                else
                {
                    current.Lines.Add(line);
                }
            }

            if (current is not null)
            {
                segments.Add(current);
            }
        }

        return segments;
    }

    // ---------- classification ----------

    private static List<HymnSection> Classify(List<Block> segments, List<string> notes)
    {
        var keys = segments.Select(s => HymnTextNormalizer.NormalizeForComparison(string.Join("\n", s.Lines)))
                           .ToList();

        // Repetition is what identifies an unlabelled refrain. Only unnumbered, unlabelled
        // text is eligible: a numbered stanza that happens to repeat is still a stanza.
        var repeated = keys
            .Where((k, i) => k.Length > 0 && segments[i].Number is null && !segments[i].Labelled)
            .GroupBy(k => k)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet();

        var kinds = new SectionKind[segments.Count];
        var unlabelledRefrains = 0;

        for (var i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            if (segment.Labelled)
            {
                kinds[i] = SectionKind.Refrain;
            }
            else if (segment.Number is not null)
            {
                kinds[i] = SectionKind.Verse;
            }
            else if (repeated.Contains(keys[i]))
            {
                kinds[i] = SectionKind.Refrain;
                unlabelledRefrains++;
            }
            else
            {
                kinds[i] = SectionKind.Continuation;   // provisional; may become a verse below
            }
        }

        if (unlabelledRefrains > 0)
        {
            notes.Add($"{unlabelledRefrains} refrain slide(s) identified by repetition, not by a label");
        }

        PromoteGapFillingVerses(segments, kinds, notes);

        // A hymn that never states a number still has stanzas; without this every slide
        // would be a continuation of nothing.
        if (kinds.All(k => k != SectionKind.Verse))
        {
            for (var i = 0; i < kinds.Length; i++)
            {
                if (kinds[i] == SectionKind.Continuation)
                {
                    kinds[i] = SectionKind.Verse;
                }
            }

            notes.Add("no verse numbers anywhere; each non-refrain slide treated as a stanza");
        }

        return Number(segments, kinds, notes);
    }

    /// <summary>
    /// Turns an unlabelled slide into a stanza when it is the only candidate for a number
    /// the deck skipped - the "1. ... 3. ..." shape, where verse 2 simply lost its digit.
    /// Anything less clear-cut stays a continuation, which preserves order without
    /// inventing a stanza number.
    /// </summary>
    private static void PromoteGapFillingVerses(List<Block> segments, SectionKind[] kinds, List<string> notes)
    {
        for (var i = 0; i < segments.Count; i++)
        {
            if (kinds[i] != SectionKind.Continuation)
            {
                continue;
            }

            var previous = LastVerseNumberBefore(segments, kinds, i);
            var next = NextVerseNumberAfter(segments, kinds, i);
            if (previous is null || next is null || next - previous != 2)
            {
                continue;
            }

            // Only when this slide is the single unlabelled one inside the gap.
            var others = 0;
            for (var j = i + 1; j < segments.Count && kinds[j] != SectionKind.Verse; j++)
            {
                if (kinds[j] == SectionKind.Continuation)
                {
                    others++;
                }
            }

            for (var j = i - 1; j >= 0 && kinds[j] != SectionKind.Verse; j--)
            {
                if (kinds[j] == SectionKind.Continuation)
                {
                    others++;
                }
            }

            if (others == 0)
            {
                kinds[i] = SectionKind.Verse;
                notes.Add($"unnumbered slide treated as stanza {previous + 1} (the deck skips it)");
            }
            else
            {
                // Several unlabelled slides compete for one missing number and nothing in
                // the text says which. Guessing would caption the wrong stanza, so they
                // stay continuations and the ambiguity is recorded instead.
                notes.Add($"stanza {previous + 1} is missing and {others + 1} unlabelled slides could be it; left unnumbered");
            }
        }
    }

    private static int? LastVerseNumberBefore(List<Block> segments, SectionKind[] kinds, int index)
    {
        for (var i = index - 1; i >= 0; i--)
        {
            if (kinds[i] == SectionKind.Verse && segments[i].Number is { } n)
            {
                return n;
            }
        }

        return null;
    }

    private static int? NextVerseNumberAfter(List<Block> segments, SectionKind[] kinds, int index)
    {
        for (var i = index + 1; i < segments.Count; i++)
        {
            if (kinds[i] == SectionKind.Verse && segments[i].Number is { } n)
            {
                return n;
            }
        }

        return null;
    }

    /// <summary>
    /// Assigns final stanza numbers. Stated numbers are kept when they are consistent;
    /// where a deck repeats or reverses them the sequence wins, because a duplicate number
    /// breaks navigation while a renumbered stanza only differs by its caption.
    /// </summary>
    private static List<HymnSection> Number(List<Block> segments, SectionKind[] kinds, List<string> notes)
    {
        var sections = new List<HymnSection>(segments.Count);
        var used = new HashSet<int>();
        var next = 1;
        var order = 1;
        var renumbered = 0;

        for (var i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            var content = string.Join("\n", segment.Lines).Trim();
            if (content.Length == 0)
            {
                continue;
            }

            var number = 0;
            if (kinds[i] == SectionKind.Verse)
            {
                var stated = segment.Number;
                if (stated is { } value && value > 0 && used.Add(value))
                {
                    number = value;
                }
                else
                {
                    while (!used.Add(next))
                    {
                        next++;
                    }

                    number = next;
                    if (stated is not null)
                    {
                        renumbered++;
                    }
                }

                next = Math.Max(next, number + 1);
            }

            sections.Add(new HymnSection(
                kinds[i], number, content, order++,
                WasNumbered: segment.Number is not null,
                WasLabelled: segment.Labelled));
        }

        if (renumbered > 0)
        {
            notes.Add($"{renumbered} stanza number(s) reassigned; the deck repeats or reverses them");
        }

        return sections;
    }

    // ---------- patterns ----------

    /// <summary>"1." or "12)" at the start of a line, with or without a following space.</summary>
    [GeneratedRegex(@"^(\d{1,3})[.)]\s*")]
    private static partial Regex NumberedLine();

    /// <summary>A line that is nothing but a number - some decks title a hymn that way.</summary>
    [GeneratedRegex(@"^\d{1,3}\s*$")]
    private static partial Regex BareNumber();

    /// <summary>
    /// "Refren:", "Refren :", "REFREN:" - including the six decks that run the label
    /// straight into the first line - or "Refren" alone on its line.
    /// <para>
    /// The colon is required unless the label is the whole line, because "Refrenul" and
    /// "Corul" are ordinary Romanian words that begin lyric lines. Matching them as labels
    /// silently splits a stanza in two. Every label spelling in the library satisfies this.
    /// </para>
    /// </summary>
    [GeneratedRegex(@"^refren(?:ul)?\s*(?::\s*|$)", RegexOptions.IgnoreCase)]
    private static partial Regex RefrainLabel();

    [GeneratedRegex(@"^\s*imnul\s+\d+\s*", RegexOptions.IgnoreCase)]
    private static partial Regex ImnulTitle();

    [GeneratedRegex(@"^0*(\d+)")]
    private static partial Regex LeadingDigits();
}
