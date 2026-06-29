namespace AndroidUsbAssistant.Core.Models;

public class DeviceActionConfig
{
    public string ActionId { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public Dictionary<string, string> Parameters { get; set; } = new();
}
