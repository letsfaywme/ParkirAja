namespace ParkingSystem;

/// <summary>
/// Tampilan Dashboard Real-time untuk Administrator.
/// Sesuai diagram Draw.io:
/// - 1 - Use Case: Real-time Dashboard
/// - 2 - Activity Diagram: Retrieve today's data -> Calculate stats &amp; revenue -> Display dashboard
/// - 4 - Wireframe (ad): KPI Cards (Total Capacity, Occupied, Available, Revenue) + DataGridView status area
/// </summary>
/// <remarks>
/// Halaman ini sengaja dipertahankan sebagai tampilan statis tanpa logic.
/// Fokus demonstrasi proyek berada pada modul Vehicle Entry.
/// </remarks>
public partial class AdminDashboard : UserControl
{
    /// <summary>
    /// Inisialisasi antarmuka dashboard admin.
    /// </summary>
    public AdminDashboard()
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
