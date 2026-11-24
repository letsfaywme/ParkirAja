#nullable enable annotations
namespace ParkingSystem;

partial class Login
{
    private System.ComponentModel.IContainer? components = null;
    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }
    #region Windows Form Designer generated code
    private void InitializeComponent()
    {
        label1 = new Label();
        spacer1 = new Panel();
        label3 = new Label();
        usernamePanel = new Panel();
        usernameTextBox = new TextBox();
        spacer2 = new Panel();
        label7 = new Label();
        passwordPanel = new Panel();
        passwordTextBox = new TextBox();
        spacer3 = new Panel();
        signInButton = new Button();
        spacer4 = new Panel();
        demoHintLabel = new Label();
        loginFields = new Panel();
        loginLogo = new PictureBox();
        usernamePanel.SuspendLayout();
        passwordPanel.SuspendLayout();
        loginFields.SuspendLayout();
        SuspendLayout();
        // 
        // loginLogo
        // 
        loginLogo.BackColor = Color.Transparent;
        loginLogo.Location = new Point(240, 30);
        loginLogo.Margin = new Padding(0);
        loginLogo.Name = "loginLogo";
        loginLogo.Size = new Size(120, 120);
        loginLogo.SizeMode = PictureBoxSizeMode.Zoom;
        loginLogo.TabIndex = 13;
        loginLogo.TabStop = false;
        loginLogo.Image = AppBrand.Logo;
        // 
        // label1
        // 
        label1.BackColor = Color.Transparent;
        label1.Dock = DockStyle.Top;
        label1.Font = new Font("Segoe UI", 15.99F, FontStyle.Bold);
        label1.ForeColor = Color.FromArgb(15, 23, 42);
        label1.Location = new Point(75, 100);
        label1.Margin = new Padding(0);
        label1.Name = "label1";
        label1.Size = new Size(450, 70);
        label1.TabIndex = 0;
        label1.Text = "ParkirAja — Sign In";
        label1.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // spacer1
        // 
        spacer1.BackColor = Color.Transparent;
        spacer1.Dock = DockStyle.Top;
        spacer1.Location = new Point(75, 170);
        spacer1.Margin = new Padding(0);
        spacer1.Name = "spacer1";
        spacer1.Size = new Size(450, 35);
        spacer1.TabIndex = 1;
        // 
        // label3
        // 
        label3.BackColor = Color.Transparent;
        label3.Dock = DockStyle.Top;
        label3.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        label3.ForeColor = Color.FromArgb(100, 116, 139);
        label3.Location = new Point(75, 205);
        label3.Margin = new Padding(0);
        label3.Name = "label3";
        label3.Size = new Size(450, 22);
        label3.TabIndex = 2;
        label3.Text = "Username";
        // 
        // usernamePanel
        // 
        usernamePanel.BackColor = Color.FromArgb(255, 255, 255);
        usernamePanel.BorderStyle = BorderStyle.FixedSingle;
        usernamePanel.Controls.Add(usernameTextBox);
        usernamePanel.Dock = DockStyle.Top;
        usernamePanel.ForeColor = Color.FromArgb(15, 23, 42);
        usernamePanel.Location = new Point(75, 227);
        usernamePanel.Margin = new Padding(0);
        usernamePanel.Name = "usernamePanel";
        usernamePanel.Padding = new Padding(15, 11, 15, 11);
        usernamePanel.Size = new Size(450, 50);
        usernamePanel.TabIndex = 3;
        // 
        // usernameTextBox
        // 
        usernameTextBox.AccessibleName = "Username";
        usernameTextBox.BackColor = Color.FromArgb(255, 255, 255);
        usernameTextBox.BorderStyle = BorderStyle.None;
        usernameTextBox.Dock = DockStyle.Fill;
        usernameTextBox.ForeColor = Color.FromArgb(15, 23, 42);
        usernameTextBox.Location = new Point(15, 11);
        usernameTextBox.Margin = new Padding(0);
        usernameTextBox.Name = "usernameTextBox";
        usernameTextBox.PlaceholderText = "Enter username";
        usernameTextBox.Size = new Size(418, 20);
        usernameTextBox.TabIndex = 4;
        // 
        // spacer2
        // 
        spacer2.BackColor = Color.Transparent;
        spacer2.Dock = DockStyle.Top;
        spacer2.Location = new Point(75, 277);
        spacer2.Margin = new Padding(0);
        spacer2.Name = "spacer2";
        spacer2.Size = new Size(450, 30);
        spacer2.TabIndex = 5;
        // 
        // label7
        // 
        label7.BackColor = Color.Transparent;
        label7.Dock = DockStyle.Top;
        label7.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        label7.ForeColor = Color.FromArgb(100, 116, 139);
        label7.Location = new Point(75, 307);
        label7.Margin = new Padding(0);
        label7.Name = "label7";
        label7.Size = new Size(450, 22);
        label7.TabIndex = 6;
        label7.Text = "Password";
        // 
        // passwordPanel
        // 
        passwordPanel.BackColor = Color.FromArgb(255, 255, 255);
        passwordPanel.BorderStyle = BorderStyle.FixedSingle;
        passwordPanel.Controls.Add(passwordTextBox);
        passwordPanel.Dock = DockStyle.Top;
        passwordPanel.ForeColor = Color.FromArgb(15, 23, 42);
        passwordPanel.Location = new Point(75, 329);
        passwordPanel.Margin = new Padding(0);
        passwordPanel.Name = "passwordPanel";
        passwordPanel.Padding = new Padding(15, 11, 15, 11);
        passwordPanel.Size = new Size(450, 50);
        passwordPanel.TabIndex = 7;
        // 
        // passwordTextBox
        // 
        passwordTextBox.AccessibleName = "Password";
        passwordTextBox.BackColor = Color.FromArgb(255, 255, 255);
        passwordTextBox.BorderStyle = BorderStyle.None;
        passwordTextBox.Dock = DockStyle.Fill;
        passwordTextBox.ForeColor = Color.FromArgb(15, 23, 42);
        passwordTextBox.Location = new Point(15, 11);
        passwordTextBox.Margin = new Padding(0);
        passwordTextBox.Name = "passwordTextBox";
        passwordTextBox.PlaceholderText = "Enter password";
        passwordTextBox.Size = new Size(418, 20);
        passwordTextBox.TabIndex = 8;
        passwordTextBox.UseSystemPasswordChar = true;
        // 
        // spacer3
        // 
        spacer3.BackColor = Color.Transparent;
        spacer3.Dock = DockStyle.Top;
        spacer3.Location = new Point(75, 379);
        spacer3.Margin = new Padding(0);
        spacer3.Name = "spacer3";
        spacer3.Size = new Size(450, 40);
        spacer3.TabIndex = 9;
        // 
        // signInButton
        // 
        signInButton.AccessibleName = "Sign In";
        signInButton.BackColor = Color.FromArgb(5, 150, 105);
        signInButton.Cursor = Cursors.Hand;
        signInButton.Dock = DockStyle.Top;
        signInButton.FlatAppearance.BorderSize = 0;
        signInButton.FlatStyle = FlatStyle.Flat;
        signInButton.ForeColor = Color.White;
        signInButton.Location = new Point(75, 419);
        signInButton.Margin = new Padding(0);
        signInButton.Name = "signInButton";
        signInButton.Size = new Size(450, 60);
        signInButton.TabIndex = 10;
        signInButton.Text = "Sign In";
        signInButton.UseVisualStyleBackColor = false;
        signInButton.Click += OpenWorkspace;
        // 
        // spacer4
        // 
        spacer4.BackColor = Color.Transparent;
        spacer4.Dock = DockStyle.Top;
        spacer4.Location = new Point(75, 479);
        spacer4.Margin = new Padding(0);
        spacer4.Name = "spacer4";
        spacer4.Size = new Size(450, 20);
        spacer4.TabIndex = 11;
        // 
        // demoHintLabel
        // 
        demoHintLabel.BackColor = Color.Transparent;
        demoHintLabel.Dock = DockStyle.Top;
        demoHintLabel.Font = new Font("Segoe UI", 8F);
        demoHintLabel.ForeColor = Color.FromArgb(100, 116, 139);
        demoHintLabel.Location = new Point(75, 499);
        demoHintLabel.Margin = new Padding(0);
        demoHintLabel.Name = "demoHintLabel";
        demoHintLabel.Size = new Size(450, 25);
        demoHintLabel.TabIndex = 12;
        demoHintLabel.Text = "Demo: kosongkan username & password lalu klik Sign In";
        demoHintLabel.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // loginFields
        // 
        loginFields.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        loginFields.Controls.Add(demoHintLabel);
        loginFields.Controls.Add(spacer4);
        loginFields.Controls.Add(signInButton);
        loginFields.Controls.Add(spacer3);
        loginFields.Controls.Add(passwordPanel);
        loginFields.Controls.Add(label7);
        loginFields.Controls.Add(spacer2);
        loginFields.Controls.Add(usernamePanel);
        loginFields.Controls.Add(label3);
        loginFields.Controls.Add(spacer1);
        loginFields.Controls.Add(label1);
        loginFields.Controls.Add(loginLogo);
        loginFields.Location = new Point(0, 0);
        loginFields.Margin = new Padding(0);
        loginFields.Name = "loginFields";
        loginFields.Padding = new Padding(75, 100, 75, 0);
        loginFields.Size = new Size(600, 800);
        loginFields.TabIndex = 0;
        // 
        // Login
        // 
        AcceptButton = signInButton;
        AutoScaleDimensions = new SizeF(120F, 120F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(248, 250, 252);
        ClientSize = new Size(600, 800);
        Controls.Add(loginFields);
        Font = new Font("Segoe UI", 9F);
        Margin = new Padding(4);
        MaximizeBox = false;
        MinimumSize = new Size(520, 688);
        Name = "Login";
        Icon = AppBrand.Icon;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "ParkirAja — Sign In";
        usernamePanel.ResumeLayout(false);
        usernamePanel.PerformLayout();
        passwordPanel.ResumeLayout(false);
        passwordPanel.PerformLayout();
        loginFields.ResumeLayout(false);
        ResumeLayout(false);
    }
    #endregion
    private PictureBox loginLogo = null!;
    private Label label1 = null!;
    private Panel spacer1 = null!;
    private Label label3 = null!;
    private Panel usernamePanel = null!;
    private TextBox usernameTextBox = null!;
    private Panel spacer2 = null!;
    private Label label7 = null!;
    private Panel passwordPanel = null!;
    private TextBox passwordTextBox = null!;
    private Panel spacer3 = null!;
    private Button signInButton = null!;
    private Panel spacer4 = null!;
    private Label demoHintLabel = null!;
    private Panel loginFields = null!;
}
