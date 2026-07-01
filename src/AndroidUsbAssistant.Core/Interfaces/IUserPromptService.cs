using AndroidUsbAssistant.Core.Models;

namespace AndroidUsbAssistant.Core.Interfaces;

public interface IUserPromptService
{
    Task<bool> PromptYesNoAsync(string title, string message);
    Task<(UsbConnectionMode Mode, bool Remember)> PromptTetherConfirmAsync(string deviceSerial);
}
