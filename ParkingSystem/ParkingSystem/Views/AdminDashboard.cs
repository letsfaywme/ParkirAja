using ParkingSystem.Database;

namespace ParkingSystem;

/// <summary>
/// Tampilan Dashboard Real-time untuk Administrator.
/// Sesuai diagram Draw.io:
/// - 1 - Use Case: Real-time Dashboard
/// - 2 - Activity Diagram: Retrieve today's data -> Calculate stats &amp; revenue -> Display dashboard
/// - 4 - Wireframe (ad): KPI Cards (Total Capacity, Occupied, Available, Revenue) + DataGridView status area
/// </summary>
public partial class AdminDashboard : UserControl
{
    /// <summary>
    /// Inisialisasi antarmuka dashboard admin dan mengaitkan event handler tombol.
    /// </summary>
    public AdminDashboard()
    {
        InitializeComponent();
        ResponsiveLayout.Attach(this);

        refreshButton.Click += RefreshButton_Click;
        LoadDashboard();
    }

    /// <summary>
    /// Event handler tombol "Refresh" untuk memperbarui metrik secara real-time.
    /// </summary>
    private void RefreshButton_Click(object? sender, EventArgs e)
    {
        LoadDashboard();
    }

    /// <summary>
    /// Menghitung total kapasitas seluruh area, total slot terisi, sisa ketersediaan slot,
    /// dan total omzet pendapatan transaksi hari ini dari database.
    /// </summary>
    private void LoadDashboard()
    {
        var summary = InMemoryDb.GetAdminSummary();

        // Kalkulasi okupansi seluruh area parkir
        int totalCap = InMemoryDb.Spots.Sum(s => s.Capacity);
        int totalOcc = InMemoryDb.Spots.Sum(s => s.Occupied);
        int available = Math.Max(0, totalCap - totalOcc);

        // Update teks pada metric panel card
        label6.Text = totalCap.ToString();
        label13.Text = totalOcc.ToString();
        label20.Text = available.ToString();
        label27.Text = $"Rp {summary.Revenue:N0}";

        // Tampilkan status dan ketersediaan tiap area parkir di tabel
        grid33.Rows.Clear();
        foreach (var s in InMemoryDb.Spots)
        {
            int spotAvail = Math.Max(0, s.Capacity - s.Occupied);
            grid33.Rows.Add(
                s.Name,
                s.Capacity.ToString(),
                s.Occupied.ToString(),
                spotAvail.ToString(),
                s.IsActive ? "ACTIVE" : "INACTIVE"
            );
        }
    }
}
