namespace AndroidUsbAssistant.Core.Interfaces;

public interface IUserPromptService
{
    Task<bool> PromptYesNoAsync(string title, string message);
    Task<bool> PromptTetherConfirmAsync(string deviceSerial);
}
