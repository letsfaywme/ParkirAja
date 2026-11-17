using ParkingSystem.Security;
using ParkingSystem.Database;

namespace ParkingSystem;

/// <summary>
/// Tampilan modul manajemen akun petugas juru parkir (Manage Jukir Data).
/// Sesuai diagram Draw.io:
/// - 1 - Use Case: Manage Jukir Data
/// - 4 - Wireframe: Form input (nama jukir, username, area tugas, password),
///   tabel daftar akun petugas, tombol Add, Edit, Delete (toggle status aktif).
/// </summary>
public partial class JukirAccounts : UserControl
{
    /// <summary>
    /// Inisialisasi antarmuka kelola akun jukir, mengisi dropdown pilihan area tugas,
    /// dan mengaitkan event handler tombol.
    /// </summary>
    public JukirAccounts()
    {
        InitializeComponent();
        ResponsiveLayout.Attach(this);

        // Isi dropdown area penugasan dari daftar titik area parkir yang ada
        assignedareaComboBox.Items.Clear();
        foreach (var s in InMemoryDb.Spots)
            assignedareaComboBox.Items.Add(s.Name);
        if (assignedareaComboBox.Items.Count > 0)
            assignedareaComboBox.SelectedIndex = 0;

        // Registrasi event tombol
        addButton.Click += AddButton_Click;
        editButton.Click += EditButton_Click;
        deleteButton.Click += DeleteButton_Click;

        LoadAccounts();
    }

    /// <summary>
    /// Memuat daftar akun petugas parkir dari InMemoryDb/EF Core ke DataGridView.
    /// </summary>
    private void LoadAccounts()
    {
        grid5.Rows.Clear();
        foreach (var u in InMemoryDb.Users)
        {
            var spot = InMemoryDb.Spots.FirstOrDefault(s => s.Id == u.ParkingSpotId);
            int idx = grid5.Rows.Add(
                u.Name,
                u.Username,
                spot?.Name ?? "-",
                u.IsActive ? "ACTIVE" : "INACTIVE"
            );
            grid5.Rows[idx].Tag = u; // Simpan referensi objek akun
        }
    }

    /// <summary>
    /// Event handler tombol "Add".
    /// Menambahkan akun jukir baru dengan peran UserRole.Jukir dan mengaitkannya ke area tugas.
    /// </summary>
    private void AddButton_Click(object? sender, EventArgs e)
    {
        string name = jukirnameTextBox.Text.Trim();
        string username = usernameTextBox.Text.Trim();
        string pwd = passwordTextBox.Text;

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(username))
        {
            MessageBox.Show("Nama dan username harus diisi!", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var selectedSpotName = assignedareaComboBox.SelectedItem?.ToString();
        var spot = InMemoryDb.Spots.FirstOrDefault(s => s.Name == selectedSpotName);

        var newUser = new AppUser
        {
            Id = InMemoryDb.NextUserId(),
            Name = name,
            Username = username,
            PasswordHash = PasswordHasher.HashPassword(string.IsNullOrWhiteSpace(pwd) ? "jukirpassword" : pwd),
            Role = UserRole.Jukir,
            ParkingSpotId = spot?.Id ?? 1,
            IsActive = true
        };
        InMemoryDb.AddUser(newUser);

        MessageBox.Show($"Akun Jukir '{newUser.Username}' berhasil dibuat!", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);

        jukirnameTextBox.Clear();
        usernameTextBox.Clear();
        passwordTextBox.Clear();
        LoadAccounts();
    }

    /// <summary>
    /// Event handler tombol "Edit".
    /// Memperbarui nama, username, kata sandi, atau lokasi penugasan akun yang dipilih.
    /// </summary>
    private void EditButton_Click(object? sender, EventArgs e)
    {
        if (grid5.CurrentRow?.Tag is not AppUser user)
        {
            MessageBox.Show("Pilih akun dari tabel!", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        user.Name = jukirnameTextBox.Text.Trim();
        user.Username = usernameTextBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(passwordTextBox.Text))
            user.PasswordHash = PasswordHasher.HashPassword(passwordTextBox.Text);

        var selectedSpotName = assignedareaComboBox.SelectedItem?.ToString();
        var spot = InMemoryDb.Spots.FirstOrDefault(s => s.Name == selectedSpotName);
        if (spot != null) user.ParkingSpotId = spot.Id;

        InMemoryDb.SaveChanges();
        MessageBox.Show($"Akun '{user.Username}' berhasil diperbarui!", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
        LoadAccounts();
    }

    /// <summary>
    /// Event handler tombol "Delete" untuk mengaktifkan atau menonaktifkan akun petugas (toggle isActive).
    /// </summary>
    private void DeleteButton_Click(object? sender, EventArgs e)
    {
        if (grid5.CurrentRow?.Tag is not AppUser user)
        {
            MessageBox.Show("Pilih akun dari tabel!", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        user.IsActive = !user.IsActive;
        InMemoryDb.SaveChanges();
        MessageBox.Show($"Status akun '{user.Username}' diubah menjadi: {(user.IsActive ? "ACTIVE" : "INACTIVE")}",
            "Status Berubah", MessageBoxButtons.OK, MessageBoxIcon.Information);
        LoadAccounts();
    }
}
