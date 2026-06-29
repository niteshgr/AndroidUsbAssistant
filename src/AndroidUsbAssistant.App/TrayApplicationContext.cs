using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using AndroidUsbAssistant.App.Forms;
using AndroidUsbAssistant.Core.Interfaces;

namespace AndroidUsbAssistant.App;

public class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ILogger<TrayApplicationContext> _logger;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IServiceProvider _serviceProvider;
    private readonly IUsbDetector _usbDetector;
    private readonly Icon _customIcon;

    private StatusForm? _statusForm;
    private SettingsForm? _settingsForm;
    private AboutForm? _aboutForm;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);

    public TrayApplicationContext(
        ILogger<TrayApplicationContext> logger,
        IHostApplicationLifetime lifetime,
        IServiceProvider serviceProvider,
        IUsbDetector usbDetector)
    {
        _logger = logger;
        _lifetime = lifetime;
        _serviceProvider = serviceProvider;
        _usbDetector = usbDetector;

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

    private void OnUsbDeviceChanged(object? sender, EventArgs e)
    {
        _logger.LogInformation("USB device change event triggered in Tray.");
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
