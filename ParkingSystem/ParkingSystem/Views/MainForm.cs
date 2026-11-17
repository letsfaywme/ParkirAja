namespace ParkingSystem;

/// <summary>
/// Jendela form utama aplikasi yang berfungsi sebagai shell penampung halaman (UserControl).
/// Mengimplementasikan navigasi sidebar dinamis berdasarkan peran pengguna (Admin atau Jukir)
/// sesuai halaman 4 - Wireframe di Draw.io.
/// </summary>
public partial class MainForm : Form
{
    private readonly bool isAdmin;
    private IDemoPage? demoPage;

    /// <summary>Informasi sesi pengguna yang sedang login saat ini</summary>
    public UserSession? Session { get; }

    /// <summary>
    /// Konstruktor tanpa parameter untuk kebutuhan Windows Forms Designer (default ke Jukir).
    /// </summary>
    public MainForm() : this(false)
    {
    }

    /// <summary>
    /// Konstruktor berbasis boolean isAdmin untuk demo atau preview cepat.
    /// </summary>
    public MainForm(bool isAdmin) : this(new UserSession(
        isAdmin ? 1 : 2,
        isAdmin ? "admin" : "jukir1",
        isAdmin ? "Administrator Sistem" : "Budi Santoso",
        isAdmin ? UserRole.Admin : UserRole.Jukir,
        isAdmin ? null : 1))
    {
    }

    /// <summary>
    /// Konstruktor utama menerima sesi pengguna aktif hasil autentikasi.
    /// Mempersiapkan menu sidebar dan memuat modul yang sesuai dengan hak akses.
    /// </summary>
    public MainForm(UserSession session)
    {
        InitializeComponent();
        Session = session;
        isAdmin = session.Role == UserRole.Admin;
        SetupPages();
    }

    /// <summary>
    /// Mengonfigurasi tombol menu navigasi sidebar dan modul halaman (UserControl)
    /// berdasarkan peran pengguna aktif (Admin vs Jukir).
    /// Sesuai pembagian menu di Diagram 4 - Wireframe Draw.io.
    /// </summary>
    private void SetupPages()
    {
        workspaceLabel.Text = isAdmin ? $"ADMIN — {Session?.Name}" : $"JUKIR — {Session?.Name}";
        Text = isAdmin ? "ParkirAja — Admin Dashboard" : "ParkirAja — Jukir Dashboard";

        UserControl[] pages = isAdmin
            ? [new AdminDashboard(), new ReportValidation(), new ParkingAreas(), new JukirAccounts(), new ExportReports()]
            : [new JukirDashboard(), new VehicleEntry(), new VehicleExit(), new TransactionHistory(), new DailyReport()];

        string[] titles = isAdmin
            ? ["Dashboard", "Validate Reports", "Parking Areas", "Jukir Accounts", "Export Reports"]
            : ["Dashboard", "Vehicle Entry", "Vehicle Exit", "Transaction History", "Daily Report"];

        Button[] buttons = [dashboardButton, secondButton, thirdButton, fourthButton, fifthButton];

        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].Dock = DockStyle.Fill;
            buttons[i].Text = titles[i];
            buttons[i].AccessibleName = titles[i];
            buttons[i].Tag = pages[i];
        }

        if (Session != null)
        {
            foreach (var p in pages)
            {
                if (p is ISessionAware sa) sa.BindSession(Session);
            }
        }

        demoPage = pages.OfType<IDemoPage>().FirstOrDefault();
        contentPanel.Controls.AddRange(pages);

        // Label di Designer diukur untuk 96 DPI; naikkan tinggi kotak yang kurang agar teks utuh.
        LabelMetrics.EnsureReadable(contentPanel);

        ShowPage(dashboardButton);
    }

    /// <summary>
    /// Event handler klik tombol navigasi sidebar untuk berganti halaman.
    /// </summary>
    private void Navigate(object? sender, EventArgs e)
    {
        if (sender is Button button && button.Tag is UserControl)
        {
            ShowPage(button);
        }
    }

    /// <summary>
    /// Menampilkan modul halaman yang dipilih dan memperbarui status aktif visual pada tombol menu.
    /// </summary>
    /// <param name="selected">Tombol menu navigasi yang sedang dipilih</param>
    private void ShowPage(Button selected)
    {
        var page = (UserControl)selected.Tag!;

        if (!ReferenceEquals(page, demoPage))
        {
            demoPage?.OnDemoPageLeft();
        }

        foreach (Control child in contentPanel.Controls)
        {
            child.Visible = ReferenceEquals(child, page);
        }
        page.BringToFront();

        Button[] buttons = [dashboardButton, secondButton, thirdButton, fourthButton, fifthButton];
        foreach (var btn in buttons)
        {
            bool isActive = ReferenceEquals(btn, selected);
            btn.BackColor = isActive ? AppTheme.Primary : AppTheme.SidebarBackground;
            btn.ForeColor = isActive ? AppTheme.CardBackground : AppTheme.Text;
        }
    }

    /// <summary>
    /// Event handler tombol "Sign Out" untuk menutup workspace dan kembali ke form login.
    /// Sesuai nama event di MainForm.Designer.cs (SignOut).
    /// </summary>
    private void SignOut(object? sender, EventArgs e)
    {
        Close();
    }
}
