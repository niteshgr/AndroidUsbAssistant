using System.Windows.Forms;
using System.Drawing;
using AndroidUsbAssistant.Core.Interfaces;

namespace AndroidUsbAssistant.App.Forms;

public class StatusForm : Form
{
    private readonly IConfigurationService _configService;

    public StatusForm(IConfigurationService configService)
    {
        _configService = configService;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "Service Status - Android USB Assistant";
        Size = new Size(480, 360);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(25, 25, 25);
        ForeColor = Color.FromArgb(230, 230, 230);
        Font = new Font("Segoe UI", 9.5f);

        var config = _configService.GetConfiguration();

        // Title Label
        var lblTitle = new Label
        {
            Text = "System Status Dashboard",
            Font = new Font("Segoe UI Semibold", 14f, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 150, 136),
            Location = new Point(20, 20),
            Size = new Size(440, 30)
        };

        // Status Group Box (Custom Flat Panel)
        var pnlStatus = new Panel
        {
            BackColor = Color.FromArgb(33, 33, 33),
            Location = new Point(20, 60),
            Size = new Size(424, 200),
            BorderStyle = BorderStyle.None
        };

        // Inner controls for panel
        var lblUsbDetectionTitle = new Label
        {
            Text = "USB Connection Monitor:",
            Font = new Font("Segoe UI Semibold", 9.5f),
            Location = new Point(15, 20),
            Size = new Size(200, 20)
        };

        var lblUsbDetectionValue = new Label
        {
            Text = "Offline (Milestone 2)",
            ForeColor = Color.FromArgb(220, 53, 69), // Red
            Location = new Point(240, 20),
            Size = new Size(170, 20),
            TextAlign = ContentAlignment.MiddleRight
        };

        var lblAdbTitle = new Label
        {
            Text = "ADB Daemon Status:",
            Font = new Font("Segoe UI Semibold", 9.5f),
            Location = new Point(15, 60),
            Size = new Size(200, 20)
        };

        var lblAdbValue = new Label
        {
            Text = "Disconnected (Milestone 3)",
            ForeColor = Color.FromArgb(255, 193, 7), // Yellow
            Location = new Point(240, 60),
            Size = new Size(170, 20),
            TextAlign = ContentAlignment.MiddleRight
        };

        var lblTetherTitle = new Label
        {
            Text = "USB Tethering Action:",
            Font = new Font("Segoe UI Semibold", 9.5f),
            Location = new Point(15, 100),
            Size = new Size(200, 20)
        };

        var lblTetherValue = new Label
        {
            Text = "Inactive (Milestone 6)",
            ForeColor = Color.FromArgb(108, 117, 125), // Gray
            Location = new Point(240, 100),
            Size = new Size(170, 20),
            TextAlign = ContentAlignment.MiddleRight
        };

        var lblAdbPathTitle = new Label
        {
            Text = "Configured ADB Path:",
            Font = new Font("Segoe UI Semibold", 9.5f),
            Location = new Point(15, 140),
            Size = new Size(150, 20)
        };

        var lblAdbPathValue = new Label
        {
            Text = string.IsNullOrWhiteSpace(config.AdbPath) ? "Not configured" : config.AdbPath,
            ForeColor = string.IsNullOrWhiteSpace(config.AdbPath) ? Color.FromArgb(150, 150, 150) : Color.FromArgb(230, 230, 230),
            Location = new Point(15, 165),
            Size = new Size(394, 20),
            AutoEllipsis = true
        };

        pnlStatus.Controls.Add(lblUsbDetectionTitle);
        pnlStatus.Controls.Add(lblUsbDetectionValue);
        pnlStatus.Controls.Add(lblAdbTitle);
        pnlStatus.Controls.Add(lblAdbValue);
        pnlStatus.Controls.Add(lblTetherTitle);
        pnlStatus.Controls.Add(lblTetherValue);
        pnlStatus.Controls.Add(lblAdbPathTitle);
        pnlStatus.Controls.Add(lblAdbPathValue);

        // Close Button
        var btnClose = new Button
        {
            Text = "Close",
            Size = new Size(100, 32),
            Location = new Point(344, 280),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(45, 45, 48),
            ForeColor = Color.White
        };
        btnClose.FlatAppearance.BorderSize = 0;
        btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(63, 63, 65);
        btnClose.Click += (s, e) => Close();

        Controls.Add(lblTitle);
        Controls.Add(pnlStatus);
        Controls.Add(btnClose);
    }
}
