namespace AndroidUsbAssistant.Core.Interfaces;

public interface IAutomationAction
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    Task ExecuteAsync(string deviceSerial, Dictionary<string, string> parameters);
}
