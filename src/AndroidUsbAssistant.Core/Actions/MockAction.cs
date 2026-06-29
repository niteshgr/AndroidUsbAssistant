using AndroidUsbAssistant.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AndroidUsbAssistant.Core.Actions;

public class MockAction : IAutomationAction
{
    public string Id => "mock-action";
    public string Name => "Mock Logger Action";
    public string Description => "Logs details of action execution parameters for testing.";

    private readonly ILogger<MockAction> _logger;

    public MockAction(ILogger<MockAction> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(string deviceSerial, Dictionary<string, string> parameters)
    {
        _logger.LogInformation("Mock Logger Action executed successfully on device {Serial}.", deviceSerial);
        foreach (var param in parameters)
        {
            _logger.LogInformation("Parameter -> Key: {Key}, Value: {Value}", param.Key, param.Value);
        }
        return Task.CompletedTask;
    }
}
