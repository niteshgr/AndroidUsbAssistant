using System.Windows.Forms;
using System.Drawing;
using AndroidUsbAssistant.Core.Interfaces;
using AndroidUsbAssistant.Core.Models;
using AndroidUsbAssistant.App.Services;

namespace AndroidUsbAssistant.App.Forms;

public class SettingsForm : Form
{
    private readonly IConfigurationService _configService;
    private readonly List<string> _tempTrustedDevices = new();

    private TextBox? _txtAdbPath;
    private CheckBox? _chkStartWithWindows;
    private ListBox? _lstTrustedDevices;
    private Button? _btnRemoveDevice;

    public SettingsForm(IConfigurationService configService)
    {
        _configService = configService;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "Settings - Android USB Assistant";
        Size = new Size(500, 420);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(25, 25, 25);
        ForeColor = Color.FromArgb(230, 230, 230);
        Font = new Font("Segoe UI", 9.5f);

        var config = _configService.GetConfiguration();
        _tempTrustedDevices.AddRange(config.TrustedDevices);

        // Title
        var lblTitle = new Label
        {
            Text = "Application Settings",
            Font = new Font("Segoe UI Semibold", 14f, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 150, 136),
            Location = new Point(20, 20),
            Size = new Size(460, 30)
        };

        // ADB Path label & textbox & browse button
        var lblAdbPath = new Label
        {
            Text = "ADB Executable Path",
            Font = new Font("Segoe UI Semibold", 9.5f),
            Location = new Point(20, 65),
            Size = new Size(200, 20)
        };

        _txtAdbPath = new TextBox
        {
            Text = config.AdbPath,
            Location = new Point(20, 90),
            Size = new Size(350, 25),
            BackColor = Color.FromArgb(37, 37, 38),
            ForeColor = Color.FromArgb(240, 240, 240),
            BorderStyle = BorderStyle.FixedSingle
        };

        var btnBrowse = new Button
        {
            Text = "Browse...",
            Location = new Point(380, 89),
            Size = new Size(80, 26),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(45, 45, 48),
            ForeColor = Color.White
        };
        btnBrowse.FlatAppearance.BorderSize = 0;
        btnBrowse.Click += BtnBrowse_Click;

        // Start with Windows checkbox
        _chkStartWithWindows = new CheckBox
        {
            Text = "Start application with Windows",
            Checked = config.StartWithWindows,
            Location = new Point(20, 135),
            Size = new Size(300, 24),
            FlatStyle = FlatStyle.Flat
        };

        // Trusted devices list
        var lblTrustedDevices = new Label
        {
            Text = "Trusted Serial Numbers",
            Font = new Font("Segoe UI Semibold", 9.5f),
            Location = new Point(20, 180),
            Size = new Size(200, 20)
        };

        _lstTrustedDevices = new ListBox
        {
            Location = new Point(20, 205),
            Size = new Size(340, 100),
            BackColor = Color.FromArgb(37, 37, 38),
            ForeColor = Color.FromArgb(240, 240, 240),
            BorderStyle = BorderStyle.FixedSingle
        };
        _lstTrustedDevices.SelectedIndexChanged += LstTrustedDevices_SelectedIndexChanged;

        _btnRemoveDevice = new Button
        {
            Text = "Remove",
            Location = new Point(380, 204),
            Size = new Size(80, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(220, 53, 69), // Soft Red
            ForeColor = Color.White,
            Enabled = false
        };
        _btnRemoveDevice.FlatAppearance.BorderSize = 0;
        _btnRemoveDevice.Click += BtnRemoveDevice_Click;

        // Bind data initial
        RefreshTrustedDevicesList();

        // Action Buttons Panel
        var line = new Panel
        {
            BackColor = Color.FromArgb(45, 45, 45),
            Location = new Point(20, 325),
            Size = new Size(440, 1)
        };

        var btnSave = new Button
        {
            Text = "Save",
            Location = new Point(250, 338),
            Size = new Size(100, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0, 150, 136),
            ForeColor = Color.White
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 120, 109);
        btnSave.Click += BtnSave_Click;

        var btnCancel = new Button
        {
            Text = "Cancel",
            Location = new Point(360, 338),
            Size = new Size(100, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(45, 45, 48),
            ForeColor = Color.White
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(63, 63, 65);
        btnCancel.Click += (s, e) => Close();

        Controls.Add(lblTitle);
        Controls.Add(lblAdbPath);
        Controls.Add(_txtAdbPath);
        Controls.Add(btnBrowse);
        Controls.Add(_chkStartWithWindows);
        Controls.Add(lblTrustedDevices);
        Controls.Add(_lstTrustedDevices);
        Controls.Add(_btnRemoveDevice);
        Controls.Add(line);
        Controls.Add(btnSave);
        Controls.Add(btnCancel);
    }

    private void LstTrustedDevices_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_lstTrustedDevices == null || _btnRemoveDevice == null) return;
        _btnRemoveDevice.Enabled = _lstTrustedDevices.SelectedIndex >= 0 && _lstTrustedDevices.Enabled;
    }

    private void BtnRemoveDevice_Click(object? sender, EventArgs e)
    {
        if (_lstTrustedDevices == null || _lstTrustedDevices.SelectedIndex < 0) return;

        var selectedSerial = _lstTrustedDevices.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(selectedSerial))
        {
            _tempTrustedDevices.Remove(selectedSerial);
            RefreshTrustedDevicesList();
        }
    }

    private void RefreshTrustedDevicesList()
    {
        if (_lstTrustedDevices == null || _btnRemoveDevice == null) return;

        _lstTrustedDevices.Items.Clear();
        if (_tempTrustedDevices.Count == 0)
        {
            _lstTrustedDevices.Items.Add("(No trusted devices remembered yet)");
            _lstTrustedDevices.Enabled = false;
            _btnRemoveDevice.Enabled = false;
        }
        else
        {
            _lstTrustedDevices.Enabled = true;
            foreach (var serial in _tempTrustedDevices)
            {
                _lstTrustedDevices.Items.Add(serial);
            }
            _btnRemoveDevice.Enabled = _lstTrustedDevices.SelectedIndex >= 0;
        }
    }

    private void BtnBrowse_Click(object? sender, EventArgs e)
    {
        using var openFileDialog = new OpenFileDialog
        {
            Filter = "Executables (*.exe)|*.exe|All files (*.*)|*.*",
            Title = "Select ADB Executable"
        };

        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            if (_txtAdbPath != null)
            {
                _txtAdbPath.Text = openFileDialog.FileName;
            }
        }
    }

    private async void BtnSave_Click(object? sender, EventArgs e)
    {
        var config = _configService.GetConfiguration();
        config.AdbPath = _txtAdbPath?.Text ?? string.Empty;
        config.StartWithWindows = _chkStartWithWindows?.Checked ?? true;

        // Apply device list modifications
        config.TrustedDevices.Clear();
        config.TrustedDevices.AddRange(_tempTrustedDevices);

        try
        {
            // Sync Windows Startup configuration
            StartupRegistryHelper.SetStartup(config.StartWithWindows);

            await _configService.UpdateConfigurationAsync(config);
            MessageBox.Show("Configuration saved successfully.", "Settings Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save configuration: {ex.Message}", "Error Saving Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
