using Avalonia.Controls;
using SDAHymns.Desktop.ViewModels;

namespace SDAHymns.Desktop.Views;

public partial class MainWindow : Window
{
    private DisplayWindow? _displayWindow;

    public MainWindow()
    {
        InitializeComponent();

        // Handle window closing to clean up display window
        Closing += MainWindow_Closing;

        // Handle aspect ratio changes
        DataContextChanged += (s, e) =>
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        };
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsAspectRatio43) && _displayWindow != null)
        {
            var viewModel = (MainWindowViewModel)sender!;
            _displayWindow.Width = viewModel.DisplayWidth;
            _displayWindow.Height = viewModel.DisplayHeight;
        }
    }

    public void ToggleDisplayWindow()
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return;

        if (_displayWindow == null || !_displayWindow.IsVisible)
        {
            // Create and show display window with correct aspect ratio
            _displayWindow = new DisplayWindow
            {
                DataContext = viewModel,
                Width = viewModel.DisplayWidth,
                Height = viewModel.DisplayHeight
            };

            _displayWindow.Closed += (s, e) =>
            {
                viewModel.IsDisplayWindowOpen = false;
                _displayWindow = null;
            };

            _displayWindow.Show();
            viewModel.IsDisplayWindowOpen = true;
        }
        else
        {
            // Hide display window
            _displayWindow.Close();
            viewModel.IsDisplayWindowOpen = false;
            _displayWindow = null;
        }
    }

    private void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        // Close display window when main window closes
        _displayWindow?.Close();
    }

    private void ToggleDisplayWindow_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ToggleDisplayWindow();
    }
}
