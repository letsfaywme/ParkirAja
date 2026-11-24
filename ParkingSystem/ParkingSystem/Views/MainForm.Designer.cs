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
        brandLogo = new PictureBox();
        brandLabel = new Label();
        workspaceLabel = new Label();
        dashboardButton = new Button();
        secondButton = new Button();
        thirdButton = new Button();
        fourthButton = new Button();
        fifthButton = new Button();
        signOutButton = new Button();
        contentPanel = new Panel();
        sidebar.SuspendLayout();
        SuspendLayout();
        // 
        // sidebar
        // 
        sidebar.BackColor = Color.FromArgb(240, 253, 244);
        sidebar.Controls.Add(brandLogo);
        sidebar.Controls.Add(brandLabel);
        sidebar.Controls.Add(workspaceLabel);
        sidebar.Controls.Add(dashboardButton);
        sidebar.Controls.Add(secondButton);
        sidebar.Controls.Add(thirdButton);
        sidebar.Controls.Add(fourthButton);
        sidebar.Controls.Add(fifthButton);
        sidebar.Controls.Add(signOutButton);
        sidebar.Dock = DockStyle.Left;
        sidebar.Location = new Point(0, 0);
        sidebar.Margin = new Padding(4, 4, 4, 4);
        sidebar.Name = "sidebar";
        sidebar.Size = new Size(262, 884);
        sidebar.TabIndex = 1;
        // 
        // brandLogo
        // 
        brandLogo.BackColor = Color.Transparent;
        brandLogo.Location = new Point(25, 32);
        brandLogo.Margin = new Padding(0);
        brandLogo.Name = "brandLogo";
        brandLogo.Size = new Size(40, 40);
        brandLogo.SizeMode = PictureBoxSizeMode.Zoom;
        brandLogo.TabIndex = 0;
        brandLogo.TabStop = false;
        brandLogo.Image = AppBrand.Logo;
        // 
        // brandLabel
        // 
        brandLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        brandLabel.ForeColor = Color.FromArgb(15, 23, 42);
        brandLabel.Location = new Point(74, 35);
        brandLabel.Margin = new Padding(4, 0, 4, 0);
        brandLabel.Name = "brandLabel";
        brandLabel.Size = new Size(176, 38);
        brandLabel.TabIndex = 0;
        brandLabel.Text = "ParkirAja";
        // 
        // workspaceLabel
        // 
        workspaceLabel.ForeColor = Color.FromArgb(15, 23, 42);
        workspaceLabel.Location = new Point(25, 88);
        workspaceLabel.Margin = new Padding(4, 0, 4, 0);
        workspaceLabel.Name = "workspaceLabel";
        workspaceLabel.Size = new Size(225, 30);
        workspaceLabel.TabIndex = 1;
        workspaceLabel.Text = "JUKIR WORKSPACE";
        // 
        // dashboardButton
        // 
        dashboardButton.Cursor = Cursors.Hand;
        dashboardButton.FlatAppearance.BorderSize = 0;
        dashboardButton.FlatStyle = FlatStyle.Flat;
        dashboardButton.Location = new Point(18, 150);
        dashboardButton.Margin = new Padding(4, 4, 4, 4);
        dashboardButton.Name = "dashboardButton";
        dashboardButton.Size = new Size(228, 55);
        dashboardButton.TabIndex = 0;
        dashboardButton.Text = "Dashboard";
        dashboardButton.Click += Navigate;
        // 
        // secondButton
        // 
        secondButton.Cursor = Cursors.Hand;
        secondButton.FlatAppearance.BorderSize = 0;
        secondButton.FlatStyle = FlatStyle.Flat;
        secondButton.Location = new Point(18, 218);
        secondButton.Margin = new Padding(4, 4, 4, 4);
        secondButton.Name = "secondButton";
        secondButton.Size = new Size(228, 55);
        secondButton.TabIndex = 1;
        secondButton.Text = "Vehicle Entry";
        secondButton.Click += Navigate;
        // 
        // thirdButton
        // 
        thirdButton.Cursor = Cursors.Hand;
        thirdButton.FlatAppearance.BorderSize = 0;
        thirdButton.FlatStyle = FlatStyle.Flat;
        thirdButton.Location = new Point(18, 285);
        thirdButton.Margin = new Padding(4, 4, 4, 4);
        thirdButton.Name = "thirdButton";
        thirdButton.Size = new Size(228, 55);
        thirdButton.TabIndex = 2;
        thirdButton.Text = "Vehicle Exit";
        thirdButton.Click += Navigate;
        // 
        // fourthButton
        // 
        fourthButton.Cursor = Cursors.Hand;
        fourthButton.FlatAppearance.BorderSize = 0;
        fourthButton.FlatStyle = FlatStyle.Flat;
        fourthButton.Location = new Point(18, 352);
        fourthButton.Margin = new Padding(4, 4, 4, 4);
        fourthButton.Name = "fourthButton";
        fourthButton.Size = new Size(228, 55);
        fourthButton.TabIndex = 3;
        fourthButton.Text = "Transaction History";
        fourthButton.Click += Navigate;
        // 
        // fifthButton
        // 
        fifthButton.Cursor = Cursors.Hand;
        fifthButton.FlatAppearance.BorderSize = 0;
        fifthButton.FlatStyle = FlatStyle.Flat;
        fifthButton.Location = new Point(18, 420);
        fifthButton.Margin = new Padding(4, 4, 4, 4);
        fifthButton.Name = "fifthButton";
        fifthButton.Size = new Size(228, 55);
        fifthButton.TabIndex = 4;
        fifthButton.Text = "Daily Report";
        fifthButton.Click += Navigate;
        // 
        // signOutButton
        // 
        signOutButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        signOutButton.BackColor = Color.FromArgb(241, 245, 249);
        signOutButton.FlatAppearance.BorderSize = 0;
        signOutButton.FlatStyle = FlatStyle.Flat;
        signOutButton.ForeColor = Color.FromArgb(15, 23, 42);
        signOutButton.Location = new Point(18, 799);
        signOutButton.Margin = new Padding(4, 4, 4, 4);
        signOutButton.Name = "signOutButton";
        signOutButton.Size = new Size(228, 55);
        signOutButton.TabIndex = 5;
        signOutButton.Text = "Sign Out";
        signOutButton.UseVisualStyleBackColor = false;
        signOutButton.Click += SignOut;
        // 
        // contentPanel
        // 
        contentPanel.BackColor = Color.FromArgb(248, 250, 252);
        contentPanel.Dock = DockStyle.Fill;
        contentPanel.Location = new Point(262, 0);
        contentPanel.Margin = new Padding(4, 4, 4, 4);
        contentPanel.Name = "contentPanel";
        contentPanel.Size = new Size(1313, 884);
        contentPanel.TabIndex = 0;
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(120F, 120F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(248, 250, 252);
        ClientSize = new Size(1575, 884);
        Controls.Add(contentPanel);
        Controls.Add(sidebar);
        Font = new Font("Segoe UI", 9F);
        ForeColor = Color.FromArgb(15, 23, 42);
        Margin = new Padding(4, 4, 4, 4);
        MinimumSize = new Size(1146, 738);
        Name = "MainForm";
        Icon = AppBrand.Icon;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "ParkirAja — Jukir Dashboard";
        sidebar.ResumeLayout(false);
        ResumeLayout(false);
    }

    private Panel sidebar = null!;
    private PictureBox brandLogo = null!;
    private Label brandLabel = null!;
    private Label workspaceLabel = null!;
    private Button dashboardButton = null!;
    private Button secondButton = null!;
    private Button thirdButton = null!;
    private Button fourthButton = null!;
    private Button fifthButton = null!;
    private Button signOutButton = null!;
    private Panel contentPanel = null!;
}
