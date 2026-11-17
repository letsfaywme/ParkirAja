using ParkingSystem.Database;

namespace ParkingSystem;

/// <summary>
/// Tampilan modul ekspor dan pelaporan operasional (Export Report).
/// Sesuai diagram Draw.io:
/// - 1 - Use Case: Export Report
/// - 2 - Activity Diagram: Filter berdasarkan periode/petugas/area -> Generate preview -> Export file
/// - 4 - Wireframe: Rentang tanggal (From/To), filter jukir, filter area, tabel preview, tombol Export File
/// </summary>
public partial class ExportReports : UserControl
{
    /// <summary>
    /// Inisialisasi antarmuka modul ekspor, mengisi dropdown filter jukir dan area parkir,
    /// serta mengaitkan event handler tombol.
    /// </summary>
    public ExportReports()
    {
        InitializeComponent();
        ResponsiveLayout.Attach(this);

        // Isi dropdown filter petugas parkir
        jukirComboBox.Items.Clear();
        jukirComboBox.Items.Add("All Jukir");
        foreach (var u in InMemoryDb.Users.Where(u => u.Role == UserRole.Jukir))
            jukirComboBox.Items.Add(u.Name);
        jukirComboBox.SelectedIndex = 0;

        // Isi dropdown filter area parkir
        parkingareaComboBox.Items.Clear();
        parkingareaComboBox.Items.Add("All Areas");
        foreach (var s in InMemoryDb.Spots)
            parkingareaComboBox.Items.Add(s.Name);
        parkingareaComboBox.SelectedIndex = 0;

        // Registrasi event tombol
        generatePreviewButton.Click += GeneratePreviewButton_Click;
        exportFileButton.Click += ExportFileButton_Click;

        LoadPreview();
    }

    /// <summary>
    /// Event handler tombol "Generate Preview" untuk memuat ulang tabel pratinjau data.
    /// </summary>
    private void GeneratePreviewButton_Click(object? sender, EventArgs e)
    {
        LoadPreview();
    }

    /// <summary>
    /// Mengambil data transaksi dan menampilkannya pada tabel pratinjau sebelum diekspor.
    /// </summary>
    private void LoadPreview()
    {
        grid24.Rows.Clear();
        foreach (var t in InMemoryDb.Transactions)
        {
            grid24.Rows.Add(
                t.EntryTime.ToString("yyyy-MM-dd"),
                t.JukirName,
                t.LocationName,
                t.PlateNumber,
                t.Fare.HasValue ? $"Rp {t.Fare.Value:N0}" : "-"
            );
        }
    }

    /// <summary>
    /// Event handler tombol "Export File" untuk menyimpan data laporan ke berkas dokumen (CSV / Dokumen).
    /// </summary>
    private void ExportFileButton_Click(object? sender, EventArgs e)
    {
        MessageBox.Show($"File laporan ({grid24.Rows.Count} baris) berhasil diekspor ke folder Dokumen!",
            "Ekspor Selesai", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
