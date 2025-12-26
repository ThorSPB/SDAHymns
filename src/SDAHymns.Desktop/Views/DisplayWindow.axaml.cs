using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using SDAHymns.Core.Data.Models;
using SDAHymns.Desktop.ViewModels;

namespace SDAHymns.Desktop.Views;

public partial class DisplayWindow : Window
{
    public DisplayWindow()
    {
        InitializeComponent();

        // Add keyboard shortcuts for presentation control (PowerPoint-style)
        KeyDown += DisplayWindow_KeyDown;

        // Subscribe to DataContext changes to hook up close event
        DataContextChanged += DisplayWindow_DataContextChanged;
    }

    private void DisplayWindow_DataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            // Subscribe to auto-close event
            viewModel.DisplayWindowCloseRequested += OnDisplayWindowCloseRequested;
        }
    }

    private void OnDisplayWindowCloseRequested()
    {
        // Close the window from UI thread
        Avalonia.Threading.Dispatcher.UIThread.Post(() => Close());
    }

    protected override void OnClosed(EventArgs e)
    {
        // Unsubscribe from events to prevent memory leaks
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.DisplayWindowCloseRequested -= OnDisplayWindowCloseRequested;
        }

        base.OnClosed(e);
    }

    private void DisplayWindow_KeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return;

        switch (e.Key)
        {
            // Next verse: Space, Right Arrow, Down Arrow, Page Down, Enter
            case Key.Space:
            case Key.Right:
            case Key.Down:
            case Key.PageDown:
            case Key.Enter:
                if (viewModel.NextVerseCommand.CanExecute(null))
                {
                    viewModel.NextVerseCommand.Execute(null);
                    e.Handled = true;
                }
                break;

            // Previous verse: Left Arrow, Up Arrow, Page Up, Backspace
            case Key.Left:
            case Key.Up:
            case Key.PageUp:
            case Key.Back:
                if (viewModel.PreviousVerseCommand.CanExecute(null))
                {
                    viewModel.PreviousVerseCommand.Execute(null);
                    e.Handled = true;
                }
                break;

            // Close/Blank: Escape, B (for blank)
            case Key.Escape:
            case Key.B:
                Close();
                e.Handled = true;
                break;
        }
    }

    /// <summary>
    /// Applies a display profile to the window, updating all visual properties
    /// </summary>
    public void ApplyProfile(DisplayProfile profile)
    {
        if (profile == null)
            return;

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

        // Apply verse number styling (Spec 018)
        ApplyVerseNumberStyle(profile);

        // Apply chorus formatting (Spec 018)
        ApplyChorusFormatting(profile);

        // Apply to verse content (with typography enhancements - Phase 7)
        if (VerseContentText != null)
        {
            VerseContentText.FontFamily = new FontFamily(profile.FontFamily);
            VerseContentText.FontSize = profile.VerseFontSize;
            VerseContentText.Foreground = new SolidColorBrush(Color.Parse(profile.TextColor));
            VerseContentText.FontWeight = ParseFontWeight(profile.FontWeight);
            VerseContentText.TextAlignment = ParseTextAlignment(profile.TextAlignment);

            // Typography enhancements (Spec 018 - Phase 7)
            VerseContentText.LineHeight = profile.LineHeight * profile.VerseFontSize;
            VerseContentText.LetterSpacing = profile.LetterSpacing;

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

    /// <summary>
    /// Applies verse number styling based on profile settings (Spec 018 - Phase 5)
    /// </summary>
    private void ApplyVerseNumberStyle(DisplayProfile profile)
    {
        if (VerseLabelText == null || VerseLabelBadge == null || VerseLabelBadgeText == null || VerseLabelContainer == null)
            return;

        var verseNumberSize = profile.VerseNumberSize > 0 ? profile.VerseNumberSize : profile.LabelFontSize;
        var verseNumberColor = Color.Parse(profile.VerseNumberColor);

        // Hide all verse number elements first
        VerseLabelText.IsVisible = false;
        VerseLabelBadge.IsVisible = false;
        VerseLabelContainer.IsVisible = profile.ShowVerseNumbers;

        // Set margin based on ParagraphSpacing
        VerseLabelContainer.Margin = new Thickness(0, 0, 0, profile.ParagraphSpacing);

        switch (profile.VerseNumberStyle)
        {
            case "None":
                VerseLabelContainer.IsVisible = false;
                break;

            case "InlinePlain":
                VerseLabelText.IsVisible = true;
                VerseLabelText.FontFamily = new FontFamily(profile.FontFamily);
                VerseLabelText.FontSize = verseNumberSize;
                VerseLabelText.Foreground = new SolidColorBrush(verseNumberColor);
                VerseLabelText.FontWeight = FontWeight.Normal;
                break;

            case "InlineBold":
                VerseLabelText.IsVisible = true;
                VerseLabelText.FontFamily = new FontFamily(profile.FontFamily);
                VerseLabelText.FontSize = verseNumberSize;
                VerseLabelText.Foreground = new SolidColorBrush(Color.Parse(profile.AccentColor));
                VerseLabelText.FontWeight = FontWeight.Bold;
                break;

            case "Badge":
                VerseLabelBadge.IsVisible = true;
                VerseLabelBadge.Background = new SolidColorBrush(Color.Parse(profile.AccentColor));
                VerseLabelBadgeText.FontFamily = new FontFamily(profile.FontFamily);
                VerseLabelBadgeText.FontSize = verseNumberSize;
                VerseLabelBadgeText.FontWeight = FontWeight.Bold;
                break;

            case "LargeDecorative":
                VerseLabelText.IsVisible = true;
                VerseLabelText.FontFamily = new FontFamily(profile.FontFamily);
                VerseLabelText.FontSize = verseNumberSize * 2; // 2x larger
                VerseLabelText.Foreground = new SolidColorBrush(verseNumberColor);
                VerseLabelText.FontWeight = FontWeight.Light;
                VerseLabelText.Opacity = 0.6;
                if (profile.VerseNumberSeparateLine)
                {
                    VerseLabelContainer.Orientation = Avalonia.Layout.Orientation.Vertical;
                }
                break;

            case "Superscript":
                VerseLabelText.IsVisible = true;
                VerseLabelText.FontFamily = new FontFamily(profile.FontFamily);
                VerseLabelText.FontSize = verseNumberSize * 0.6; // Smaller
                VerseLabelText.Foreground = new SolidColorBrush(verseNumberColor);
                VerseLabelText.FontWeight = FontWeight.Normal;
                VerseLabelText.Margin = new Thickness(0, -10, 5, 0); // Raised position
                break;

            default:
                // Fallback to InlinePlain
                VerseLabelText.IsVisible = true;
                VerseLabelText.FontFamily = new FontFamily(profile.FontFamily);
                VerseLabelText.FontSize = verseNumberSize;
                VerseLabelText.Foreground = new SolidColorBrush(verseNumberColor);
                VerseLabelText.FontWeight = FontWeight.Normal;
                break;
        }
    }

    /// <summary>
    /// Applies chorus formatting based on profile settings (Spec 018 - Phase 6)
    /// </summary>
    private void ApplyChorusFormatting(DisplayProfile profile)
    {
        if (VerseContentText == null || ChorusBackground == null)
            return;

        // Check if current verse is a chorus (label contains "Refren" or "Refrain")
        var isChorus = DataContext is MainWindowViewModel vm &&
                       vm.CurrentVerseLabel?.Contains("Refren", StringComparison.OrdinalIgnoreCase) == true;

        if (!isChorus)
        {
            // Reset to normal verse styling
            ChorusBackground.Background = new SolidColorBrush(Colors.Transparent);
            ChorusBackground.Padding = new Thickness(0);
            VerseContentText.Foreground = new SolidColorBrush(Color.Parse(profile.TextColor));
            VerseContentText.FontStyle = FontStyle.Normal;
            VerseContentText.Margin = new Thickness(0);
            return;
        }

        // Apply chorus styling based on ChorusStyle
        switch (profile.ChorusStyle)
        {
            case "SameAsVerse":
                // No special formatting
                ChorusBackground.Background = new SolidColorBrush(Colors.Transparent);
                ChorusBackground.Padding = new Thickness(0);
                VerseContentText.Foreground = new SolidColorBrush(Color.Parse(profile.TextColor));
                VerseContentText.FontStyle = FontStyle.Normal;
                VerseContentText.Margin = new Thickness(0);
                break;

            case "Indented":
                ChorusBackground.Background = new SolidColorBrush(Colors.Transparent);
                ChorusBackground.Padding = new Thickness(0);
                VerseContentText.Foreground = new SolidColorBrush(Color.Parse(profile.TextColor));
                VerseContentText.FontStyle = FontStyle.Normal;
                VerseContentText.Margin = new Thickness(profile.ChorusIndentAmount, 0, 0, 0);
                break;

            case "Italic":
                ChorusBackground.Background = new SolidColorBrush(Colors.Transparent);
                ChorusBackground.Padding = new Thickness(0);
                VerseContentText.Foreground = new SolidColorBrush(Color.Parse(profile.TextColor));
                VerseContentText.FontStyle = FontStyle.Italic;
                VerseContentText.Margin = new Thickness(0);
                break;

            case "ColoredText":
                ChorusBackground.Background = new SolidColorBrush(Colors.Transparent);
                ChorusBackground.Padding = new Thickness(0);
                VerseContentText.Foreground = new SolidColorBrush(Color.Parse(profile.ChorusTextColor));
                VerseContentText.FontStyle = FontStyle.Normal;
                VerseContentText.Margin = new Thickness(0);
                break;

            case "BackgroundHighlight":
                var bgColor = Color.Parse(profile.ChorusBackgroundColor);
                ChorusBackground.Background = new SolidColorBrush(bgColor);
                ChorusBackground.Padding = new Thickness(30, 20);
                VerseContentText.Foreground = new SolidColorBrush(Color.Parse(profile.TextColor));
                VerseContentText.FontStyle = FontStyle.Normal;
                VerseContentText.Margin = new Thickness(0);
                break;

            case "Combined":
                // Indented + Italic + Colored
                var combinedBgColor = Color.Parse(profile.ChorusBackgroundColor);
                ChorusBackground.Background = new SolidColorBrush(combinedBgColor);
                ChorusBackground.Padding = new Thickness(30, 20);
                VerseContentText.Foreground = new SolidColorBrush(Color.Parse(profile.ChorusTextColor));
                VerseContentText.FontStyle = profile.ChorusItalic ? FontStyle.Italic : FontStyle.Normal;
                VerseContentText.Margin = new Thickness(profile.ChorusIndentAmount, 0, 0, 0);
                break;

            default:
                // Fallback to SameAsVerse
                ChorusBackground.Background = new SolidColorBrush(Colors.Transparent);
                ChorusBackground.Padding = new Thickness(0);
                VerseContentText.Foreground = new SolidColorBrush(Color.Parse(profile.TextColor));
                VerseContentText.FontStyle = FontStyle.Normal;
                VerseContentText.Margin = new Thickness(0);
                break;
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
