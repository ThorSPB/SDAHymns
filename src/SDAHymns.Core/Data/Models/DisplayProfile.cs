namespace SDAHymns.Core.Data.Models;

/// <summary>
/// Represents a display profile with customizable fonts, colors, backgrounds, and layout settings.
/// Used to switch between different projection scenarios (projector, OBS, practice, etc.)
/// </summary>
public class DisplayProfile
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsSystemProfile { get; set; } = false;  // Cannot be deleted
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Typography
    public string FontFamily { get; set; } = "Inter";
    public int TitleFontSize { get; set; } = 36;
    public int VerseFontSize { get; set; } = 48;
    public int LabelFontSize { get; set; } = 28;
    public string FontWeight { get; set; } = "Normal"; // Normal, SemiBold, Bold
    public double LineHeight { get; set; } = 1.4;
    public double LetterSpacing { get; set; } = 0;

    // Colors (stored as hex strings like "#FFFFFF")
    public string BackgroundColor { get; set; } = "#000000";
    public string TextColor { get; set; } = "#FFFFFF";
    public string TitleColor { get; set; } = "#FFFFFF";
    public string LabelColor { get; set; } = "#CCCCCC";
    public string AccentColor { get; set; } = "#0078D4";

    // Background
    public double BackgroundOpacity { get; set; } = 1.0;
    public string? BackgroundImagePath { get; set; }
    public string BackgroundImageMode { get; set; } = "Fill"; // Fill, Fit, Stretch, Tile
    public double BackgroundImageOpacity { get; set; } = 0.3;

    // Layout
    public string TextAlignment { get; set; } = "Left"; // Left, Center, Right
    public string VerticalAlignment { get; set; } = "Center"; // Top, Center, Bottom
    public int MarginLeft { get; set; } = 100;
    public int MarginRight { get; set; } = 100;
    public int MarginTop { get; set; } = 60;
    public int MarginBottom { get; set; } = 60;

    // Effects
    public bool EnableTextShadow { get; set; } = false;
    public string ShadowColor { get; set; } = "#000000";
    public int ShadowBlurRadius { get; set; } = 10;
    public int ShadowOffsetX { get; set; } = 2;
    public int ShadowOffsetY { get; set; } = 2;

    public bool EnableTextOutline { get; set; } = false;
    public string OutlineColor { get; set; } = "#000000";
    public int OutlineThickness { get; set; } = 2;

    // Advanced
    public bool TransparentBackground { get; set; } = false;
    public bool ShowVerseNumbers { get; set; } = true;
    public bool ShowHymnTitle { get; set; } = true;

    // === Enhanced Slide Formatting (Spec 018) ===

    // Verse Number Styling
    public string VerseNumberStyle { get; set; } = "InlinePlain"; // None, InlinePlain, InlineBold, Badge, LargeDecorative, Superscript
    public bool VerseNumberSeparateLine { get; set; } = false;
    public string VerseNumberColor { get; set; } = "#CCCCCC";
    public int VerseNumberSize { get; set; } = 32; // Font size override (0 = use default)

    // Chorus Formatting
    public string ChorusStyle { get; set; } = "SameAsVerse"; // SameAsVerse, Indented, Italic, ColoredText, BackgroundHighlight, Combined
    public int ChorusIndentAmount { get; set; } = 80; // Pixels
    public string ChorusTextColor { get; set; } = "#E0E0E0"; // Hex color
    public string ChorusBackgroundColor { get; set; } = "#1A1A1A"; // Hex color
    public bool ChorusItalic { get; set; } = false;
    public bool ShowChorusLabel { get; set; } = true; // "Refrain:" prefix

    // Typography Enhancements
    public int ParagraphSpacing { get; set; } = 40; // Pixels between verse number and text
    public int VerseSpacing { get; set; } = 60; // Pixels between verses/choruses

    // Slide Metadata Display
    public bool ShowHymnNumber { get; set; } = false;
    public bool ShowCategory { get; set; } = false;
    public bool ShowVerseIndicator { get; set; } = false; // "Verse 2/4"
    public string MetadataPosition { get; set; } = "None"; // None, TopLeft, TopRight, BottomLeft, BottomRight
    public int MetadataFontSize { get; set; } = 20;
    public string MetadataColor { get; set; } = "#888888";
    public double MetadataOpacity { get; set; } = 0.7;

    // Transition Effects
    public string VerseTransition { get; set; } = "None"; // None, Fade, Slide, Dissolve, FadeToBlack
    public int TransitionDuration { get; set; } = 300; // Milliseconds (100-1000)

    // Special Slides (User Requirements)
    public bool ShowTitleOnFirstVerseOnly { get; set; } = false; // Title appears only on first verse
    public bool EnableBlackEndingSlide { get; set; } = true; // Black slide after last verse
    public int EndingSlideAutoCloseDuration { get; set; } = 10; // Seconds (0 = disabled)
}
