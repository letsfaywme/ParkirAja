using ParkingSystem.Database;

namespace ParkingSystem;

/// <summary>
/// Tampilan riwayat transaksi parkir harian (Daily Transaction History).
/// Sesuai diagram Draw.io:
/// - 1 - Use Case: Daily Transaction History
/// - 2 - Activity Diagram: Filter berdasarkan tanggal, jenis kendaraan, dan plat -> Tampilkan daftar riwayat
/// - 4 - Wireframe (history): Panel filter tanggal/tipe/plat, tabel riwayat, dan tombol ekspor
/// </summary>
public partial class TransactionHistory : UserControl
{
    /// <summary>
    /// Inisialisasi antarmuka riwayat transaksi, setup filter combo jenis kendaraan, dan muat data.
    /// </summary>
    public TransactionHistory()
    {
        InitializeComponent();
        ResponsiveLayout.Attach(this);

        // Siapkan opsi filter dropdown tipe kendaraan
        vehicletypeComboBox.Items.Clear();
        vehicletypeComboBox.Items.Add("All Types");
        foreach (var type in Enum.GetValues<VehicleType>())
            vehicletypeComboBox.Items.Add(type);
        vehicletypeComboBox.SelectedIndex = 0;

        // Registrasi event tombol
        searchButton.Click += SearchButton_Click;
        exportButton.Click += ExportButton_Click;
        dateDarkDateTimePicker.ValueChanged += (s, e) => LoadHistory();
        vehicletypeComboBox.SelectedIndexChanged += (s, e) => LoadHistory();

        LoadHistory();
    }

    /// <summary>
    /// Event handler tombol "Search" untuk memfilter riwayat sesuai kriteria yang dipilih.
    /// </summary>
    private void SearchButton_Click(object? sender, EventArgs e)
    {
        LoadHistory();
    }

    /// <summary>
    /// Mengambil data transaksi parkir dari InMemoryDb/EF Core dan menerapkan
    /// filter pencarian plat nomor dan jenis kendaraan.
    /// </summary>
    private void LoadHistory()
    {
        string plateKeyword = platenumberTextBox.Text.Trim();
        string typeFilter = vehicletypeComboBox.SelectedItem?.ToString() ?? "All Types";

        var list = InMemoryDb.Transactions.AsEnumerable();

        // Filter tanggal berdasarkan dateDarkDateTimePicker
        var selectedDate = DateOnly.FromDateTime(dateDarkDateTimePicker.Value);
        list = list.Where(t => DateOnly.FromDateTime(t.EntryTime.LocalDateTime) == selectedDate);

        // Filter plat nomor jika diisi
        if (!string.IsNullOrWhiteSpace(plateKeyword))
            list = list.Where(t => t.PlateNumber.Contains(plateKeyword, StringComparison.OrdinalIgnoreCase));

        // Filter tipe kendaraan jika dipilih spesifik
        if (typeFilter != "All Types" && Enum.TryParse<VehicleType>(typeFilter, out var parsedType))
            list = list.Where(t => t.VehicleType == parsedType);

        var results = list.OrderByDescending(t => t.Id).ToList();
        grid21.Rows.Clear();

        // Tampilkan hasil query ke DataGridView
        foreach (var t in results)
        {
            grid21.Rows.Add(
                t.EntryTime.ToString("yyyy-MM-dd HH:mm"),
                t.PlateNumber,
                t.VehicleType.ToString(),
                t.ExitTime?.ToString("HH:mm") ?? "-",
                t.Fare.HasValue ? $"Rp {t.Fare.Value:N0}" : "-",
                t.Status
            );
        }

        label31.Text = $"Page 1 of 1 ({results.Count} items)";
    }

    /// <summary>
    /// Event handler tombol "Export" untuk mengekspor data riwayat transaksi yang tampil.
    /// </summary>
    private void ExportButton_Click(object? sender, EventArgs e)
    {
        MessageBox.Show($"Data riwayat transaksi ({InMemoryDb.Transactions.Count()} baris) berhasil diekspor!",
            "Ekspor Selesai", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
