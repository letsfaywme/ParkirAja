namespace ParkingSystem;

/// <summary>
/// Tampilan Dashboard untuk Juru Parkir.
/// Sesuai diagram Draw.io:
/// - 1 - Use Case: Menampilkan ringkasan operasional harian petugas parkir
/// - 4 - Wireframe (jd): KPI Cards (Vehicles In, Vehicles Out, Revenue) + DataGridView transaksi terakhir
/// </summary>
/// <remarks>
/// Halaman ini sengaja dipertahankan sebagai tampilan statis tanpa logic.
/// Fokus demonstrasi proyek berada pada modul Vehicle Entry.
/// </remarks>
public partial class JukirDashboard : UserControl
{
    /// <summary>
    /// Inisialisasi antarmuka dashboard jukir.
    /// </summary>
    public JukirDashboard()
    {
        InitializeComponent();
        ResponsiveLayout.Attach(this);

        refreshButton.Click += RefreshButton_Click;
    }

    /// <summary>
    /// Event handler tombol "Refresh". Tanpa logic data (tampilan statis).
    /// </summary>
    private void RefreshButton_Click(object? sender, EventArgs e)
    {
    }
}
