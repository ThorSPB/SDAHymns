namespace SDAHymns.Core.Services.Decks;

/// <summary>
/// Turns deck paths into a form the database can keep, and back again.
/// </summary>
/// <remarks>
/// Hymn rows store a path to the deck they came from. Stored absolute, that path is
/// valid on exactly one machine - 184 rows in the shipped database point at
/// <c>D:\Coding\SDAHymns\...</c> and resolve nowhere else, because the orphan importer
/// wrote <see cref="Path.GetFullPath(string)"/> where the XML importer wrote a relative
/// path. Stored relative, it also depends on the working directory, so reading one has
/// to anchor it explicitly.
/// </remarks>
public static class LegacyLibraryPath
{
    private const string SolutionMarker = "*.sln";

    /// <summary>Repo-relative, forward-slashed - the form to store.</summary>
    public static string ToRelative(string path, string? root = null)
    {
        root ??= FindRoot();
        return root is null
            ? path.Replace('\\', '/')
            : Path.GetRelativePath(root, Path.GetFullPath(path)).Replace('\\', '/');
    }

    /// <summary>
    /// An absolute path to read from. Accepts either stored form, and falls back to the
    /// value as given so a caller's own error message stays meaningful.
    /// </summary>
    public static string Resolve(string storedPath, string? root = null)
    {
        if (Path.IsPathRooted(storedPath) && File.Exists(storedPath))
        {
            return storedPath;
        }

        root ??= FindRoot();
        if (root is not null)
        {
            var candidate = Path.GetFullPath(Path.Combine(root, storedPath));
            if (File.Exists(candidate))
            {
                return candidate;
            }

            var insensitive = FindIgnoringCase(candidate);
            if (insensitive is not null)
            {
                return insensitive;
            }
        }

        return FindIgnoringCase(Path.GetFullPath(storedPath)) ?? storedPath;
    }

    /// <summary>
    /// Finds a deck whose name differs only in case.
    /// </summary>
    /// <remarks>
    /// 152 rows in the shipped database end in <c>.PPT</c> where the file on disk ends in
    /// <c>.ppt</c>. They were written on Windows, where the difference is invisible, and
    /// resolve nowhere on Linux or macOS - so those hymns quietly import no verses on the
    /// platforms the app also targets.
    /// </remarks>
    private static string? FindIgnoringCase(string absolutePath)
    {
        var directory = Path.GetDirectoryName(absolutePath);
        var name = Path.GetFileName(absolutePath);
        if (directory is null || name.Length == 0 || !Directory.Exists(directory))
        {
            return null;
        }

        return Directory.EnumerateFiles(directory)
            .FirstOrDefault(f => string.Equals(Path.GetFileName(f), name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Walks up from the running assembly looking for the solution file.</summary>
    private static string? FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (directory.GetFiles(SolutionMarker).Length > 0)
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
