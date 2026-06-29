using Microsoft.Win32;

namespace AndroidUsbAssistant.App.Services;

public static class StartupRegistryHelper
{
    private const string RegistryKeyName = "AndroidUsbAssistant";

    public static void SetStartup(bool startWithWindows)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (key == null) return;

            if (startWithWindows)
            {
                var exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(RegistryKeyName, $"\"{exePath}\"");
                }
            }
            else
            {
                key.DeleteValue(RegistryKeyName, false);
            }
        }
        catch
        {
            // Ignore permission or OS exception limits gracefully
        }
    }
}
