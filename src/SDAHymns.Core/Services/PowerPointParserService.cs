using System.Diagnostics;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;

namespace SDAHymns.Core.Services;

/// <summary>
/// Service for parsing hymn titles from legacy PowerPoint (.PPT) files
/// </summary>
public class PowerPointParserService
{
    private const string LibreOfficePath = @"C:\Program Files\LibreOffice\program\soffice.com";
    private readonly string _tempDirectory;

    public PowerPointParserService()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "sdahymns-ppt-parser");
        Directory.CreateDirectory(_tempDirectory);
    }

    /// <summary>
    /// Extracts hymn number and title from a .PPT file
    /// </summary>
    /// <param name="pptFilePath">Path to the .PPT file</param>
    /// <returns>Tuple of (hymn number, title) or null if parsing failed</returns>
    public async Task<(int hymnNumber, string title)?> ExtractHymnInfoAsync(string pptFilePath)
    {
        if (!File.Exists(pptFilePath))
        {
            throw new FileNotFoundException($"PPT file not found: {pptFilePath}");
        }

        try
        {
            // Step 1: Convert PPT to PPTX using LibreOffice
            var pptxPath = await ConvertPptToPptxAsync(pptFilePath);

            if (pptxPath == null)
            {
                return null;
            }

            // Step 2: Extract text from first slide
            var texts = ExtractFirstSlideText(pptxPath);

            // Step 3: Parse hymn number and title
            var result = ParseHymnInfo(texts);

            // Clean up temporary PPTX file
            try
            {
                File.Delete(pptxPath);
            }
            catch
            {
                // Ignore cleanup errors
            }

            return result;
        }
        catch (Exception)
        {
            // Silent failure - will be reported by calling code
            return null;
        }
    }

    /// <summary>
    /// Converts a .PPT file to .PPTX using LibreOffice
    /// </summary>
    private async Task<string?> ConvertPptToPptxAsync(string pptFilePath)
    {
        if (!File.Exists(LibreOfficePath))
        {
            throw new InvalidOperationException($"LibreOffice not found at: {LibreOfficePath}");
        }

        var fullPptPath = Path.GetFullPath(pptFilePath);
        var outputDir = _tempDirectory;

        var startInfo = new ProcessStartInfo
        {
            FileName = LibreOfficePath,
            Arguments = $"--headless --convert-to pptx --outdir \"{outputDir}\" \"{fullPptPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            return null;
        }

        await process.WaitForExitAsync();

        // Construct expected output path
        var fileName = Path.GetFileNameWithoutExtension(pptFilePath);
        var pptxPath = Path.Combine(outputDir, $"{fileName}.pptx");

        return File.Exists(pptxPath) ? pptxPath : null;
    }

    /// <summary>
    /// Extracts all text from the first slide of a PPTX file
    /// </summary>
    private List<string> ExtractFirstSlideText(string pptxPath)
    {
        var texts = new List<string>();

        using var presentationDocument = PresentationDocument.Open(pptxPath, false);
        var presentationPart = presentationDocument.PresentationPart;
        if (presentationPart == null)
        {
            return texts;
        }

        // Get first slide
        var slideIdList = presentationPart.Presentation.SlideIdList;
        if (slideIdList == null)
        {
            return texts;
        }

        var firstSlideId = slideIdList.ChildElements.OfType<SlideId>().FirstOrDefault();
        if (firstSlideId?.RelationshipId?.Value == null)
        {
            return texts;
        }

        var slidePart = (SlidePart)presentationPart.GetPartById(firstSlideId.RelationshipId.Value);

        // Extract all text from the slide
        var textElements = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>();
        foreach (var textElement in textElements)
        {
            if (!string.IsNullOrWhiteSpace(textElement.Text))
            {
                texts.Add(textElement.Text.Trim());
            }
        }

        return texts;
    }

    /// <summary>
    /// Parses hymn number and title from extracted text
    /// Format found in actual files:
    /// - Element 0: Title (e.g., "Bunul nostru Salvator")
    /// - Element 1: "Imnul" (literal text)
    /// - Elements 2+: Number parts (e.g., "7", "37" -> combine to "737")
    /// </summary>
    private (int hymnNumber, string title)? ParseHymnInfo(List<string> texts)
    {
        if (texts.Count < 3)
        {
            return null;
        }

        // First text is the title
        var title = texts[0].Trim();
        if (string.IsNullOrEmpty(title))
        {
            return null;
        }

        // Collect all numeric text elements and combine them
        var numberParts = new List<string>();
        for (int i = 1; i < texts.Count; i++)
        {
            // Extract any digits from this element
            var digits = Regex.Match(texts[i], @"\d+");
            if (digits.Success)
            {
                numberParts.Add(digits.Value);
            }
        }

        if (numberParts.Count == 0)
        {
            return null;
        }

        // Combine all number parts (e.g., ["7", "37"] -> "737")
        var fullNumber = string.Join("", numberParts);
        if (!int.TryParse(fullNumber, out var hymnNumber))
        {
            return null;
        }

        return (hymnNumber, title);
    }

    /// <summary>
    /// Batch process multiple PPT files
    /// </summary>
    public async Task<Dictionary<string, (int hymnNumber, string title)?>> ExtractBatchAsync(
        IEnumerable<string> pptFiles,
        IProgress<(int current, int total, string fileName)>? progress = null)
    {
        var results = new Dictionary<string, (int hymnNumber, string title)?>();
        var fileList = pptFiles.ToList();
        var total = fileList.Count;
        var current = 0;

        foreach (var pptFile in fileList)
        {
            current++;
            var fileName = Path.GetFileName(pptFile);
            progress?.Report((current, total, fileName));

            var result = await ExtractHymnInfoAsync(pptFile);
            results[pptFile] = result;

            // Small delay to avoid overwhelming LibreOffice
            await Task.Delay(100);
        }

        return results;
    }
}
