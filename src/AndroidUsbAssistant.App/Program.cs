using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AndroidUsbAssistant.Core.Interfaces;
using AndroidUsbAssistant.Core.Services;
using AndroidUsbAssistant.Core.Actions;
using AndroidUsbAssistant.Infrastructure.Services;
using AndroidUsbAssistant.App.Forms;
using AndroidUsbAssistant.App.Services;

namespace AndroidUsbAssistant.App;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
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
                services.AddSingleton<IAdbService, AdbService>();
                services.AddSingleton<IUserPromptService, UserPromptService>();
                services.AddSingleton<INotificationService, NotificationService>();
                services.AddSingleton<IActionEngine, ActionEngine>();
                services.AddSingleton<IAutomationAction, MockAction>();
                services.AddSingleton<IAutomationAction, UsbTetherAction>();
                services.AddSingleton<DeviceDisconnectTracker>();

                // Application Context
                services.AddSingleton<TrayApplicationContext>();

                // UI Forms
                services.AddTransient<StatusForm>();
                services.AddTransient<SettingsForm>();
                services.AddTransient<AboutForm>();
            })
            .Build();

        var logger = host.Services.GetRequiredService<ILogger<TrayApplicationContext>>();
        logger.LogInformation("Starting Android USB Assistant Generic Host (STA thread).");

        // Start host background lifecycle synchronously to preserve STA state on main thread
        host.Start();

        // Run UI message loop using the registered TrayApplicationContext
        var trayContext = host.Services.GetRequiredService<TrayApplicationContext>();
        Application.Run(trayContext);

        // Clean up host on exit
        logger.LogInformation("Stopping Android USB Assistant Generic Host.");
        host.StopAsync().GetAwaiter().GetResult();
        host.Dispose();
    }
}
