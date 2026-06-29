namespace AndroidUsbAssistant.Core.Interfaces;

public interface IActionEngine
{
    Task ExecuteActionsForDeviceAsync(string deviceSerial);
}
