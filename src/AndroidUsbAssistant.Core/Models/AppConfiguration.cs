namespace AndroidUsbAssistant.Core.Models;

public class AppConfiguration
{
    public string AdbPath { get; set; } = string.Empty;
    public bool StartWithWindows { get; set; } = true;
    public List<string> TrustedDevices { get; set; } = new();
}
