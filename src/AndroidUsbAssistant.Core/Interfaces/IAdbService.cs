using AndroidUsbAssistant.Core.Models;

namespace AndroidUsbAssistant.Core.Interfaces;

public interface IAdbService
{
    Task<List<AndroidDevice>> GetConnectedDevicesAsync();
    Task<bool> IsAdbAvailableAsync();
    Task<string> ExecuteAdbCommandAsync(string arguments);
    Task<bool> IsUsbTetheringActiveAsync(string deviceSerial);
    Task<bool> SetUsbTetheringAsync(string deviceSerial, bool enable);
}
