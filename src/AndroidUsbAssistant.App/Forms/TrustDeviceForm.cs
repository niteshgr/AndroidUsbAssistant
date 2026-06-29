using System.Windows.Forms;
using System.Drawing;
using AndroidUsbAssistant.Core.Models;

namespace AndroidUsbAssistant.App.Forms;

public class TrustDeviceForm : Form
{
    private readonly AndroidDevice _device;

    public TrustDeviceForm(AndroidDevice device)
    {
        _device = device;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "New Device Detected";
        Size = new Size(450, 300);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(25, 25, 25);
        ForeColor = Color.FromArgb(230, 230, 230);
        Font = new Font("Segoe UI", 9.5f);

        // Header Title
        var lblTitle = new Label
        {
            Text = "Trust Connected Device?",
            Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 150, 136),
            Location = new Point(20, 20),
            Size = new Size(410, 30)
        };

        // Info / Subtitle
        var lblSubtitle = new Label
        {
            Text = "A new Android device has been connected over USB. Do you want to trust this device for automation actions?",
            Location = new Point(20, 55),
            Size = new Size(400, 45)
        };

        // Details Panel (Manufacturer, Model, Serial)
        var pnlDetails = new Panel
        {
            BackColor = Color.FromArgb(33, 33, 33),
            Location = new Point(20, 110),
            Size = new Size(400, 90)
        };

        var lblBrandTitle = new Label
        {
            Text = "Brand / Manufacturer:",
            Font = new Font("Segoe UI Semibold", 9f),
            Location = new Point(15, 10),
            Size = new Size(150, 20)
        };

        var lblBrandValue = new Label
        {
            Text = string.IsNullOrWhiteSpace(_device.Manufacturer) ? "Unknown" : char.ToUpper(_device.Manufacturer[0]) + _device.Manufacturer[1..],
            Location = new Point(180, 10),
            Size = new Size(205, 20),
            TextAlign = ContentAlignment.MiddleRight
        };

        var lblModelTitle = new Label
        {
            Text = "Model:",
            Font = new Font("Segoe UI Semibold", 9f),
            Location = new Point(15, 35),
            Size = new Size(150, 20)
        };

        var lblModelValue = new Label
        {
            Text = string.IsNullOrWhiteSpace(_device.Model) ? "Unknown" : _device.Model,
            Location = new Point(180, 35),
            Size = new Size(205, 20),
            TextAlign = ContentAlignment.MiddleRight
        };

        var lblSerialTitle = new Label
        {
            Text = "Serial Number:",
            Font = new Font("Segoe UI Semibold", 9f),
            Location = new Point(15, 60),
            Size = new Size(150, 20)
        };

        var lblSerialValue = new Label
        {
            Text = _device.SerialNumber,
            Font = new Font("Consolas", 9.5f),
            Location = new Point(180, 60),
            Size = new Size(205, 20),
            TextAlign = ContentAlignment.MiddleRight
        };

        pnlDetails.Controls.Add(lblBrandTitle);
        pnlDetails.Controls.Add(lblBrandValue);
        pnlDetails.Controls.Add(lblModelTitle);
        pnlDetails.Controls.Add(lblModelValue);
        pnlDetails.Controls.Add(lblSerialTitle);
        pnlDetails.Controls.Add(lblSerialValue);

        // Save Button (Trust)
        var btnTrust = new Button
        {
            Text = "Trust Device",
            Size = new Size(110, 32),
            Location = new Point(190, 215),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0, 150, 136),
            ForeColor = Color.White
        };
        btnTrust.FlatAppearance.BorderSize = 0;
        btnTrust.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 120, 109);
        btnTrust.Click += (s, e) => {
            DialogResult = DialogResult.Yes;
            Close();
        };

        // Cancel Button (Don't Trust)
        var btnCancel = new Button
        {
            Text = "Don't Trust",
            Size = new Size(110, 32),
            Location = new Point(310, 215),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(45, 45, 48),
            ForeColor = Color.White
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(63, 63, 65);
        btnCancel.Click += (s, e) => {
            DialogResult = DialogResult.No;
            Close();
        };

        Controls.Add(lblTitle);
        Controls.Add(lblSubtitle);
        Controls.Add(pnlDetails);
        Controls.Add(btnTrust);
        Controls.Add(btnCancel);
    }
}
