namespace ParkingSystem;

public partial class Login : Form
{
    public Login()
    {
        InitializeComponent();
    }

    private void OpenWorkspace(object? sender, EventArgs e)
    {
        // TODO: Tambahkan validasi & logika autentikasi login manual Anda di sini
        string username = usernameTextBox.Text.Trim();
        string password = passwordTextBox.Text;

        // Contoh alur navigasi sederhana:
        // Jika username mengandung "admin", buka workspace Admin.
        // Jika tidak, buka workspace Jukir (Petugas Parkir).
        bool isAdmin = username.Contains("admin", StringComparison.OrdinalIgnoreCase);

        using var workspace = new MainForm(isAdmin);
        Hide();
        workspace.ShowDialog(this);
        Show();
        passwordTextBox.Clear();
    }
}
