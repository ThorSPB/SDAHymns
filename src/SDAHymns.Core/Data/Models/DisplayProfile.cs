namespace SDAHymns.Core.Data.Models;

public class DisplayProfile
{
    public int Id { get; set; }

    public required string Name { get; set; }  // "Projector", "OBS Stream"
    public string? Description { get; set; }

    // Background
    public string BackgroundColor { get; set; } = "#000000";
    public double BackgroundOpacity { get; set; } = 1.0;
    public string? BackgroundImagePath { get; set; }

    // Text styling
    public string FontFamily { get; set; } = "Arial";
    public int FontSize { get; set; } = 48;
    public string TextColor { get; set; } = "#FFFFFF";
    public string TextAlignment { get; set; } = "Center";  // Left, Center, Right
    public bool EnableTextShadow { get; set; } = true;
    public string? ShadowColor { get; set; } = "#000000";

    // Layout
    public int PaddingHorizontal { get; set; } = 40;
    public int PaddingVertical { get; set; } = 40;
    public int LineSpacing { get; set; } = 10;

    // Special modes
    public bool TransparentBackground { get; set; } = false;

    // Metadata
    public bool IsDefault { get; set; } = false;
    public bool IsSystemProfile { get; set; } = false;  // Cannot be deleted

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
