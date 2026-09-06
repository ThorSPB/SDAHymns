using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;

namespace SDAHymns.Core.Services.Decks;

/// <summary>
/// Reads OOXML presentations, whatever extension they carry.
/// </summary>
/// <remarks>
/// "Imnuri crestine" 737-920 are OOXML packages saved with a <c>.PPT</c> extension, so
/// dispatch is by magic bytes. The old code reached these files only after LibreOffice
/// had converted them - a conversion from OOXML back to OOXML.
/// </remarks>
public sealed class OoxmlDeckReader : IDeckReader
{
    private static readonly byte[] ZipSignature = [0x50, 0x4B, 0x03, 0x04];

    public bool CanRead(ReadOnlySpan<byte> header) =>
        header.Length >= ZipSignature.Length && header[..4].SequenceEqual(ZipSignature);

    public SlideDeck Read(string path)
    {
        try
        {
            using var document = PresentationDocument.Open(path, false);
            var presentationPart = document.PresentationPart
                ?? throw new DeckReadException($"No presentation part: {path}");

            var slideIds = presentationPart.Presentation.SlideIdList?
                .ChildElements.OfType<SlideId>() ?? [];

            var slides = new List<string>();
            foreach (var slideId in slideIds)
            {
                var relationshipId = slideId.RelationshipId?.Value;
                if (relationshipId is null)
                {
                    continue;
                }

                var slidePart = (SlidePart)presentationPart.GetPartById(relationshipId);
                slides.Add(ReadSlide(slidePart));
            }

            return new SlideDeck(path, DeckFormat.Ooxml, slides);
        }
        catch (Exception ex) when (ex is not DeckReadException)
        {
            throw new DeckReadException($"Not a readable OOXML presentation: {path}", ex);
        }
    }

    private static string ReadSlide(SlidePart slidePart)
    {
        var shapeTree = slidePart.Slide.CommonSlideData?.ShapeTree;
        if (shapeTree is null)
        {
            return string.Empty;
        }

        // Vertical position, then document order for shapes that share one (or inherit
        // their position from the layout, which reads back as 0).
        var shapes = shapeTree.Elements<Shape>()
            .OrderBy(s => s.ShapeProperties?.Transform2D?.Offset?.Y?.Value ?? 0);

        var lines = new List<string>();
        foreach (var shape in shapes)
        {
            foreach (var paragraph in shape.Descendants<A.Paragraph>())
            {
                var builder = new StringBuilder();
                foreach (var child in paragraph.Elements())
                {
                    switch (child)
                    {
                        case A.Run run:
                            builder.Append(run.Text?.Text);
                            break;
                        case A.Break:
                            builder.Append('\n');
                            break;
                    }
                }

                var line = builder.ToString().Trim();
                if (line.Length > 0)
                {
                    lines.Add(line);
                }
            }
        }

        return string.Join("\n", lines).Trim();
    }
}
