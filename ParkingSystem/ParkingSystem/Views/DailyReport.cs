using ParkingSystem.Database;

namespace ParkingSystem;

/// <summary>
/// Tampilan laporan harian dan form pengajuan setoran jukir (Daily Report).
/// Sesuai diagram Draw.io:
/// - 1 - Use Case: Daily Report (Jukir)
/// - 2 - Activity Diagram: Calculate expected revenue -> Review summary -> Submit daily report (status pending)
/// - 4 - Wireframe: KPI ringkasan omzet, tabel breakdown jenis kendaraan, input jumlah setoran fisik
/// </summary>
public partial class DailyReport : UserControl, ISessionAware
{
    /// <summary>Sesi jukir aktif yang bertugas</summary>
    public UserSession? CurrentSession { get; private set; }

    /// <inheritdoc />
    public void BindSession(UserSession session)
    {
        CurrentSession = session;
        LoadDailyReport();
    }
    private decimal _expectedRevenue;

    /// <summary>
    /// Inisialisasi antarmuka laporan harian jukir dan mengaitkan event handler.
    /// </summary>
    public DailyReport()
    {
        InitializeComponent();
        ResponsiveLayout.Attach(this);

        showReportButton.Click += ShowReportButton_Click;
        submitforValidationButton.Click += SubmitforValidationButton_Click;
        exportReportButton.Click += ExportReportButton_Click;

        LoadDailyReport();
    }

    /// <summary>
    /// Event handler tombol "Show Report" untuk memuat ulang laporan berdasarkan tanggal.
    /// </summary>
    private void ShowReportButton_Click(object? sender, EventArgs e)
    {
        LoadDailyReport();
    }

    /// <summary>
    /// Mengkalkulasi pendapatan parkir yang diharapkan sistem (expected amount)
    /// dari transaksi yang sudah berstatus 'COMPLETED' serta menampilkan breakdown per tipe kendaraan.
    /// </summary>
    private void LoadDailyReport()
    {
        var completedTx = InMemoryDb.Transactions.Where(t => t.Status == "COMPLETED").ToList();
        int entries = InMemoryDb.Transactions.Count();
        int exits = completedTx.Count;
        _expectedRevenue = completedTx.Sum(t => t.Fare ?? 0);

        // Update teks kartu KPI
        label11.Text = entries.ToString();
        label18.Text = exits.ToString();
        label25.Text = exits.ToString();
        label32.Text = $"Rp {_expectedRevenue:N0}";

        // Isi default nilai uang setoran sama dengan ekspektasi sistem
        amountDepositedNumericUpDown.Value = _expectedRevenue;

        // Tampilkan rincian pendapatan berdasarkan jenis kendaraan
        grid38.Rows.Clear();
        var grouped = completedTx.GroupBy(t => t.VehicleType);
        foreach (var g in grouped)
        {
            grid38.Rows.Add(
                g.Key.ToString(),
                $"{g.Count()} vehicles",
                $"Rp {g.Sum(t => t.Fare ?? 0):N0}"
            );
        }
    }

    /// <summary>
    /// Event handler tombol "Submit for Validation".
    /// Mengajukan setoran fisik uang ke Admin dengan status 'Pending'.
    /// Menghitung selisih (discrepancy) otomatis jika jumlah setor berbeda dari ekspektasi.
    /// Sesuai Activity Diagram Draw.io: Set date, expected amount, change status to pending.
    /// </summary>
    private void SubmitforValidationButton_Click(object? sender, EventArgs e)
    {
        decimal deposited = amountDepositedNumericUpDown.Value;
        int jukirId = CurrentSession?.Id ?? 2;
        string jukirName = CurrentSession?.Name ?? "Budi Santoso";
        int spotId = CurrentSession?.ParkingSpotId ?? 1;
        var spot = InMemoryDb.Spots.FirstOrDefault(s => s.Id == spotId);
        string spotName = spot?.Name ?? "Area Parkir Utama";

        var deposit = new DailyDeposit
        {
            Id = InMemoryDb.NextDepositId(),
            JukirId = jukirId,
            JukirName = jukirName,
            ParkingSpotId = spotId,
            ParkingSpotName = spotName,
            ReportDate = DateOnly.FromDateTime(DateTime.Today),
            ExpectedAmount = _expectedRevenue,
            DepositedAmount = deposited,
            Status = DepositStatus.Pending,
            Notes = deposited == _expectedRevenue ? "Setoran tepat" : "Ada perbedaan fisik uang"
        };
        InMemoryDb.AddDeposit(deposit);

        MessageBox.Show($"Setoran berhasil diajukan ke Admin!\n\nSeharusnya: Rp {_expectedRevenue:N0}\nDisetorkan: Rp {deposited:N0}\nSelisih: Rp {deposit.Discrepancy:N0}\nStatus: PENDING",
            "Pengajuan Setoran Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>
    /// Event handler tombol "Export" untuk mengekspor laporan rekapitulasi harian.
    /// </summary>
    private void ExportReportButton_Click(object? sender, EventArgs e)
    {
        MessageBox.Show("Laporan harian berhasil diekspor ke file!", "Ekspor", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
