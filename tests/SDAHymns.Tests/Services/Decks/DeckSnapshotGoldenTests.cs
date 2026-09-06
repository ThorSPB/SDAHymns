using FluentAssertions;
using SDAHymns.Core.Services.Decks;

namespace SDAHymns.Tests.Services.Decks;

/// <summary>
/// Compares every deck in the library against a committed snapshot.
/// </summary>
/// <remarks>
/// This is the safety net for the parser. The classification rules have to cope with
/// 1256 decks that disagree about almost everything - two file formats, three title
/// styles, labelled and unlabelled refrains, verses that restart at 2 - so any change
/// is judged by what it does to the whole corpus, not to the one hymn that prompted it.
/// <para>
/// When a change is intentional, regenerate and read the diff before committing:
/// <code>
/// dotnet run --project src/SDAHymns.CLI -- snapshot-decks \
///     --path "Imnuri Azs/Resurse" --out tests/SDAHymns.Tests/Fixtures/decks
/// </code>
/// </para>
/// </remarks>
public class DeckSnapshotGoldenTests
{
    private static readonly string FixtureDirectory =
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "decks");

    public static TheoryData<string, string> Categories() => new()
    {
        { "Imnuri companioni", "companioni.txt" },
        { "Imnuri crestine", "crestine.txt" },
        { "Imnuri exploratori", "exploratori.txt" },
        { "Imnuri licurici", "licurici.txt" },
        { "Imnuri tineret", "tineret.txt" },
    };

    [Theory]
    [MemberData(nameof(Categories))]
    public void Category_matches_its_committed_snapshot(string categoryFolder, string fixtureName)
    {
        if (!HymnLibrary.IsAvailable)
        {
            return;   // see HymnLibrary.SkipReason
        }

        var fixturePath = Path.Combine(FixtureDirectory, fixtureName);
        File.Exists(fixturePath).Should().BeTrue($"snapshot '{fixtureName}' should be committed");

        var expected = File.ReadAllText(fixturePath).Replace("\r\n", "\n");
        var actual = DeckSnapshot.RenderCategory(
            Path.Combine(HymnLibrary.ResoursePath!, categoryFolder), new DeckReader());

        if (actual == expected)
        {
            return;
        }

        FirstDifference(expected, actual, fixtureName)
            .Should().BeNull("the parser output should match the committed snapshot");
    }

    [Fact]
    public void Every_deck_in_the_library_is_readable()
    {
        if (!HymnLibrary.IsAvailable)
        {
            return;   // see HymnLibrary.SkipReason
        }

        var reader = new DeckReader();
        var failures = new List<string>();
        var count = 0;

        foreach (var file in Directory.EnumerateFiles(HymnLibrary.ResoursePath!, "*", SearchOption.AllDirectories))
        {
            var extension = Path.GetExtension(file);
            if (!extension.Equals(".ppt", StringComparison.OrdinalIgnoreCase) &&
                !extension.Equals(".pps", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            count++;
            try
            {
                var deck = reader.Read(file);
                if (deck.Slides.Count == 0)
                {
                    failures.Add($"{Path.GetFileName(file)}: no slides");
                }
            }
            catch (Exception ex)
            {
                failures.Add($"{Path.GetFileName(file)}: {ex.Message}");
            }
        }

        count.Should().BeGreaterThan(1000, "the full library should have been found");
        failures.Should().BeEmpty();
    }

    /// <summary>Returns a readable description of the first differing line, or null if identical.</summary>
    private static string? FirstDifference(string expected, string actual, string fixtureName)
    {
        var expectedLines = expected.Split('\n');
        var actualLines = actual.Split('\n');

        for (var i = 0; i < Math.Max(expectedLines.Length, actualLines.Length); i++)
        {
            var e = i < expectedLines.Length ? expectedLines[i] : "<end of snapshot>";
            var a = i < actualLines.Length ? actualLines[i] : "<end of output>";
            if (e != a)
            {
                return $"{fixtureName} line {i + 1}:\n  snapshot: {e}\n  parsed:   {a}";
            }
        }

        return null;
    }
}
