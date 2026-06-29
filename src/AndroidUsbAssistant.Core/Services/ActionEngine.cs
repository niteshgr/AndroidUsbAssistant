using AndroidUsbAssistant.Core.Interfaces;
using AndroidUsbAssistant.Core.Models;
using Microsoft.Extensions.Logging;

namespace AndroidUsbAssistant.Core.Services;

public class ActionEngine : IActionEngine
{
    private readonly IConfigurationService _configService;
    private readonly IEnumerable<IAutomationAction> _actions;
    private readonly ILogger<ActionEngine> _logger;

    public ActionEngine(
        IConfigurationService configService,
        IEnumerable<IAutomationAction> actions,
        ILogger<ActionEngine> logger)
    {
        _configService = configService;
        _actions = actions;
        _logger = logger;
    }

    public async Task ExecuteActionsForDeviceAsync(string deviceSerial)
    {
        _logger.LogInformation("Action Engine starting execution for device {Serial}.", deviceSerial);

        var config = _configService.GetConfiguration();

        // If no actions configured for this device yet, register default actions
        if (!config.DeviceActions.TryGetValue(deviceSerial, out var actionConfigs) || actionConfigs.Count == 0)
        {
            _logger.LogInformation("No custom actions configured for device {Serial}. Initializing default actions.", deviceSerial);

            actionConfigs = new List<DeviceActionConfig>
            {
                new() { ActionId = "usb-tethering", Enabled = true },
                new() { ActionId = "mock-action", Enabled = true, Parameters = new() { { "testKey", "testValue" } } }
            };

            config.DeviceActions[deviceSerial] = actionConfigs;
            await _configService.UpdateConfigurationAsync(config);
        }

        foreach (var actionConfig in actionConfigs)
        {
            if (!actionConfig.Enabled)
            {
                _logger.LogInformation("Action {ActionId} is disabled for device {Serial}.", actionConfig.ActionId, deviceSerial);
                continue;
            }

            var action = _actions.FirstOrDefault(a => string.Equals(a.Id, actionConfig.ActionId, StringComparison.OrdinalIgnoreCase));
            if (action == null)
            {
                _logger.LogWarning("Action {ActionId} is configured but no matching implementation was found in the application.", actionConfig.ActionId);
                continue;
            }

            try
            {
                _logger.LogInformation("Executing action {ActionName} ({ActionId}) on device {Serial}.", action.Name, action.Id, deviceSerial);
                await action.ExecuteAsync(deviceSerial, actionConfig.Parameters);
                _logger.LogInformation("Action {ActionId} completed successfully for device {Serial}.", action.Id, deviceSerial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute action {ActionId} for device {Serial}.", action.Id, deviceSerial);
            }
        }
    }
}
