namespace AndroidUsbAssistant.Core.Interfaces;

public interface INotificationService
{
    void ShowNotification(string title, string message, bool isError = false);
}
