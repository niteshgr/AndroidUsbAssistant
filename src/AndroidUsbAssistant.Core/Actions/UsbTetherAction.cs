using AndroidUsbAssistant.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace AndroidUsbAssistant.Core.Actions;

public class UsbTetherAction : IAutomationAction
{
    public string Id => "usb-tethering";
    public string Name => "Enable USB Tethering";
    public string Description => "Enables USB tethering via ADB shell svc usb setFunctions rndis.";

    private readonly IAdbService _adbService;
    private readonly IUserPromptService _promptService;
    private readonly INotificationService _notificationService;
    private readonly IConfigurationService _configService;
    private readonly ILogger<UsbTetherAction> _logger;

    public UsbTetherAction(
        IAdbService adbService,
        IUserPromptService promptService,
        INotificationService notificationService,
        IConfigurationService configService,
        ILogger<UsbTetherAction> _logger1)
    {
        _adbService = adbService;
        _promptService = promptService;
        _notificationService = notificationService;
        _configService = configService;
        _logger = _logger1;
    }

    public async Task ExecuteAsync(string deviceSerial, Dictionary<string, string> parameters)
    {
        try
        {
            // Check if AutoRun is set to "never"
            if (parameters.TryGetValue("AutoRun", out var autoRun) && string.Equals(autoRun, "never", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("USB tethering auto-run is set to 'never' for device {Serial}. Skipping.", deviceSerial);
                return;
            }

            // Check if USB tethering is already active
            _logger.LogInformation("Checking USB tethering status on device {Serial}.", deviceSerial);
            if (await _adbService.IsUsbTetheringActiveAsync(deviceSerial))
            {
                _logger.LogInformation("USB tethering is already active on device {Serial}. Skipping.", deviceSerial);
                return;
            }

            bool shouldTether = false;
            bool remember = false;

            // Check if AutoRun is set to "always"
            if (parameters.TryGetValue("AutoRun", out var ar) && string.Equals(ar, "always", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Auto-run enabled for device {Serial}. Activating USB tethering automatically.", deviceSerial);
                shouldTether = true;
            }
            else
            {
                // Prompt user using custom TetherPromptForm via IUserPromptService
                var promptResult = await _promptService.PromptTetherConfirmAsync(deviceSerial);
                shouldTether = promptResult.Result;
                remember = promptResult.Remember;
            }

            if (!shouldTether)
            {
                _logger.LogInformation("User chose not to enable USB tethering on device {Serial}.", deviceSerial);
                if (remember)
                {
                    await SaveAutoRunSettingAsync(deviceSerial, "never");
                }
                return;
            }

            if (remember)
            {
                await SaveAutoRunSettingAsync(deviceSerial, "always");
            }

            _logger.LogInformation("Enabling USB tethering on device {Serial}.", deviceSerial);
            var command = $"-s {deviceSerial} shell svc usb setFunctions rndis";
            _logger.LogInformation("Executing ADB command: adb {Command}", command);

            try
            {
                var result = await _adbService.ExecuteAdbCommandAsync(command);
                _logger.LogInformation("USB tethering command completed. ADB Output: {Output}", 
                    string.IsNullOrWhiteSpace(result) ? "Success (No Output)" : result.Trim());

                _notificationService.ShowNotification("USB Tethering Enabled", $"Tethering has been successfully enabled on device {deviceSerial}.");
            }
            catch (Exception ex)
            {
                // If the connection was lost/reset because the phone switched USB modes, this is a hardware success!
                var message = ex.Message.ToLower();
                if (message.Contains("offline") || 
                    message.Contains("lost") || 
                    message.Contains("closed") || 
                    message.Contains("not found") || 
                    message.Contains("reset"))
                {
                    _logger.LogInformation("Connection reset detected during USB mode switch. Treating as success. Details: {Details}", ex.Message);
                    _notificationService.ShowNotification("USB Tethering Enabled", $"Tethering has been successfully enabled on device {deviceSerial}.");
                }
                else
                {
                    throw;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable USB tethering on device {Serial}.", deviceSerial);
            _notificationService.ShowNotification("Tethering Failed", $"Failed to enable USB tethering on device {deviceSerial}.", isError: true);
            throw;
        }
    }

    private async Task SaveAutoRunSettingAsync(string deviceSerial, string autoRunValue)
    {
        try
        {
            var config = _configService.GetConfiguration();
            if (config.DeviceActions.TryGetValue(deviceSerial, out var actionConfigs))
            {
                var tetherConfig = actionConfigs.FirstOrDefault(c => string.Equals(c.ActionId, Id, StringComparison.OrdinalIgnoreCase));
                if (tetherConfig != null)
                {
                    tetherConfig.Parameters["AutoRun"] = autoRunValue;
                    await _configService.UpdateConfigurationAsync(config);
                    _logger.LogInformation("Saved AutoRun={Value} setting for device {Serial}.", autoRunValue, deviceSerial);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save AutoRun setting for device {Serial}.", deviceSerial);
        }
    }
}
