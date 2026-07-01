using System.Windows.Forms;
using System.Drawing;
using AndroidUsbAssistant.Core.Models;

namespace AndroidUsbAssistant.App.Forms;

public class TetherPromptForm : Form
{
    private readonly string _serial;
    private CheckBox? _chkRemember;

    public bool RememberChoice => _chkRemember?.Checked ?? false;
    public UsbConnectionMode SelectedMode { get; private set; } = UsbConnectionMode.ChargeOnly;

    public TetherPromptForm(string serial)
    {
        _serial = serial;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "USB Connection Mode - Android USB Assistant";
        Size = new Size(440, 220);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(25, 25, 25);
        ForeColor = Color.FromArgb(230, 230, 230);
        Font = new Font("Segoe UI", 9.5f);

        // Title
        var lblTitle = new Label
        {
            Text = "Select USB Connection Mode",
            Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 150, 136),
            Location = new Point(20, 15),
            Size = new Size(400, 25)
        };

        // Subtitle/Message
        var lblSubtitle = new Label
        {
            Text = $"A trusted Android device ({_serial}) has been connected. Please choose how you want to configure this connection:",
            Location = new Point(20, 45),
            Size = new Size(385, 45)
        };

        // Checkbox "Don't ask again"
        _chkRemember = new CheckBox
        {
            Text = "Remember my choice (don't ask again)",
            Location = new Point(20, 95),
            Size = new Size(300, 24),
            FlatStyle = FlatStyle.Flat
        };

        // USB Tether Button
        var btnTether = new Button
        {
            Text = "USB Tethering",
            Size = new Size(115, 32),
            Location = new Point(20, 130),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0, 150, 136),
            ForeColor = Color.White
        };
        btnTether.FlatAppearance.BorderSize = 0;
        btnTether.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 120, 109);
        btnTether.Click += (s, e) => {
            SelectedMode = UsbConnectionMode.Tether;
            DialogResult = DialogResult.Yes;
            Close();
        };

        // Transfer Files Button
        var btnTransfer = new Button
        {
            Text = "Transfer Files",
            Size = new Size(115, 32),
            Location = new Point(145, 130),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(45, 45, 48),
            ForeColor = Color.White
        };
        btnTransfer.FlatAppearance.BorderSize = 0;
        btnTransfer.FlatAppearance.MouseOverBackColor = Color.FromArgb(63, 63, 65);
        btnTransfer.Click += (s, e) => {
            SelectedMode = UsbConnectionMode.TransferFiles;
            DialogResult = DialogResult.OK;
            Close();
        };

        // Charge Only Button
        var btnCharge = new Button
        {
            Text = "Charge Only",
            Size = new Size(115, 32),
            Location = new Point(270, 130),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(45, 45, 48),
            ForeColor = Color.White
        };
        btnCharge.FlatAppearance.BorderSize = 0;
        btnCharge.FlatAppearance.MouseOverBackColor = Color.FromArgb(63, 63, 65);
        btnCharge.Click += (s, e) => {
            SelectedMode = UsbConnectionMode.ChargeOnly;
            DialogResult = DialogResult.Cancel;
            Close();
        };

        Controls.Add(lblTitle);
        Controls.Add(lblSubtitle);
        Controls.Add(_chkRemember);
        Controls.Add(btnTether);
        Controls.Add(btnTransfer);
        Controls.Add(btnCharge);
    }
}
