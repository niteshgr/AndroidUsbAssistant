using System.Text.Json;
using AndroidUsbAssistant.Core.Interfaces;
using AndroidUsbAssistant.Core.Models;
using Microsoft.Extensions.Logging;

namespace AndroidUsbAssistant.Infrastructure.Services;

public class ConfigurationService : IConfigurationService
{
    private readonly string _filePath;
    private readonly ILogger<ConfigurationService> _logger;
    private readonly object _lock = new();
    private AppConfiguration _cachedConfig;

    public ConfigurationService(ILogger<ConfigurationService> logger)
    {
        _logger = logger;
        
        var appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AndroidUsbAssistant"
        );
        _filePath = Path.Combine(appDataFolder, "settings.json");
        
        _cachedConfig = LoadOrCreateConfiguration();
    }

    public AppConfiguration GetConfiguration()
    {
        lock (_lock)
        {
            return CloneConfiguration(_cachedConfig);
        }
    }

    public async Task UpdateConfigurationAsync(AppConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _logger.LogInformation("Updating application configuration.");
        
        AppConfiguration cloned;
        lock (_lock)
        {
            _cachedConfig = CloneConfiguration(configuration);
            cloned = _cachedConfig;
        }

        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogInformation("Created directory {DirectoryPath}", directory);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(cloned, options);
            await File.WriteAllTextAsync(_filePath, json);
            _logger.LogInformation("Configuration saved successfully to {FilePath}", _filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration to {FilePath}", _filePath);
            throw;
        }
    }

    private AppConfiguration LoadOrCreateConfiguration()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var config = JsonSerializer.Deserialize<AppConfiguration>(json);
                if (config != null)
                {
                    _logger.LogInformation("Configuration loaded successfully from {FilePath}", _filePath);
                    return config;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load configuration from {FilePath}. Reverting to defaults.", _filePath);
        }

        _logger.LogInformation("Creating default configuration.");
        var defaultConfig = new AppConfiguration();
        SaveDefaultConfiguration(defaultConfig);
        return defaultConfig;
    }

    private void SaveDefaultConfiguration(AppConfiguration config)
    {
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(_filePath, json);
            _logger.LogInformation("Default configuration written to {FilePath}", _filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write default configuration to {FilePath}", _filePath);
        }
    }

    private static AppConfiguration CloneConfiguration(AppConfiguration source)
    {
        var clonedActions = new Dictionary<string, List<DeviceActionConfig>>();
        if (source.DeviceActions != null)
        {
            foreach (var kvp in source.DeviceActions)
            {
                var actionList = kvp.Value.Select(a => new DeviceActionConfig
                {
                    ActionId = a.ActionId,
                    Enabled = a.Enabled,
                    Parameters = a.Parameters != null ? new Dictionary<string, string>(a.Parameters) : new Dictionary<string, string>()
                }).ToList();
                clonedActions[kvp.Key] = actionList;
            }
        }

        return new AppConfiguration
        {
            AdbPath = source.AdbPath,
            StartWithWindows = source.StartWithWindows,
            TrustedDevices = source.TrustedDevices != null ? new List<string>(source.TrustedDevices) : new List<string>(),
            DeviceActions = clonedActions
        };
    }
}
