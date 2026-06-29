using AndroidUsbAssistant.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AndroidUsbAssistant.Core.Actions;

public class UsbTetherAction : IAutomationAction
{
    public string Id => "usb-tethering";
    public string Name => "Enable USB Tethering";
    public string Description => "Enables USB tethering via ADB shell svc usb setFunctions rndis.";

    private readonly IAdbService _adbService;
    private readonly IUserPromptService _promptService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<UsbTetherAction> _logger;

    public UsbTetherAction(
        IAdbService adbService,
        IUserPromptService promptService,
        INotificationService notificationService,
        ILogger<UsbTetherAction> logger)
    {
        _adbService = adbService;
        _promptService = promptService;
        _notificationService = notificationService;
        _logger = logger;
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

            // Prompt user before enabling
            var shouldTether = await _promptService.PromptYesNoAsync(
                "Enable USB Tethering",
                $"A trusted Android device ({deviceSerial}) has been connected.\n\nWould you like to enable USB Tethering on it?"
            );

            if (!shouldTether)
            {
                _logger.LogInformation("User chose not to enable USB tethering on device {Serial}.", deviceSerial);
                return;
            }

            _logger.LogInformation("Enabling USB tethering on device {Serial}.", deviceSerial);
            var command = $"-s {deviceSerial} shell svc usb setFunctions rndis";
            _logger.LogInformation("Executing ADB command: adb {Command}", command);

            var result = await _adbService.ExecuteAdbCommandAsync(command);

            _logger.LogInformation("USB tethering command completed. ADB Output: {Output}", 
                string.IsNullOrWhiteSpace(result) ? "Success (No Output)" : result.Trim());

            _notificationService.ShowNotification("USB Tethering Enabled", $"Tethering has been successfully enabled on device {deviceSerial}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable USB tethering on device {Serial}.", deviceSerial);
            _notificationService.ShowNotification("Tethering Failed", $"Failed to enable USB tethering on device {deviceSerial}.", isError: true);
            throw;
        }
    }
}
