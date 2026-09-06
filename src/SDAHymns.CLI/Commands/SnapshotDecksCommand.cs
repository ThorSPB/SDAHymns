using System.Diagnostics;
using CommandLine;
using SDAHymns.Core.Services.Decks;

namespace SDAHymns.CLI.Commands;

[Verb("snapshot-decks", HelpText = "Parse every hymn deck and write the golden-file snapshots")]
public class SnapshotDecksCommand
{
    [Option('p', "path", Required = true, HelpText = "Path to the legacy 'Resurse' folder")]
    public string ResoursePath { get; set; } = string.Empty;

    [Option('o', "out", Required = true, HelpText = "Directory to write snapshot files into")]
    public string OutputPath { get; set; } = string.Empty;
}

public class SnapshotDecksCommandHandler
{
    public int Execute(SnapshotDecksCommand options)
    {
        if (!Directory.Exists(options.ResoursePath))
        {
            Console.Error.WriteLine($"Resurse folder not found: {options.ResoursePath}");
            return 1;
        }

        var textDirectory = Path.Combine(options.OutputPath, "text");
        var structureDirectory = Path.Combine(options.OutputPath, "structure");
        Directory.CreateDirectory(textDirectory);
        Directory.CreateDirectory(structureDirectory);
        var reader = new DeckReader();
        var stopwatch = Stopwatch.StartNew();
        var total = 0;

        var categories = Directory.EnumerateDirectories(options.ResoursePath)
            .OrderBy(Path.GetFileName, StringComparer.Ordinal);

        foreach (var category in categories)
        {
            var name = Path.GetFileName(category);
            var fileName = Slug(name) + ".txt";

            var text = DeckSnapshot.RenderCategory(category, reader);
            var structure = DeckSnapshot.RenderStructureCategory(category, reader);

            // '\n' throughout, so the snapshots diff identically on Windows and Linux.
            File.WriteAllText(Path.Combine(textDirectory, fileName), text);
            File.WriteAllText(Path.Combine(structureDirectory, fileName), structure);

            var decks = text.Split("\n### ").Length - 1;
            var errors = text.Split(" | ERROR\n").Length - 1;
            var notes = structure.Split("\nnote: ").Length - 1;
            total += decks;
            Console.WriteLine($"{name,-22} {decks,5} decks  {errors,3} errors  {notes,5} notes  -> {fileName}");
        }

        Console.WriteLine($"\n{total} decks in {stopwatch.Elapsed.TotalSeconds:F1}s");
        return 0;
    }

    private static string Slug(string categoryName) =>
        categoryName.Replace("Imnuri ", "").Replace(' ', '-').ToLowerInvariant();
}
