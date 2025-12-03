namespace SDAHymns.Core.Data.Models;

public class Verse
{
    public int Id { get; set; }

    // Hymn relationship
    public int HymnId { get; set; }
    public Hymn Hymn { get; set; } = null!;

    // Verse data
    public int VerseNumber { get; set; }  // 1, 2, 3, etc.
    public required string Content { get; set; }  // Verse text/lyrics

    // Optional metadata
    public string? Label { get; set; }  // e.g., "Refren", "Strofa 1"

    // Order and display
    public int DisplayOrder { get; set; }  // For custom ordering

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
