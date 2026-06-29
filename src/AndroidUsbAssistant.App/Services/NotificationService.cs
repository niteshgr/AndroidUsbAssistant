using System.Windows.Forms;
using AndroidUsbAssistant.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AndroidUsbAssistant.App.Services;

public class NotificationService : INotificationService
{
    private readonly IServiceProvider _serviceProvider;

    public NotificationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void ShowNotification(string title, string message, bool isError = false)
    {
        // Resolve TrayApplicationContext lazily to prevent circular dependencies in DI
        var context = _serviceProvider.GetService<TrayApplicationContext>();
        if (context != null)
        {
            var icon = isError ? ToolTipIcon.Error : ToolTipIcon.Info;
            context.ShowTrayNotification(title, message, icon);
        }
    }
}
