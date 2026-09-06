using FluentAssertions;
using SDAHymns.Core.Services.Decks;

namespace SDAHymns.Tests.Services.Decks;

public class DeckReaderTests
{
    [Fact]
    public void Ppt97Reader_recognises_the_OLE2_signature()
    {
        byte[] header = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];
        new Ppt97Reader().CanRead(header).Should().BeTrue();
    }

    [Fact]
    public void Ppt97Reader_rejects_a_zip_package()
    {
        byte[] header = [0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x06, 0x00];
        new Ppt97Reader().CanRead(header).Should().BeFalse();
    }

    [Fact]
    public void OoxmlReader_recognises_a_zip_package()
    {
        byte[] header = [0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x06, 0x00];
        new OoxmlDeckReader().CanRead(header).Should().BeTrue();
    }

    [Fact]
    public void Reader_reports_an_unrecognised_format_rather_than_guessing()
    {
        var path = Path.Combine(Path.GetTempPath(), $"deck-{Guid.NewGuid():N}.ppt");
        File.WriteAllText(path, "this is plain text, not a presentation");
        try
        {
            var act = () => new DeckReader().Read(path);
            act.Should().Throw<DeckReadException>().WithMessage("*Unrecognised presentation format*");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Reader_reports_a_missing_file_distinctly_from_a_bad_one()
    {
        var act = () => new DeckReader().Read(Path.Combine(Path.GetTempPath(), "does-not-exist.ppt"));
        act.Should().Throw<FileNotFoundException>();
    }

    /// <summary>
    /// 184 decks in "Imnuri crestine" are OOXML packages named .PPT. Dispatching on the
    /// extension would send every one of them to the binary reader.
    /// </summary>
    [Fact]
    public void Format_is_decided_by_content_not_extension()
    {
        if (!HymnLibrary.IsAvailable)
        {
            return;   // see HymnLibrary.SkipReason
        }

        var crestine = Path.Combine(HymnLibrary.ResoursePath!, "Imnuri crestine", "ppt");
        var reader = new DeckReader();

        reader.Read(Path.Combine(crestine, "001.PPT")).Format.Should().Be(DeckFormat.Ppt97);
        reader.Read(Path.Combine(crestine, "737.PPT")).Format.Should().Be(DeckFormat.Ooxml);
    }

    /// <summary>
    /// A .ppt stream keeps records from earlier saves. Reading the live persist chain is
    /// what keeps hymn 422 at its real 7 slides; scanning for SlideContainers finds 12.
    /// </summary>
    [Fact]
    public void Slides_from_superseded_saves_are_not_returned()
    {
        if (!HymnLibrary.IsAvailable)
        {
            return;   // see HymnLibrary.SkipReason
        }

        var path = Path.Combine(HymnLibrary.ResoursePath!, "Imnuri crestine", "ppt", "422.PPT");
        var deck = new DeckReader().Read(path);

        deck.Slides.Should().HaveCount(7);
        deck.Slides[0].Should().Contain("422");
    }
}
