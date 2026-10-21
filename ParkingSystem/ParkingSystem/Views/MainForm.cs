namespace ParkingSystem;

public partial class MainForm : Form
{
    private readonly bool isAdmin;

    public MainForm() : this(false)
    {
    }

    public MainForm(bool isAdmin)
    {
        InitializeComponent();
        this.isAdmin = isAdmin;
        SetupPages();
    }

    private void SetupPages()
    {
        workspaceLabel.Text = isAdmin ? "ADMIN WORKSPACE" : "JUKIR WORKSPACE";

        UserControl[] pages = isAdmin
            ? [new AdminDashboard(), new ReportValidation(), new ParkingAreas(), new JukirAccounts(), new ExportReports()]
            : [new JukirDashboard(), new VehicleEntry(), new VehicleExit(), new TransactionHistory(), new DailyReport()];

        string[] titles = isAdmin
            ? ["Dashboard", "Validate Reports", "Parking Areas", "Jukir Accounts", "Export Reports"]
            : ["Dashboard", "Vehicle Entry", "Vehicle Exit", "Transaction History", "Daily Report"];

        RoundedButton[] buttons = [dashboardButton, secondButton, thirdButton, fourthButton, fifthButton];

        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].Dock = DockStyle.Fill;
            contentPanel.Controls.Add(pages[i]);
            buttons[i].Text = titles[i];
            buttons[i].AccessibleName = titles[i];
            buttons[i].Tag = pages[i];
        }

        ShowPage(dashboardButton);
    }

    private void Navigate(object? sender, EventArgs e)
    {
        if (sender is RoundedButton button && button.Tag is UserControl)
        {
            ShowPage(button);
        }
    }

    private void ShowPage(RoundedButton selected)
    {
        var page = (UserControl)selected.Tag!;
        foreach (Control child in contentPanel.Controls)
            child.Visible = ReferenceEquals(child, page);

        foreach (Control control in sidebar.Controls)
        {
            if (control is not RoundedButton button || button.Tag is not UserControl) continue;
            bool active = ReferenceEquals(button, selected);
            button.BackColor = active ? AppTheme.SidebarActive : sidebar.BackColor;
            button.ForeColor = active ? AppTheme.ButtonPrimaryForeground : AppTheme.Text;
            button.Invalidate();
        }

        Text = "ParkirAja — " + (isAdmin ? "Admin" : "Jukir") + " / " + selected.Text;
        page.BringToFront();
        page.Focus();
    }

    private void SignOut(object? sender, EventArgs e)
    {
        Close();
    }
}
