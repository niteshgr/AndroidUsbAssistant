using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AndroidUsbAssistant.Core.Interfaces;
using AndroidUsbAssistant.Infrastructure.Services;
using AndroidUsbAssistant.App.Forms;

namespace AndroidUsbAssistant.App;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static async Task Main(string[] args)
    {
        // Customize application configuration like DPI and default font
        ApplicationConfiguration.Initialize();

        // Build the generic host
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .ConfigureServices((context, services) =>
            {
                // Core / Infrastructure services
                services.AddSingleton<IConfigurationService, ConfigurationService>();
                services.AddSingleton<IUsbDetector, WindowsUsbDetector>();

                // Application Context
                services.AddSingleton<TrayApplicationContext>();

                // UI Forms
                services.AddTransient<StatusForm>();
                services.AddTransient<SettingsForm>();
                services.AddTransient<AboutForm>();
            })
            .Build();

        var logger = host.Services.GetRequiredService<ILogger<TrayApplicationContext>>();
        logger.LogInformation("Starting Android USB Assistant Generic Host.");

        // Start host background lifecycle
        await host.StartAsync();

        // Run UI message loop using the registered TrayApplicationContext
        var trayContext = host.Services.GetRequiredService<TrayApplicationContext>();
        Application.Run(trayContext);

        // Clean up host on exit
        logger.LogInformation("Stopping Android USB Assistant Generic Host.");
        await host.StopAsync();
        host.Dispose();
    }
}