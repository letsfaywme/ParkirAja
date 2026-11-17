using ParkingSystem.Database;

namespace ParkingSystem;

/// <summary>
/// Jendela form login utama aplikasi.
/// Sesuai diagram Draw.io:
/// - 1 - Use Case: Login (Jukir &amp; Admin)
/// - 2 - Activity Diagram: Fill username/password -> Validasi -> Branch Admin vs Jukir
/// - 4 - Wireframe: Form Login dengan judul, input username, input password, tombol Sign In
/// </summary>
public partial class Login : Form
{
    /// <summary>
    /// Inisialisasi komponen visual form login.
    /// </summary>
    public Login()
    {
        InitializeComponent();
        LabelMetrics.EnsureReadable(this);
    }

    /// <summary>
    /// Event handler tombol "Sign In".
    /// Melakukan validasi input, autentikasi melalui InMemoryDb/EF Core,
    /// dan membuka MainForm dengan hak akses sesuai peran (Admin atau Jukir).
    /// </summary>
    private void OpenWorkspace(object? sender, EventArgs e)
    {
        string username = usernameTextBox.Text.Trim();
        string password = passwordTextBox.Text;

        if (string.IsNullOrWhiteSpace(username) && string.IsNullOrWhiteSpace(password))
        {
            OpenWorkspaceFor(new UserSession(2, "jukir1", "Budi Santoso", UserRole.Jukir, 1));
            return;
        }

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show("Silakan masukkan username dan password.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var session = InMemoryDb.Authenticate(username, password);
        if (session == null)
        {
            MessageBox.Show("Username atau password salah!\n\nDemo Akun (In-Memory):\n• Admin: admin / adminpassword\n• Jukir: jukir1 / jukirpassword\n\nMode demo: kosongkan username & password lalu klik Sign In untuk membuka seluruh menu.",
                "Login Gagal", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        OpenWorkspaceFor(session);
    }

    /// <summary>Membuka workspace utama untuk sesi tertentu (dipakai login & mode demo).</summary>
    private void OpenWorkspaceFor(UserSession session)
    {
        using var workspace = new MainForm(session);
        Hide();
        workspace.ShowDialog(this);
        Show();
        passwordTextBox.Clear();
    }

    private void passwordTextBox_TextChanged(object sender, EventArgs e)
    {

    }
}
