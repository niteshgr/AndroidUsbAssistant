using System.Windows.Forms;
using System.Drawing;
using AndroidUsbAssistant.Core.Interfaces;
using AndroidUsbAssistant.Core.Models;

namespace AndroidUsbAssistant.App.Forms;

public class StatusForm : Form
{
    private readonly IConfigurationService _configService;
    private readonly IAdbService _adbService;
    private readonly IUsbDetector _usbDetector;

    private Label? _lblUsbDetectionValue;
    private Label? _lblAdbValue;
    private Label? _lblTetherValue;
    private Label? _lblAdbPathValue;
    private ListBox? _lstDevices;
    private Button? _btnRefresh;

    public StatusForm(
        IConfigurationService configService,
        IAdbService adbService,
        IUsbDetector usbDetector)
    {
        _configService = configService;
        _adbService = adbService;
        _usbDetector = usbDetector;

        InitializeComponent();

        // Subscribe to USB connection changes to refresh automatically
        _usbDetector.DeviceChanged += OnUsbDeviceChanged;
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await RefreshStatusAsync();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _usbDetector.DeviceChanged -= OnUsbDeviceChanged;
        base.OnFormClosing(e);
    }

    private void InitializeComponent()
    {
        Text = "Service Status - Android USB Assistant";
        Size = new Size(500, 500);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(25, 25, 25);
        ForeColor = Color.FromArgb(230, 230, 230);
        Font = new Font("Segoe UI", 9.5f);

        // Title Label
        var lblTitle = new Label
        {
            Text = "System Status Dashboard",
            Font = new Font("Segoe UI Semibold", 14f, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 150, 136),
            Location = new Point(20, 20),
            Size = new Size(460, 30)
        };

        // Status Panel (Monitoring info)
        var pnlStatus = new Panel
        {
            BackColor = Color.FromArgb(33, 33, 33),
            Location = new Point(20, 60),
            Size = new Size(440, 160),
            BorderStyle = BorderStyle.None
        };

        var lblUsbDetectionTitle = new Label
        {
            Text = "USB Connection Monitor:",
            Font = new Font("Segoe UI Semibold", 9.5f),
            Location = new Point(15, 15),
            Size = new Size(180, 20)
        };

        _lblUsbDetectionValue = new Label
        {
            Text = "Active",
            ForeColor = Color.FromArgb(0, 150, 136), // Teal
            Location = new Point(250, 15),
            Size = new Size(175, 20),
            TextAlign = ContentAlignment.MiddleRight
        };

        var lblAdbTitle = new Label
        {
            Text = "ADB Daemon Status:",
            Font = new Font("Segoe UI Semibold", 9.5f),
            Location = new Point(15, 45),
            Size = new Size(180, 20)
        };

        _lblAdbValue = new Label
        {
            Text = "Checking...",
            ForeColor = Color.FromArgb(255, 193, 7), // Yellow
            Location = new Point(250, 45),
            Size = new Size(175, 20),
            TextAlign = ContentAlignment.MiddleRight
        };

        var lblTetherTitle = new Label
        {
            Text = "USB Tethering State:",
            Font = new Font("Segoe UI Semibold", 9.5f),
            Location = new Point(15, 75),
            Size = new Size(180, 20)
        };

        _lblTetherValue = new Label
        {
            Text = "Checking...",
            ForeColor = Color.FromArgb(150, 150, 150),
            Location = new Point(250, 75),
            Size = new Size(175, 20),
            TextAlign = ContentAlignment.MiddleRight
        };

        var lblAdbPathTitle = new Label
        {
            Text = "Configured ADB Path:",
            Font = new Font("Segoe UI Semibold", 9.5f),
            Location = new Point(15, 105),
            Size = new Size(150, 20)
        };

        _lblAdbPathValue = new Label
        {
            Text = "Checking...",
            ForeColor = Color.FromArgb(150, 150, 150),
            Location = new Point(15, 128),
            Size = new Size(410, 20),
            AutoEllipsis = true
        };

        pnlStatus.Controls.Add(lblUsbDetectionTitle);
        pnlStatus.Controls.Add(_lblUsbDetectionValue);
        pnlStatus.Controls.Add(lblAdbTitle);
        pnlStatus.Controls.Add(_lblAdbValue);
        pnlStatus.Controls.Add(lblTetherTitle);
        pnlStatus.Controls.Add(_lblTetherValue);
        pnlStatus.Controls.Add(lblAdbPathTitle);
        pnlStatus.Controls.Add(_lblAdbPathValue);

        // Connected Devices Section
        var lblDevicesTitle = new Label
        {
            Text = "Connected Android Devices",
            Font = new Font("Segoe UI Semibold", 9.5f),
            Location = new Point(20, 235),
            Size = new Size(200, 20)
        };

        _lstDevices = new ListBox
        {
            Location = new Point(20, 260),
            Size = new Size(440, 120),
            BackColor = Color.FromArgb(37, 37, 38),
            ForeColor = Color.FromArgb(240, 240, 240),
            BorderStyle = BorderStyle.FixedSingle
        };

        // Separation Line
        var line = new Panel
        {
            BackColor = Color.FromArgb(45, 45, 45),
            Location = new Point(20, 400),
            Size = new Size(440, 1)
        };

        // Refresh Button
        _btnRefresh = new Button
        {
            Text = "Refresh",
            Size = new Size(100, 32),
            Location = new Point(250, 413),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0, 150, 136),
            ForeColor = Color.White
        };
        _btnRefresh.FlatAppearance.BorderSize = 0;
        _btnRefresh.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 120, 109);
        _btnRefresh.Click += async (s, e) => await RefreshStatusAsync();

        // Close Button
        var btnClose = new Button
        {
            Text = "Close",
            Size = new Size(100, 32),
            Location = new Point(360, 413),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(45, 45, 48),
            ForeColor = Color.White
        };
        btnClose.FlatAppearance.BorderSize = 0;
        btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(63, 63, 65);
        btnClose.Click += (s, e) => Close();

        Controls.Add(lblTitle);
        Controls.Add(pnlStatus);
        Controls.Add(lblDevicesTitle);
        Controls.Add(_lstDevices);
        Controls.Add(line);
        Controls.Add(_btnRefresh);
        Controls.Add(btnClose);
    }

    private void OnUsbDeviceChanged(object? sender, EventArgs e)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(async () => await RefreshStatusAsync()));
        }
        else
        {
            _ = RefreshStatusAsync();
        }
    }

    private async Task RefreshStatusAsync()
    {
        if (_btnRefresh != null) _btnRefresh.Enabled = false;

        try
        {
            var config = _configService.GetConfiguration();

            if (_lblAdbPathValue != null)
            {
                _lblAdbPathValue.Text = string.IsNullOrWhiteSpace(config.AdbPath) ? "Default (System PATH)" : config.AdbPath;
                _lblAdbPathValue.ForeColor = string.IsNullOrWhiteSpace(config.AdbPath) ? Color.FromArgb(150, 150, 150) : Color.FromArgb(230, 230, 230);
            }

            var adbAvailable = await _adbService.IsAdbAvailableAsync();
            if (_lblAdbValue != null)
            {
                if (adbAvailable)
                {
                    _lblAdbValue.Text = "Available";
                    _lblAdbValue.ForeColor = Color.FromArgb(0, 150, 136); // Teal
                }
                else
                {
                    _lblAdbValue.Text = "Not Found";
                    _lblAdbValue.ForeColor = Color.FromArgb(220, 53, 69); // Red
                }
            }

            // Check USB tethering state
            if (_lblTetherValue != null)
            {
                if (!adbAvailable)
                {
                    _lblTetherValue.Text = "Inactive (ADB Offline)";
                    _lblTetherValue.ForeColor = Color.FromArgb(150, 150, 150);
                }
                else
                {
                    var devices = await _adbService.GetConnectedDevicesAsync();
                    var activeTetherDevice = "";
                    foreach (var device in devices)
                    {
                        if (device.IsAuthorized && await _adbService.IsUsbTetheringActiveAsync(device.SerialNumber))
                        {
                            activeTetherDevice = string.IsNullOrWhiteSpace(device.Model) ? device.SerialNumber : device.Model;
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(activeTetherDevice))
                    {
                        _lblTetherValue.Text = $"Active ({activeTetherDevice})";
                        _lblTetherValue.ForeColor = Color.FromArgb(0, 150, 136); // Teal
                    }
                    else
                    {
                        _lblTetherValue.Text = "Inactive";
                        _lblTetherValue.ForeColor = Color.FromArgb(150, 150, 150);
                    }
                }
            }

            if (_lstDevices != null)
            {
                _lstDevices.Items.Clear();

                if (!adbAvailable)
                {
                    _lstDevices.Items.Add("(Cannot list devices: ADB not found)");
                    _lstDevices.Enabled = false;
                }
                else
                {
                    var devices = await _adbService.GetConnectedDevicesAsync();
                    if (devices.Count == 0)
                    {
                        _lstDevices.Items.Add("(No Android devices detected)");
                        _lstDevices.Enabled = false;
                    }
                    else
                    {
                        _lstDevices.Enabled = true;
                        foreach (var device in devices)
                        {
                            _lstDevices.Items.Add(device.DisplayName);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (_lstDevices != null)
            {
                _lstDevices.Items.Clear();
                _lstDevices.Items.Add($"Error: {ex.Message}");
                _lstDevices.Enabled = false;
            }
        }
        finally
        {
            if (_btnRefresh != null) _btnRefresh.Enabled = true;
        }
    }
}
