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
        label3 = new Label();
        usernamePanel = new RoundedPanel();
        usernameTextBox = new TextBox();
        label7 = new Label();
        passwordPanel = new RoundedPanel();
        passwordTextBox = new TextBox();
        signInButton = new RoundedButton();
        SuspendLayout();
        label1.Name = "label1";
        label1.Location = new Point(339, 149);
        label1.Size = new Size(443, 25);
        label1.BackColor = Color.Transparent;
        label1.ForeColor = AppTheme.Text;
        label1.TabIndex = 1;
        label1.Text = "ParkirAja — Sign In";
        label1.ForeColor = AppTheme.Text;
        label1.Font = new Font("Segoe UI", 15.99F, FontStyle.Bold);
        label3.Name = "label3";
        label3.Location = new Point(339, 243);
        label3.Size = new Size(394, 12);
        label3.BackColor = Color.Transparent;
        label3.ForeColor = AppTheme.Text;
        label3.TabIndex = 3;
        label3.Text = "Username";
        label3.ForeColor = AppTheme.TextMuted;
        label3.Font = new Font("Segoe UI", 8.00F, FontStyle.Bold);
        usernamePanel.Name = "usernamePanel";
        usernamePanel.Location = new Point(339, 263);
        usernamePanel.Size = new Size(394, 38);
        usernamePanel.BackColor = AppTheme.InputBackground;
        usernamePanel.ForeColor = AppTheme.Text;
        usernamePanel.TabIndex = 4;
        usernamePanel.Padding = Padding.Empty;
        usernameTextBox.Name = "usernameTextBox";
        usernameTextBox.Location = new Point(8, 7);
        usernameTextBox.Size = new Size(377, 26);
        usernameTextBox.BackColor = AppTheme.InputBackground;
        usernameTextBox.ForeColor = AppTheme.Text;
        usernameTextBox.TabIndex = 5;
        usernameTextBox.AccessibleName = "Username";
        usernameTextBox.BorderStyle = BorderStyle.None;
        usernameTextBox.PlaceholderText = "Enter username";
        label7.Name = "label7";
        label7.Location = new Point(339, 326);
        label7.Size = new Size(394, 12);
        label7.BackColor = Color.Transparent;
        label7.ForeColor = AppTheme.Text;
        label7.TabIndex = 7;
        label7.Text = "Password";
        label7.ForeColor = AppTheme.TextMuted;
        label7.Font = new Font("Segoe UI", 8.00F, FontStyle.Bold);
        passwordPanel.Name = "passwordPanel";
        passwordPanel.Location = new Point(339, 347);
        passwordPanel.Size = new Size(394, 38);
        passwordPanel.BackColor = AppTheme.InputBackground;
        passwordPanel.ForeColor = AppTheme.Text;
        passwordPanel.TabIndex = 8;
        passwordPanel.Padding = Padding.Empty;
        passwordTextBox.Name = "passwordTextBox";
        passwordTextBox.Location = new Point(8, 7);
        passwordTextBox.Size = new Size(377, 26);
        passwordTextBox.BackColor = AppTheme.InputBackground;
        passwordTextBox.ForeColor = AppTheme.Text;
        passwordTextBox.TabIndex = 9;
        passwordTextBox.AccessibleName = "Password";
        passwordTextBox.BorderStyle = BorderStyle.None;
        passwordTextBox.PlaceholderText = "Enter password";
        passwordTextBox.UseSystemPasswordChar = true;
        signInButton.Name = "signInButton";
        signInButton.Location = new Point(339, 424);
        signInButton.Size = new Size(394, 36);
        signInButton.BackColor = AppTheme.ButtonPrimaryBackground;
        signInButton.ForeColor = AppTheme.ButtonPrimaryForeground;
        signInButton.TabIndex = 10;
        signInButton.Text = "Sign In";
        signInButton.AccessibleName = "Sign In";
        signInButton.ForeColor = AppTheme.ButtonPrimaryForeground;
        signInButton.Click += OpenWorkspace;
        Controls.Add(signInButton);
        passwordPanel.Controls.Add(passwordTextBox);
        Controls.Add(passwordPanel);
        Controls.Add(label7);
        usernamePanel.Controls.Add(usernameTextBox);
        Controls.Add(usernamePanel);
        Controls.Add(label3);
        Controls.Add(label1);
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = AppTheme.Background;
        Font = new Font("Segoe UI", 9F);
        ClientSize = new Size(620, 640);
        MinimumSize = new Size(480, 560);
        MaximizeBox = false;
        Name = "Login";
        Text = "ParkirAja — Sign In";
        StartPosition = FormStartPosition.CenterScreen;
        AcceptButton = signInButton;
        loginLayout = new TableLayoutPanel();
        loginLayout.Dock = DockStyle.Fill;
        loginLayout.ColumnCount = 3;
        loginLayout.RowCount = 3;
        loginLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        loginLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        loginLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        loginLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        loginLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        loginLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        loginFields = new TableLayoutPanel();
        loginFields.AutoSize = true;
        loginFields.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        loginFields.ColumnCount = 1;
        loginFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        loginFields.Width = 394;
        loginFields.MinimumSize = new Size(394, 0);
        label1.Size = new Size(394, 56);
        label1.TextAlign = ContentAlignment.MiddleCenter;
        label1.Margin = new Padding(0, 0, 0, 28);
        label3.Height = 22;
        label7.Height = 22;
        usernamePanel.Margin = new Padding(0, 4, 0, 20);
        passwordPanel.Margin = new Padding(0, 4, 0, 24);
        signInButton.Height = 44;
        signInButton.Margin = new Padding(0, 0, 0, 20);
        foreach (Control field in new Control[] { label1, label3, usernamePanel, label7, passwordPanel, signInButton })
        {
            field.Dock = DockStyle.Top;
            loginFields.Controls.Add(field);
        }
        loginLayout.Controls.Add(loginFields, 1, 1);
        Controls.Add(loginLayout);
        ResumeLayout(false);
        PerformLayout();
    }
    #endregion
    private Label label1 = null!;
    private Label label3 = null!;
    private RoundedPanel usernamePanel = null!;
    private TextBox usernameTextBox = null!;
    private Label label7 = null!;
    private RoundedPanel passwordPanel = null!;
    private TextBox passwordTextBox = null!;
    private RoundedButton signInButton = null!;
    private TableLayoutPanel loginLayout = null!;
    private TableLayoutPanel loginFields = null!;
}
