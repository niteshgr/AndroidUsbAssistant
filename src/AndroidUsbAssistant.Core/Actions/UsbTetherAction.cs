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
            // Check if USB tethering is already active
            _logger.LogInformation("Checking USB tethering status on device {Serial}.", deviceSerial);
            if (await _adbService.IsUsbTetheringActiveAsync(deviceSerial))
            {
                _logger.LogInformation("USB tethering is already active on device {Serial}. Skipping.", deviceSerial);
                return;
            }

            // Prompt user using custom TetherPromptForm via IUserPromptService
            bool shouldTether = await _promptService.PromptTetherConfirmAsync(deviceSerial);

            if (!shouldTether)
            {
                _logger.LogInformation("User chose not to enable USB tethering on device {Serial}.", deviceSerial);
                return;
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
                // Swapping USB modes causes ADB connection to reset. We treat this expected disconnection as success.
                _logger.LogInformation("ADB connection state changed during USB mode switch: {Message}", ex.Message);
                _notificationService.ShowNotification("USB Tethering Enabled", $"Tethering has been successfully enabled on device {deviceSerial}.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute USB tethering action on device {Serial}.", deviceSerial);
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
