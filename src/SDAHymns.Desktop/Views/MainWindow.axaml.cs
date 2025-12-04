using Avalonia.Controls;
using Avalonia.Input;
using SDAHymns.Desktop.ViewModels;

namespace SDAHymns.Desktop.Views;

public partial class MainWindow : Window
{
    private DisplayWindow? _displayWindow;

    public MainWindow()
    {
        InitializeComponent();

        // Add KeyDown handler for Enter key
        HymnNumberTextBox.KeyDown += HymnNumberTextBox_KeyDown;

        // Handle window closing to clean up display window
        Closing += MainWindow_Closing;
    }

    private async void HymnNumberTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.LoadHymnCommand.ExecuteAsync(null);
        }
    }

    public void ToggleDisplayWindow()
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return;

        if (_displayWindow == null || !_displayWindow.IsVisible)
        {
            // Create and show display window
            _displayWindow = new DisplayWindow
            {
                DataContext = viewModel
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
