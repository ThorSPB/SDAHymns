namespace SDAHymns.Tests.Services.Decks;

/// <summary>
/// Locates the legacy hymn library, which is deliberately not committed (it is roughly
/// 180 MB of PowerPoint). Tests that need it skip when it is absent, so CI stays green
/// while the golden-file check still runs on any machine that has the decks.
/// </summary>
internal static class HymnLibrary
{
    private const string ResourceFolder = "Imnuri Azs/Resurse";

    public static string? ResoursePath { get; } = Find();

    public static bool IsAvailable => ResoursePath is not null;

    public const string SkipReason =
        "Legacy hymn library not present (expected at '<repo>/Imnuri Azs/Resurse').";

    private static string? Find()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, ResourceFolder);
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
