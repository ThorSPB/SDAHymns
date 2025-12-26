using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

            // Configure update options
            services.Configure<SDAHymns.Core.Services.UpdateOptions>(options =>
            {
                options.GitHubRepoUrl = "https://github.com/ThorSPB/SDAHymns";
            });

            // Services
            services.AddScoped<IHymnDisplayService, HymnDisplayService>();
            services.AddScoped<ISearchService, SearchService>();
            services.AddScoped<IDisplayProfileService, DisplayProfileService>();
            services.AddSingleton<IUpdateService, UpdateService>();
            services.AddSingleton<IHotKeyManager, HotKeyManager>();
            services.AddSingleton<IAudioPlayerService, AudioPlayerService>();

            // ViewModels
            services.AddTransient<MainWindowViewModel>();

            _serviceProvider = services.BuildServiceProvider();

            var hotKeyManager = _serviceProvider.GetRequiredService<IHotKeyManager>();
            var mainWindow = new MainWindow(hotKeyManager)
            {
                DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>()
            };

            // Provide service provider to main window for creating child windows
            mainWindow.SetServiceProvider(_serviceProvider);

            desktop.MainWindow = mainWindow;

            // Check for updates in background (non-blocking)
            Task.Run(async () =>
            {
                // Create a proper DI scope for the background task
                await using var scope = _serviceProvider.CreateAsyncScope();
                var logger = scope.ServiceProvider.GetService<ILogger<App>>();

                try
                {
                    var updateService = scope.ServiceProvider.GetRequiredService<IUpdateService>();
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
                catch (Exception ex)
                {
                    // Log the error but don't crash the app
                    logger?.LogWarning(ex, "Background update check failed during app startup");
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
