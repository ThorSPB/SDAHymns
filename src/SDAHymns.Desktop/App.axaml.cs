using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SDAHymns.Core.Data;
using SDAHymns.Core.Services;
using SDAHymns.Desktop.ViewModels;
using SDAHymns.Desktop.Views;
using Velopack;

namespace SDAHymns.Desktop;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Velopack startup hook - MUST be called first
        VelopackApp.Build().Run();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            // Setup Dependency Injection
            var services = new ServiceCollection();

            // Database - locate hymns.db in Resources folder
            var dbPath = Path.Combine(AppContext.BaseDirectory, "Resources", "hymns.db");
            services.AddDbContext<HymnsContext>(options =>
            {
                options.UseSqlite($"Data Source={dbPath}");
            });

            // Services
            services.AddScoped<IHymnDisplayService, HymnDisplayService>();
            services.AddSingleton<IUpdateService, UpdateService>();

            // ViewModels
            services.AddTransient<MainWindowViewModel>();

            _serviceProvider = services.BuildServiceProvider();

            var mainWindow = new MainWindow
            {
                DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>()
            };

            desktop.MainWindow = mainWindow;

            // Check for updates in background (non-blocking)
            Task.Run(async () =>
            {
                try
                {
                    var updateService = _serviceProvider.GetRequiredService<IUpdateService>();
                    var updateInfo = await updateService.CheckForUpdatesAsync();

                    if (updateInfo != null)
                    {
                        // Dispatch to UI thread to show notification
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (mainWindow.DataContext is MainWindowViewModel viewModel)
                            {
                                viewModel.ShowUpdateNotification(updateInfo);
                            }
                        });
                    }
                }
                catch (Exception)
                {
                    // Silently fail - update check should never crash the app
                }
            });
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
