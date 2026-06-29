namespace AndroidUsbAssistant.Core.Interfaces;

public interface IUserPromptService
{
    Task<bool> PromptYesNoAsync(string title, string message);
}
