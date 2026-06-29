using AndroidUsbAssistant.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AndroidUsbAssistant.Core.Actions;

public class UsbTetherAction : IAutomationAction
{
    public string Id => "usb-tethering";
    public string Name => "Enable USB Tethering";
    public string Description => "Enables USB tethering via ADB shell svc usb setFunctions rndis.";

    private readonly IAdbService _adbService;
    private readonly ILogger<UsbTetherAction> _logger;

    public UsbTetherAction(IAdbService adbService, ILogger<UsbTetherAction> logger)
    {
        _adbService = adbService;
        _logger = logger;
    }

    public async Task ExecuteAsync(string deviceSerial, Dictionary<string, string> parameters)
    {
        _logger.LogInformation("Enabling USB tethering on device {Serial}.", deviceSerial);

        try
        {
            var command = $"-s {deviceSerial} shell svc usb setFunctions rndis";
            _logger.LogInformation("Executing ADB command: adb {Command}", command);

            var result = await _adbService.ExecuteAdbCommandAsync(command);

            _logger.LogInformation("USB tethering command completed. ADB Output: {Output}", 
                string.IsNullOrWhiteSpace(result) ? "Success (No Output)" : result.Trim());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable USB tethering on device {Serial}.", deviceSerial);
            throw;
        }
    }
}
