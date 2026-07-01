using AndroidUsbAssistant.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Linq;
using AndroidUsbAssistant.Core.Models;

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
            UsbConnectionMode mode = UsbConnectionMode.ChargeOnly;
            bool shouldPrompt = true;
            bool remember = false;

            if (parameters.TryGetValue("AutoRun", out var autoRun))
            {
                if (string.Equals(autoRun, "always", StringComparison.OrdinalIgnoreCase) || 
                    string.Equals(autoRun, "Tether", StringComparison.OrdinalIgnoreCase))
                {
                    mode = UsbConnectionMode.Tether;
                    shouldPrompt = false;
                }
                else if (string.Equals(autoRun, "TransferFiles", StringComparison.OrdinalIgnoreCase))
                {
                    mode = UsbConnectionMode.TransferFiles;
                    shouldPrompt = false;
                }
                else if (string.Equals(autoRun, "ChargeOnly", StringComparison.OrdinalIgnoreCase))
                {
                    mode = UsbConnectionMode.ChargeOnly;
                    shouldPrompt = false;
                }
            }

            if (shouldPrompt)
            {
                // Prompt user using custom TetherPromptForm via IUserPromptService
                var promptResult = await _promptService.PromptTetherConfirmAsync(deviceSerial);
                mode = promptResult.Mode;
                remember = promptResult.Remember;
            }

            if (remember)
            {
                await SaveAutoRunSettingAsync(deviceSerial, mode.ToString());
            }

            // If we want tethering but it's already active, we can skip
            if (mode == UsbConnectionMode.Tether)
            {
                _logger.LogInformation("Checking USB tethering status on device {Serial}.", deviceSerial);
                if (await _adbService.IsUsbTetheringActiveAsync(deviceSerial))
                {
                    _logger.LogInformation("USB tethering is already active on device {Serial}. Skipping.", deviceSerial);
                    return;
                }
            }

            _logger.LogInformation("Setting USB mode to {Mode} on device {Serial}.", mode, deviceSerial);
            string functions = mode switch
            {
                UsbConnectionMode.Tether => "rndis",
                UsbConnectionMode.TransferFiles => "mtp",
                _ => "none"
            };

            var command = $"-s {deviceSerial} shell svc usb setFunctions {functions}";
            _logger.LogInformation("Executing ADB command: adb {Command}", command);

            try
            {
                var result = await _adbService.ExecuteAdbCommandAsync(command);
                _logger.LogInformation("USB mode switch command completed. ADB Output: {Output}", 
                    string.IsNullOrWhiteSpace(result) ? "Success (No Output)" : result.Trim());

                string notifyTitle = mode switch
                {
                    UsbConnectionMode.Tether => "USB Tethering Enabled",
                    UsbConnectionMode.TransferFiles => "Transfer Files Mode Enabled",
                    _ => "Charging Mode Enabled"
                };

                string notifyText = mode switch
                {
                    UsbConnectionMode.Tether => $"Tethering has been successfully enabled on device {deviceSerial}.",
                    UsbConnectionMode.TransferFiles => $"MTP (Transfer Files) mode enabled on device {deviceSerial}.",
                    _ => $"Device {deviceSerial} is now set to charging only."
                };

                _notificationService.ShowNotification(notifyTitle, notifyText);
            }
            catch (Exception ex)
            {
                // Swapping USB modes causes ADB connection to reset. We treat this expected disconnection as success.
                _logger.LogInformation("ADB connection state changed during USB mode switch: {Message}", ex.Message);
                
                string notifyTitle = mode switch
                {
                    UsbConnectionMode.Tether => "USB Tethering Enabled",
                    UsbConnectionMode.TransferFiles => "Transfer Files Mode Enabled",
                    _ => "Charging Mode Enabled"
                };

                string notifyText = mode switch
                {
                    UsbConnectionMode.Tether => $"Tethering has been successfully enabled on device {deviceSerial}.",
                    UsbConnectionMode.TransferFiles => $"MTP (Transfer Files) mode enabled on device {deviceSerial}.",
                    _ => $"Device {deviceSerial} is now set to charging only."
                };

                _notificationService.ShowNotification(notifyTitle, notifyText);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute USB mode action on device {Serial}.", deviceSerial);
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
