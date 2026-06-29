using AndroidUsbAssistant.Core.Models;

namespace AndroidUsbAssistant.Core.Interfaces;

public interface IConfigurationService
{
    AppConfiguration GetConfiguration();
    Task UpdateConfigurationAsync(AppConfiguration configuration);
}
