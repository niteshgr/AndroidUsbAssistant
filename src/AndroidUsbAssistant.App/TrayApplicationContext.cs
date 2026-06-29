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
    private readonly IActionEngine _actionEngine;
    private readonly Icon _customIcon;
    private readonly HashSet<string> _notifiedDevices = new();
    private readonly HashSet<string> _executedDevices = new();
    private IntPtr _hIcon = IntPtr.Zero;

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
        IAdbService adbService,
        IActionEngine actionEngine)
    {
        _logger = logger;
        _lifetime = lifetime;
        _serviceProvider = serviceProvider;
        _usbDetector = usbDetector;
        _configService = configService;
        _adbService = adbService;
        _actionEngine = actionEngine;

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
        
        // Use high-quality rendering for smooth arcs and lines
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        // Draw Phone Outline (White/Light Gray)
        using var phonePen = new Pen(Color.FromArgb(240, 240, 240), 1.5f);
        graphics.DrawRectangle(phonePen, 2, 3, 5, 10);
        
        // Speaker line
        using var detailPen = new Pen(Color.FromArgb(180, 180, 180), 1f);
        graphics.DrawLine(detailPen, 4, 4, 5, 4);
        
        // Home button dot
        using var dotBrush = new SolidBrush(Color.FromArgb(240, 240, 240));
        graphics.FillEllipse(dotBrush, 4, 11, 1.5f, 1.5f);

        // Draw Hotspot/Internet Waves (Teal Theme Color: #009688)
        using var wavePen = new Pen(Color.FromArgb(0, 150, 136), 1.5f);
        
        // Small Wave
        graphics.DrawArc(wavePen, 3, 3, 10, 10, -35, 70);
        // Large Wave
        graphics.DrawArc(wavePen, 0, 0, 16, 16, -35, 70);

        _hIcon = bitmap.GetHicon();
        return Icon.FromHandle(_hIcon);
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
            // Give the ADB daemon a short moment to register the new USB interface connection
            await Task.Delay(1500);

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
            lock (_executedDevices)
            {
                _executedDevices.RemoveWhere(s => !currentSerials.Contains(s));
            }

            var config = _configService.GetConfiguration();

            foreach (var device in connectedDevices)
            {
                if (!device.IsAuthorized)
                {
                    continue;
                }

                var serial = device.SerialNumber;

                if (config.TrustedDevices.Contains(serial))
                {
                    bool alreadyExecuted;
                    lock (_executedDevices)
                    {
                        alreadyExecuted = _executedDevices.Contains(serial);
                    }

                    if (!alreadyExecuted)
                    {
                        lock (_executedDevices)
                        {
                            _executedDevices.Add(serial);
                        }

                        _logger.LogInformation("Trusted device {DisplayName} connected. Running actions.", device.DisplayName);
                        _ = _actionEngine.ExecuteActionsForDeviceAsync(serial);
                    }
                }
                else
                {
                    bool alreadyNotified;
                    lock (_notifiedDevices)
                    {
                        alreadyNotified = _notifiedDevices.Contains(serial);
                    }

                    if (!alreadyNotified)
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

            await UpdateTrayTooltipAsync(connectedDevices);
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
            lock (_executedDevices)
            {
                _executedDevices.Add(device.SerialNumber);
            }
            _ = TrustDeviceAndRunActionsAsync(device.SerialNumber);
        }
        else
        {
            _logger.LogInformation("User declined to trust device {Serial}", device.SerialNumber);
        }
    }

    private async Task TrustDeviceAndRunActionsAsync(string serial)
    {
        try
        {
            await AddDeviceToTrustedAsync(serial);
            await _actionEngine.ExecuteActionsForDeviceAsync(serial);

            var connectedDevices = await _adbService.GetConnectedDevicesAsync();
            await UpdateTrayTooltipAsync(connectedDevices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trust and execute actions for device {Serial}.", serial);
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

    private async Task UpdateTrayTooltipAsync(List<AndroidDevice> connectedDevices)
    {
        try
        {
            var activeTetherDevice = "";
            foreach (var device in connectedDevices)
            {
                if (device.IsAuthorized && await _adbService.IsUsbTetheringActiveAsync(device.SerialNumber))
                {
                    activeTetherDevice = string.IsNullOrWhiteSpace(device.Model) ? device.SerialNumber : device.Model;
                    break;
                }
            }

            string text;
            if (!string.IsNullOrEmpty(activeTetherDevice))
            {
                text = $"Android USB Assistant\nTethering: Active ({activeTetherDevice})";
            }
            else
            {
                text = "Android USB Assistant\nTethering: Inactive";
            }

            if (text.Length > 63)
            {
                text = text.Substring(0, 60) + "...";
            }

            if (SynchronizationContext.Current != null)
            {
                SynchronizationContext.Current.Post(_ => _notifyIcon.Text = text, null);
            }
            else
            {
                _notifyIcon.Text = text;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update tray icon tooltip.");
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

        if (_hIcon != IntPtr.Zero)
        {
            DestroyIcon(_hIcon);
            _hIcon = IntPtr.Zero;
        }

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

        if (_hIcon != IntPtr.Zero)
        {
            DestroyIcon(_hIcon);
            _hIcon = IntPtr.Zero;
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
