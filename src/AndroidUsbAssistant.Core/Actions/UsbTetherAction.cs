using AndroidUsbAssistant.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AndroidUsbAssistant.Core.Actions;

public class UsbTetherAction : IAutomationAction
{
    public string Id => "usb-tethering";
    public string Name => "Enable USB Tethering";
    public string Description => "Enables USB tethering via ADB shell svc usb setFunctions rndis.";

    private readonly ILogger<UsbTetherAction> _logger;

    public UsbTetherAction(ILogger<UsbTetherAction> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(string deviceSerial, Dictionary<string, string> parameters)
    {
        _logger.LogInformation("UsbTetherAction: Mock executing tethering action for device {Serial}.", deviceSerial);
        _logger.LogInformation("Executing ADB Shell: adb -s {Serial} shell svc usb setFunctions rndis (Mocked for Milestone 5).", deviceSerial);
        return Task.CompletedTask;
    }
}
