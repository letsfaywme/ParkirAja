using ParkingSystem.Database;

namespace ParkingSystem;

/// <summary>
/// Tampilan Dashboard untuk Juru Parkir.
/// Sesuai diagram Draw.io:
/// - 1 - Use Case: Menampilkan ringkasan operasional harian petugas parkir
/// - 4 - Wireframe (jd): KPI Cards (Vehicles In, Vehicles Out, Revenue) + DataGridView transaksi terakhir
/// </summary>
public partial class JukirDashboard : UserControl
{
    /// <summary>
    /// Inisialisasi antarmuka dashboard jukir dan mengaitkan event handler.
    /// </summary>
    public JukirDashboard()
    {
        InitializeComponent();
        ResponsiveLayout.Attach(this);

        refreshButton.Click += RefreshButton_Click;
        LoadDashboard();
    }

    /// <summary>
    /// Event handler tombol "Refresh" untuk memperbarui data KPI dan tabel.
    /// </summary>
    private void RefreshButton_Click(object? sender, EventArgs e)
    {
        LoadDashboard();
    }

    /// <summary>
    /// Memuat data statistik harian jukir aktif (kendaraan masuk, kendaraan keluar, total penerimaan)
    /// serta 10 riwayat transaksi terkini dari InMemoryDb/EF Core.
    /// </summary>
    private void LoadDashboard()
    {
        var summary = InMemoryDb.GetJukirSummary(2);

        // Update teks angka pada metric panel
        label6.Text = summary.Entries.ToString();
        label13.Text = summary.Exits.ToString();
        label20.Text = $"Rp {summary.Revenue:N0}";

        // Tampilkan 10 transaksi terakhir ke DataGridView
        grid26.Rows.Clear();
        foreach (var t in summary.Latest)
        {
            grid26.Rows.Add(
                t.EntryTime.ToString("HH:mm:ss"),
                t.PlateNumber,
                t.VehicleType.ToString(),
                t.Fare.HasValue ? $"Rp {t.Fare.Value:N0}" : $"Rp {t.RateAtEntry:N0}/j"
            );
        }
    }
}
