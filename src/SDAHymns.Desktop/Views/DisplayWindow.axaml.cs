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
                catch
                {
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
        return weight.ToLower() switch
        {
            "bold" => FontWeight.Bold,
            "semibold" => FontWeight.SemiBold,
            "normal" => FontWeight.Normal,
            _ => FontWeight.Normal
        };
    }

    private static TextAlignment ParseTextAlignment(string alignment)
    {
        return alignment.ToLower() switch
        {
            "center" => TextAlignment.Center,
            "right" => TextAlignment.Right,
            "left" => TextAlignment.Left,
            _ => TextAlignment.Left
        };
    }

    private static Avalonia.Layout.VerticalAlignment ParseVerticalAlignment(string alignment)
    {
        return alignment.ToLower() switch
        {
            "top" => Avalonia.Layout.VerticalAlignment.Top,
            "bottom" => Avalonia.Layout.VerticalAlignment.Bottom,
            "center" => Avalonia.Layout.VerticalAlignment.Center,
            _ => Avalonia.Layout.VerticalAlignment.Center
        };
    }

    private static Avalonia.Media.Stretch ParseStretchMode(string mode)
    {
        return mode.ToLower() switch
        {
            "fill" => Avalonia.Media.Stretch.UniformToFill,
            "fit" => Avalonia.Media.Stretch.Uniform,
            "stretch" => Avalonia.Media.Stretch.Fill,
            "tile" => Avalonia.Media.Stretch.None, // Tiling would need a different approach
            _ => Avalonia.Media.Stretch.UniformToFill
        };
    }
}
