using Avalonia.Controls;
using SDAHymns.Desktop.ViewModels;

namespace SDAHymns.Desktop.Views;

public partial class ProfileEditorWindow : Window
{
    public ProfileEditorWindow()
    {
        InitializeComponent();
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // Load profiles when window opens
        if (DataContext is ProfileEditorViewModel viewModel)
        {
            await viewModel.LoadProfilesAsync();
        }
    }
}
