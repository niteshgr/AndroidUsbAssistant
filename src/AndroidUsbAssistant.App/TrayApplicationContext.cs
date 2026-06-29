using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using AndroidUsbAssistant.App.Forms;
using AndroidUsbAssistant.Core.Interfaces;
using AndroidUsbAssistant.Core.Models;

namespace AndroidUsbAssistant.App;

public class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ILogger<TrayApplicationContext> _logger;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IServiceProvider _serviceProvider;
    private readonly IUsbDetector _usbDetector;
    private readonly IConfigurationService _configService;
    private readonly IAdbService _adbService;
    private readonly Icon _customIcon;
    private readonly HashSet<string> _notifiedDevices = new();

    private StatusForm? _statusForm;
    private SettingsForm? _settingsForm;
    private AboutForm? _aboutForm;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);

    public TrayApplicationContext(
        ILogger<TrayApplicationContext> logger,
        IHostApplicationLifetime lifetime,
        IServiceProvider serviceProvider,
        IUsbDetector usbDetector,
        IConfigurationService configService,
        IAdbService adbService)
    {
        _logger = logger;
        _lifetime = lifetime;
        _serviceProvider = serviceProvider;
        _usbDetector = usbDetector;
        _configService = configService;
        _adbService = adbService;

        _logger.LogInformation("Initializing TrayApplicationContext.");

        // Register host lifetime stopping event to exit UI thread if host is terminated externally
        _lifetime.ApplicationStopping.Register(OnApplicationStopping);

        _customIcon = CreateCustomIcon();

        _notifyIcon = new NotifyIcon
        {
            Icon = _customIcon,
            Text = "Android USB Assistant",
            Visible = true
        };

        _notifyIcon.ContextMenuStrip = CreateContextMenu();
        _notifyIcon.DoubleClick += (s, e) => ShowStatusForm();

        // Subscribe to USB changes and start the detector
        _usbDetector.DeviceChanged += OnUsbDeviceChanged;
        _usbDetector.Start();
    }

    private ContextMenuStrip CreateContextMenu()
    {
        var menu = new ContextMenuStrip
        {
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.FromArgb(230, 230, 230),
            ShowImageMargin = false
        };

        var statusItem = new ToolStripMenuItem("Status");
        statusItem.Click += (s, e) => ShowStatusForm();
        statusItem.Font = new Font(statusItem.Font, FontStyle.Bold);

        var settingsItem = new ToolStripMenuItem("Settings");
        settingsItem.Click += (s, e) => ShowSettingsForm();

        var aboutItem = new ToolStripMenuItem("About");
        aboutItem.Click += (s, e) => ShowAboutForm();

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (s, e) => ExitApplication();

        menu.Items.Add(statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(settingsItem);
        menu.Items.Add(aboutItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        // Styling for separation and colors
        menu.Renderer = new DarkMenuRenderer();

        return menu;
    }

    private Icon CreateCustomIcon()
    {
        using var bitmap = new Bitmap(16, 16);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);

        // Draw modern circular teal background
        using var backgroundBrush = new SolidBrush(Color.FromArgb(0, 150, 136));
        graphics.FillEllipse(backgroundBrush, 0, 0, 16, 16);

        // Draw light phone/USB symbolic lines in center
        using var pen = new Pen(Color.White, 2);
        graphics.DrawLine(pen, 8, 3, 8, 13);
        graphics.DrawLine(pen, 5, 8, 11, 8);

        var hIcon = bitmap.GetHicon();
        var icon = Icon.FromHandle(hIcon);
        
        // Hicon is copied by Icon.FromHandle, but we still need to free the raw pointer to prevent leaks
        DestroyIcon(hIcon);
        return icon;
    }

    private void ShowStatusForm()
    {
        if (_statusForm == null || _statusForm.IsDisposed)
        {
            _statusForm = _serviceProvider.GetRequiredService<StatusForm>();
            _statusForm.Show();
        }
        else
        {
            _statusForm.WindowState = FormWindowState.Normal;
            _statusForm.Activate();
        }
    }

    private void ShowSettingsForm()
    {
        if (_settingsForm == null || _settingsForm.IsDisposed)
        {
            _settingsForm = _serviceProvider.GetRequiredService<SettingsForm>();
            _settingsForm.Show();
        }
        else
        {
            _settingsForm.WindowState = FormWindowState.Normal;
            _settingsForm.Activate();
        }
    }

    private void ShowAboutForm()
    {
        if (_aboutForm == null || _aboutForm.IsDisposed)
        {
            _aboutForm = _serviceProvider.GetRequiredService<AboutForm>();
            _aboutForm.Show();
        }
        else
        {
            _aboutForm.WindowState = FormWindowState.Normal;
            _aboutForm.Activate();
        }
    }

    private async void OnUsbDeviceChanged(object? sender, EventArgs e)
    {
        _logger.LogInformation("USB device change event triggered in Tray.");
        await ProcessDeviceChangesAsync();
    }

    private async Task ProcessDeviceChangesAsync()
    {
        try
        {
            if (!await _adbService.IsAdbAvailableAsync())
            {
                _logger.LogWarning("USB change detected but ADB daemon is not available.");
                return;
            }

            var connectedDevices = await _adbService.GetConnectedDevicesAsync();
            var currentSerials = connectedDevices.Select(d => d.SerialNumber).ToHashSet();

            lock (_notifiedDevices)
            {
                _notifiedDevices.RemoveWhere(s => !currentSerials.Contains(s));
            }

            var config = _configService.GetConfiguration();

            foreach (var device in connectedDevices)
            {
                if (!device.IsAuthorized)
                {
                    continue;
                }

                var serial = device.SerialNumber;
                bool alreadyNotified;
                lock (_notifiedDevices)
                {
                    alreadyNotified = _notifiedDevices.Contains(serial);
                }

                if (!config.TrustedDevices.Contains(serial) && !alreadyNotified)
                {
                    lock (_notifiedDevices)
                    {
                        _notifiedDevices.Add(serial);
                    }

                    _logger.LogInformation("Detected untrusted device: {DisplayName}. Prompting user.", device.DisplayName);
                    ShowTrustPrompt(device);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing USB device changes.");
        }
    }

    private void ShowTrustPrompt(AndroidDevice device)
    {
        if (SynchronizationContext.Current != null)
        {
            SynchronizationContext.Current.Post(_ => ShowTrustPromptInternal(device), null);
        }
        else
        {
            ShowTrustPromptInternal(device);
        }
    }

    private void ShowTrustPromptInternal(AndroidDevice device)
    {
        using var form = new TrustDeviceForm(device);
        var result = form.ShowDialog();
        if (result == DialogResult.Yes)
        {
            _logger.LogInformation("User trusted device {Serial}", device.SerialNumber);
            _ = AddDeviceToTrustedAsync(device.SerialNumber);
        }
        else
        {
            _logger.LogInformation("User declined to trust device {Serial}", device.SerialNumber);
        }
    }

    private async Task AddDeviceToTrustedAsync(string serial)
    {
        try
        {
            var config = _configService.GetConfiguration();
            if (!config.TrustedDevices.Contains(serial))
            {
                config.TrustedDevices.Add(serial);
                await _configService.UpdateConfigurationAsync(config);
                _logger.LogInformation("Device {Serial} added to trusted devices configuration.", serial);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save trusted device {Serial}.", serial);
        }
    }

    private void ExitApplication()
    {
        _logger.LogInformation("Exiting application from Tray Context.");
        
        // Stop the USB detector and unsubscribe
        _usbDetector.DeviceChanged -= OnUsbDeviceChanged;
        _usbDetector.Stop();

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _customIcon.Dispose();

        _statusForm?.Close();
        _settingsForm?.Close();
        _aboutForm?.Close();

        ExitThread();
    }

    private void OnApplicationStopping()
    {
        _logger.LogInformation("Host is stopping, exiting tray thread.");
        
        if (SynchronizationContext.Current != null)
        {
            SynchronizationContext.Current.Post(_ => ExitApplication(), null);
        }
        else
        {
            ExitApplication();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _usbDetector.DeviceChanged -= OnUsbDeviceChanged;
            _usbDetector.Stop();
            _notifyIcon.Dispose();
            _customIcon.Dispose();
        }
        base.Dispose(disposing);
    }

    // Custom toolstrip menu renderer to support custom dark themes cleanly
    private class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new DarkColorTable()) { }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected)
            {
                using var brush = new SolidBrush(Color.FromArgb(50, 50, 50));
                e.Graphics.FillRectangle(brush, e.Item.ContentRectangle);
            }
            else
            {
                base.OnRenderMenuItemBackground(e);
            }
        }
    }

    private class DarkColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Color.FromArgb(30, 30, 30);
        public override Color MenuBorder => Color.FromArgb(45, 45, 45);
        public override Color MenuItemBorder => Color.Transparent;
        public override Color SeparatorDark => Color.FromArgb(45, 45, 45);
        public override Color SeparatorLight => Color.FromArgb(45, 45, 45);
    }
}
