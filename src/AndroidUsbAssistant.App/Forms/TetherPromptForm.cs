using System.Windows.Forms;
using System.Drawing;

namespace AndroidUsbAssistant.App.Forms;

public class TetherPromptForm : Form
{
    private readonly string _serial;
    private CheckBox? _chkRemember;

    public bool RememberChoice => _chkRemember?.Checked ?? false;

    public TetherPromptForm(string serial)
    {
        _serial = serial;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "Enable USB Tethering - Android USB Assistant";
        Size = new Size(440, 210);
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
            Text = "Enable USB Tethering?",
            Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 150, 136),
            Location = new Point(20, 15),
            Size = new Size(400, 25)
        };

        // Subtitle/Message
        var lblSubtitle = new Label
        {
            Text = $"A trusted Android device ({_serial}) has been connected. Would you like to enable USB Tethering now?",
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

        // Yes Button
        var btnYes = new Button
        {
            Text = "Yes",
            Size = new Size(90, 32),
            Location = new Point(210, 125),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0, 150, 136),
            ForeColor = Color.White
        };
        btnYes.FlatAppearance.BorderSize = 0;
        btnYes.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 120, 109);
        btnYes.Click += (s, e) => {
            DialogResult = DialogResult.Yes;
            Close();
        };

        // No Button
        var btnNo = new Button
        {
            Text = "No",
            Size = new Size(90, 32),
            Location = new Point(310, 125),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(45, 45, 48),
            ForeColor = Color.White
        };
        btnNo.FlatAppearance.BorderSize = 0;
        btnNo.FlatAppearance.MouseOverBackColor = Color.FromArgb(63, 63, 65);
        btnNo.Click += (s, e) => {
            DialogResult = DialogResult.No;
            Close();
        };

        Controls.Add(lblTitle);
        Controls.Add(lblSubtitle);
        Controls.Add(_chkRemember);
        Controls.Add(btnYes);
        Controls.Add(btnNo);
    }
}
