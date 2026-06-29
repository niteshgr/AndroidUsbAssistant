using System.Windows.Forms;
using System.Drawing;

namespace AndroidUsbAssistant.App.Forms;

public class AboutForm : Form
{
    public AboutForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "About Android USB Assistant";
        Size = new Size(420, 260);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(25, 25, 25);
        ForeColor = Color.FromArgb(230, 230, 230);
        Font = new Font("Segoe UI", 9.5f);

        // Header Title Label
        var lblTitle = new Label
        {
            Text = "Android USB Assistant",
            Font = new Font("Segoe UI Semibold", 16f, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 150, 136), // Modern Teal
            Location = new Point(20, 20),
            Size = new Size(380, 35),
            TextAlign = ContentAlignment.MiddleLeft
        };

        // Version Label
        var lblVersion = new Label
        {
            Text = "Version 1.0.0 (Milestone 1 - Foundation)",
            Font = new Font("Segoe UI", 9f, FontStyle.Italic),
            ForeColor = Color.FromArgb(150, 150, 150),
            Location = new Point(20, 55),
            Size = new Size(380, 20)
        };

        // Description Label
        var lblDescription = new Label
        {
            Text = "A lightweight Windows system tray utility designed to monitor connected Android devices over USB and execute configured automation, such as enabling USB tethering via ADB.\n\nBuilt using .NET 10 & Clean Architecture.",
            Location = new Point(20, 90),
            Size = new Size(365, 75),
            FlatStyle = FlatStyle.Flat
        };

        // Close Button
        var btnClose = new Button
        {
            Text = "Close",
            Size = new Size(100, 32),
            Location = new Point(285, 175),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(45, 45, 48),
            ForeColor = Color.White
        };
        btnClose.FlatAppearance.BorderSize = 0;
        btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(63, 63, 65);
        btnClose.Click += (s, e) => Close();

        // Footer border line
        var line = new Panel
        {
            BackColor = Color.FromArgb(45, 45, 45),
            Location = new Point(20, 168),
            Size = new Size(365, 1)
        };

        Controls.Add(lblTitle);
        Controls.Add(lblVersion);
        Controls.Add(lblDescription);
        Controls.Add(line);
        Controls.Add(btnClose);
    }
}
