#nullable enable annotations
namespace ParkingSystem;

partial class MainForm
{
    private System.ComponentModel.IContainer? components = null;
    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        sidebar = new Panel();
        brandLabel = new Label();
        workspaceLabel = new Label();
        dashboardButton = new RoundedButton();
        secondButton = new RoundedButton();
        thirdButton = new RoundedButton();
        fourthButton = new RoundedButton();
        fifthButton = new RoundedButton();
        signOutButton = new RoundedButton();
        contentPanel = new Panel();
        SuspendLayout();
        sidebar.SuspendLayout();
        sidebar.BackColor = AppTheme.SidebarBackground;
        sidebar.Dock = DockStyle.Left;
        sidebar.Size = new Size(210, 707);
        sidebar.Name = "sidebar";
        brandLabel.Text = "ParkirAja";
        brandLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        brandLabel.ForeColor = AppTheme.Text;
        brandLabel.Location = new Point(20, 28);
        brandLabel.Size = new Size(180, 30);
        workspaceLabel.Text = "JUKIR WORKSPACE";
        workspaceLabel.ForeColor = AppTheme.Text;
        workspaceLabel.Location = new Point(20, 70);
        workspaceLabel.Size = new Size(180, 24);
        dashboardButton.Name = "dashboardButton";
        dashboardButton.Text = "Dashboard";
        dashboardButton.Location = new Point(14, 120);
        dashboardButton.Size = new Size(182, 44);
        dashboardButton.TabIndex = 0;
        dashboardButton.Click += Navigate;
        secondButton.Name = "secondButton";
        secondButton.Text = "Vehicle Entry";
        secondButton.Location = new Point(14, 174);
        secondButton.Size = new Size(182, 44);
        secondButton.TabIndex = 1;
        secondButton.Click += Navigate;
        thirdButton.Name = "thirdButton";
        thirdButton.Text = "Vehicle Exit";
        thirdButton.Location = new Point(14, 228);
        thirdButton.Size = new Size(182, 44);
        thirdButton.TabIndex = 2;
        thirdButton.Click += Navigate;
        fourthButton.Name = "fourthButton";
        fourthButton.Text = "Transaction History";
        fourthButton.Location = new Point(14, 282);
        fourthButton.Size = new Size(182, 44);
        fourthButton.TabIndex = 3;
        fourthButton.Click += Navigate;
        fifthButton.Name = "fifthButton";
        fifthButton.Text = "Daily Report";
        fifthButton.Location = new Point(14, 336);
        fifthButton.Size = new Size(182, 44);
        fifthButton.TabIndex = 4;
        fifthButton.Click += Navigate;
        signOutButton.Name = "signOutButton";
        signOutButton.Text = "Sign Out";
        signOutButton.Location = new Point(14, 639);
        signOutButton.Size = new Size(182, 44);
        signOutButton.BackColor = AppTheme.ButtonSecondaryBackground;
        signOutButton.ForeColor = AppTheme.ButtonSecondaryForeground;
        signOutButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        signOutButton.TabIndex = 5;
        signOutButton.Click += SignOut;
        sidebar.Controls.Add(brandLabel);
        sidebar.Controls.Add(workspaceLabel);
        sidebar.Controls.Add(dashboardButton);
        sidebar.Controls.Add(secondButton);
        sidebar.Controls.Add(thirdButton);
        sidebar.Controls.Add(fourthButton);
        sidebar.Controls.Add(fifthButton);
        sidebar.Controls.Add(signOutButton);
        contentPanel.Name = "contentPanel";
        contentPanel.BackColor = AppTheme.Background;
        contentPanel.Dock = DockStyle.Fill;
        Controls.Add(contentPanel);
        Controls.Add(sidebar);
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = AppTheme.Background;
        ForeColor = AppTheme.Text;
        Font = new Font("Segoe UI", 9F);
        ClientSize = new Size(1260, 707);
        MinimumSize = new Size(920, 600);
        StartPosition = FormStartPosition.CenterScreen;
        Text = "ParkirAja — Jukir Dashboard";
        Name = "MainForm";
        sidebar.ResumeLayout(false);
        ResumeLayout(false);
    }

    private Panel sidebar = null!;
    private Label brandLabel = null!;
    private Label workspaceLabel = null!;
    private RoundedButton dashboardButton = null!;
    private RoundedButton secondButton = null!;
    private RoundedButton thirdButton = null!;
    private RoundedButton fourthButton = null!;
    private RoundedButton fifthButton = null!;
    private RoundedButton signOutButton = null!;
    private Panel contentPanel = null!;
}
