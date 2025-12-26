using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using SDAHymns.Core.Data.Models;

namespace SDAHymns.Desktop.Views;

public partial class DisplayWindow : Window
{
    public DisplayWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Applies a display profile to the window, updating all visual properties
    /// </summary>
    public void ApplyProfile(DisplayProfile profile)
    {
        if (profile == null) return;

        // Apply to root border (background)
        if (RootBorder != null)
        {
            if (profile.TransparentBackground)
            {
                // Make background fully transparent while keeping content visible
                RootBorder.Background = new SolidColorBrush(Colors.Transparent);
            }
            else if (!string.IsNullOrWhiteSpace(profile.BackgroundImagePath) && File.Exists(profile.BackgroundImagePath))
            {
                // Use background image
                try
                {
                    var bitmap = new Avalonia.Media.Imaging.Bitmap(profile.BackgroundImagePath);
                    var imageBrush = new Avalonia.Media.ImageBrush(bitmap)
                    {
                        Opacity = profile.BackgroundImageOpacity,
                        Stretch = ParseStretchMode(profile.BackgroundImageMode)
                    };
                    RootBorder.Background = imageBrush;
                }
                catch (Exception ex)
                {
                    // Log the error for debugging
                    System.Diagnostics.Debug.WriteLine($"Failed to load background image '{profile.BackgroundImagePath}': {ex.Message}");

                    // If image fails to load, fall back to color
                    var bgColor = Color.Parse(profile.BackgroundColor);
                    var bgBrush = new SolidColorBrush(bgColor);
                    bgBrush.Opacity = profile.BackgroundOpacity;
                    RootBorder.Background = bgBrush;
                }
            }
            else
            {
                // Use the specified background color with opacity
                var bgColor = Color.Parse(profile.BackgroundColor);
                var bgBrush = new SolidColorBrush(bgColor);
                bgBrush.Opacity = profile.BackgroundOpacity;
                RootBorder.Background = bgBrush;
            }
        }

        // Apply to title
        if (HymnTitleText != null)
        {
            HymnTitleText.FontFamily = new FontFamily(profile.FontFamily);
            HymnTitleText.FontSize = profile.TitleFontSize;
            HymnTitleText.Foreground = new SolidColorBrush(Color.Parse(profile.TitleColor));
            HymnTitleText.FontWeight = ParseFontWeight(profile.FontWeight);
            HymnTitleText.TextAlignment = ParseTextAlignment(profile.TextAlignment);
            HymnTitleText.IsVisible = profile.ShowHymnTitle;
        }

        // Apply to verse label
        if (VerseLabelText != null)
        {
            VerseLabelText.FontFamily = new FontFamily(profile.FontFamily);
            VerseLabelText.FontSize = profile.LabelFontSize;
            VerseLabelText.Foreground = new SolidColorBrush(Color.Parse(profile.LabelColor));
            VerseLabelText.FontWeight = ParseFontWeight(profile.FontWeight);
            VerseLabelText.IsVisible = profile.ShowVerseNumbers;
        }

        // Apply to verse content
        if (VerseContentText != null)
        {
            VerseContentText.FontFamily = new FontFamily(profile.FontFamily);
            VerseContentText.FontSize = profile.VerseFontSize;
            VerseContentText.Foreground = new SolidColorBrush(Color.Parse(profile.TextColor));
            VerseContentText.FontWeight = ParseFontWeight(profile.FontWeight);
            VerseContentText.TextAlignment = ParseTextAlignment(profile.TextAlignment);
            VerseContentText.LineHeight = profile.LineHeight * profile.VerseFontSize;

            // Apply text effects (shadow/outline)
            if (profile.EnableTextShadow)
            {
                // Note: Avalonia doesn't have DropShadowEffect like WPF
                // Shadow would need to be implemented differently (e.g., using layered TextBlocks)
                // For now, this is a placeholder
            }
        }

        // Apply margins to content panel
        if (ContentPanel != null)
        {
            ContentPanel.Margin = new Thickness(
                profile.MarginLeft,
                profile.MarginTop,
                profile.MarginRight,
                profile.MarginBottom
            );

            // Apply vertical alignment
            ContentPanel.VerticalAlignment = ParseVerticalAlignment(profile.VerticalAlignment);
        }
    }

    private static FontWeight ParseFontWeight(string weight)
    {
        if (weight.Equals("bold", StringComparison.OrdinalIgnoreCase))
            return FontWeight.Bold;
        if (weight.Equals("semibold", StringComparison.OrdinalIgnoreCase))
            return FontWeight.SemiBold;

        return FontWeight.Normal;
    }

    private static TextAlignment ParseTextAlignment(string alignment)
    {
        if (alignment.Equals("center", StringComparison.OrdinalIgnoreCase))
            return TextAlignment.Center;
        if (alignment.Equals("right", StringComparison.OrdinalIgnoreCase))
            return TextAlignment.Right;

        return TextAlignment.Left;
    }

    private static Avalonia.Layout.VerticalAlignment ParseVerticalAlignment(string alignment)
    {
        if (alignment.Equals("top", StringComparison.OrdinalIgnoreCase))
            return Avalonia.Layout.VerticalAlignment.Top;
        if (alignment.Equals("bottom", StringComparison.OrdinalIgnoreCase))
            return Avalonia.Layout.VerticalAlignment.Bottom;

        return Avalonia.Layout.VerticalAlignment.Center;
    }

    private static Avalonia.Media.Stretch ParseStretchMode(string mode)
    {
        if (mode.Equals("fit", StringComparison.OrdinalIgnoreCase))
            return Avalonia.Media.Stretch.Uniform;
        if (mode.Equals("stretch", StringComparison.OrdinalIgnoreCase))
            return Avalonia.Media.Stretch.Fill;
        if (mode.Equals("tile", StringComparison.OrdinalIgnoreCase))
            return Avalonia.Media.Stretch.None; // Tiling would need a different approach

        return Avalonia.Media.Stretch.UniformToFill; // Default: Fill
    }
}
