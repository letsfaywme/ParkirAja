namespace ParkingSystem;

public partial class MainForm : Form
{
    private readonly bool admin;
    private readonly bool runtime;
    private readonly CancellationTokenSource lifetime = new();
    private bool lifetimeDisposed;

    public MainForm()
    {
        InitializeComponent();
        workspaceLabel.Text = "DESIGNER WORKSPACE";
        signOutButton.Enabled = false;
        Disposed += (_, _) =>
        {
            if (lifetimeDisposed) return;
            lifetimeDisposed = true;
            lifetime.Cancel();
            lifetime.Dispose();
        };
    }

    public MainForm(UserSession session, ParkingService service) : this()
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(service);
        admin = session.Role == UserRole.Admin;
        runtime = true;
        signOutButton.Enabled = true;
        UserControl[] pages = admin
            ? [new AdminDashboard(), new ReportValidation(), new ParkingAreas(), new JukirAccounts(), new ExportReports()]
            : [new JukirDashboard(), new VehicleEntry(), new VehicleExit(), new TransactionHistory(), new DailyReport()];
        string[] titles = admin
            ? ["Dashboard", "Validate Reports", "Parking Areas", "Jukir Accounts", "Export Reports"]
            : ["Dashboard", "Vehicle Entry", "Vehicle Exit", "Transaction History", "Daily Report"];
        RoundedButton[] buttons = [dashboardButton, secondButton, thirdButton, fourthButton, fifthButton];
        workspaceLabel.Text = admin ? "ADMIN WORKSPACE" : "JUKIR WORKSPACE";
        for (int i = 0; i < pages.Length; i++)
        {
            BindPage(pages[i], session, service);
            pages[i].Dock = DockStyle.Fill;
            contentPanel.Controls.Add(pages[i]);
            buttons[i].Text = titles[i];
            buttons[i].AccessibleName = titles[i];
            buttons[i].Tag = pages[i];
        }
        ShowPage(dashboardButton);
    }

    private void BindPage(UserControl page, UserSession session, ParkingService service)
    {
        var cancellationToken = lifetime.Token;
        switch (page)
        {
            case AdminDashboard p: p.ShellCancellation = cancellationToken; p.Bind(session, service); break;
            case ReportValidation p: p.ShellCancellation = cancellationToken; p.Bind(session, service); break;
            case ParkingAreas p: p.ShellCancellation = cancellationToken; p.Bind(session, service); break;
            case JukirAccounts p: p.ShellCancellation = cancellationToken; p.Bind(session, service); break;
            case ExportReports p: p.ShellCancellation = cancellationToken; p.Bind(session, service); break;
            case JukirDashboard p: p.ShellCancellation = cancellationToken; p.Bind(session, service); break;
            case VehicleEntry p: p.ShellCancellation = cancellationToken; p.Bind(session, service); break;
            case VehicleExit p: p.ShellCancellation = cancellationToken; p.Bind(session, service); break;
            case TransactionHistory p: p.ShellCancellation = cancellationToken; p.Bind(session, service); break;
            case DailyReport p: p.ShellCancellation = cancellationToken; p.Bind(session, service); break;
        }
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (runtime && dashboardButton.Tag is UserControl page)
            await RefreshPageAsync(page);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (!e.Cancel) lifetime.Cancel();
    }

    private async void Navigate(object? sender, EventArgs e)
    {
        if (!runtime || lifetime.IsCancellationRequested) return;
        if (sender is RoundedButton button && button.Tag is UserControl page)
        {
            ShowPage(button);
            await RefreshPageAsync(page);
        }
    }

    private async Task RefreshPageAsync(UserControl page)
    {
        var cancellationToken = lifetime.Token;
        try
        {
            await (page switch
            {
                AdminDashboard p => p.RefreshAsync(cancellationToken),
                ReportValidation p => p.RefreshAsync(cancellationToken),
                ParkingAreas p => p.RefreshAsync(cancellationToken),
                JukirAccounts p => p.RefreshAsync(cancellationToken),
                ExportReports p => p.RefreshAsync(cancellationToken),
                JukirDashboard p => p.RefreshAsync(cancellationToken),
                VehicleEntry p => p.RefreshAsync(cancellationToken),
                VehicleExit p => p.RefreshAsync(cancellationToken),
                TransactionHistory p => p.RefreshAsync(cancellationToken),
                DailyReport p => p.RefreshAsync(cancellationToken),
                _ => Task.CompletedTask
            });
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            if (!IsDisposed && !cancellationToken.IsCancellationRequested)
            {
                string message = ex is ArgumentException or InvalidOperationException or UnauthorizedAccessException
                    ? ex.Message
                    : "Data tidak dapat dimuat. Periksa koneksi database.";
                MessageBox.Show(this, message, "ParkirAja", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
        Text = "ParkirAja — " + (admin ? "Admin" : "Jukir") + " / " + selected.Text;
        page.BringToFront();
        page.Focus();
    }

    private void SignOut(object? sender, EventArgs e)
    {
        if (runtime) Close();
    }
}
