using CommandLine;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;

namespace SDAHymns.CLI.Commands;

[Verb("test-ppt", HelpText = "Test PPT file structure analysis")]
public class TestPptCommand
{
    [Option('f', "file", Required = true, HelpText = "Path to PPT file to analyze")]
    public string FilePath { get; set; } = string.Empty;

    public async Task<int> ExecuteAsync()
    {
        Console.WriteLine($"Analyzing PPT file: {FilePath}");
        Console.WriteLine();

        // Convert PPT to PPTX first
        var tempDir = Path.Combine(Path.GetTempPath(), "sdahymns-test");
        Directory.CreateDirectory(tempDir);

        var fullPath = Path.GetFullPath(FilePath);
        var fileName = Path.GetFileNameWithoutExtension(fullPath);
        var pptxPath = Path.Combine(tempDir, $"{fileName}.pptx");

        Console.WriteLine("Converting PPT to PPTX...");
        var convertResult = await ConvertPptToPptxAsync(fullPath, tempDir);

        if (!convertResult)
        {
            Console.WriteLine("❌ Failed to convert PPT file");
            return 1;
        }

        Console.WriteLine($"✅ Converted to: {pptxPath}");
        Console.WriteLine();

        // Analyze slides
        AnalyzeSlides(pptxPath);

        // Cleanup
        try
        {
            File.Delete(pptxPath);
        }
        catch { }

        return 0;
    }

    private async Task<bool> ConvertPptToPptxAsync(string pptPath, string outputDir)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = @"C:\Program Files\LibreOffice\program\soffice.com",
            Arguments = $"--headless --convert-to pptx --outdir \"{outputDir}\" \"{pptPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(startInfo);
        if (process == null) return false;

        await process.WaitForExitAsync();

        var expectedOutput = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(pptPath) + ".pptx");
        return File.Exists(expectedOutput);
    }

    private void AnalyzeSlides(string pptxPath)
    {
        using var doc = PresentationDocument.Open(pptxPath, false);
        var presentationPart = doc.PresentationPart;

        if (presentationPart == null)
        {
            Console.WriteLine("❌ No presentation part found");
            return;
        }

        var slideIdList = presentationPart.Presentation.SlideIdList;
        if (slideIdList == null)
        {
            Console.WriteLine("❌ No slides found");
            return;
        }

        var slideIds = slideIdList.ChildElements.OfType<SlideId>().ToList();
        Console.WriteLine($"📊 Total Slides: {slideIds.Count}");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine();

        for (int i = 0; i < slideIds.Count; i++)
        {
            var slideId = slideIds[i];
            var slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!.Value!);

            Console.WriteLine($"📄 Slide {i + 1}");
            Console.WriteLine(new string('-', 40));

            var textElements = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>();
            var texts = textElements
                .Where(t => !string.IsNullOrWhiteSpace(t.Text))
                .Select(t => t.Text.Trim())
                .ToList();

            if (texts.Any())
            {
                Console.WriteLine("Text elements found:");
                foreach (var text in texts)
                {
                    Console.WriteLine($"  • {text}");
                }

                Console.WriteLine();
                Console.WriteLine("Combined slide text:");
                var combined = string.Join("\n", texts);
                Console.WriteLine($"---\n{combined}\n---");
            }
            else
            {
                Console.WriteLine("  (empty slide)");
            }

            Console.WriteLine();
        }

        Console.WriteLine(new string('=', 80));
        Console.WriteLine("✅ Analysis Complete!");
    }
}
