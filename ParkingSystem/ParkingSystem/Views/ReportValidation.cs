using ParkingSystem.Database;

namespace ParkingSystem;

/// <summary>
/// Tampilan modul validasi dan rekonsiliasi setoran harian jukir oleh Admin.
/// Sesuai diagram Draw.io:
/// - 1 - Use Case: Daily Report Jukir Validation
/// - 2 - Activity Diagram: Display submitted reports -> Calculate discrepancy -> Approve (Matched) or Reject (Need Review)
/// - 4 - Wireframe: Filter status/tanggal, tabel laporan jukir, input catatan review, tombol Approve/Reject
/// </summary>
public partial class ReportValidation : UserControl, ISessionAware
{
    /// <summary>Sesi admin aktif yang melakukan verifikasi</summary>
    public UserSession? CurrentSession { get; private set; }

    /// <inheritdoc />
    public void BindSession(UserSession session)
    {
        CurrentSession = session;
    }
    /// <summary>
    /// Inisialisasi antarmuka validasi setoran, setup combo filter status, dan muat data setoran.
    /// </summary>
    public ReportValidation()
    {
        InitializeComponent();
        ResponsiveLayout.Attach(this);

        // Opsi filter status verifikasi
        statusComboBox.Items.Clear();
        statusComboBox.Items.Add("All Status");
        foreach (var s in Enum.GetValues<DepositStatus>())
            statusComboBox.Items.Add(s);
        statusComboBox.SelectedIndex = 0;

        // Registrasi event tombol
        searchButton.Click += SearchButton_Click;
        approveButton.Click += ApproveButton_Click;
        rejectButton.Click += RejectButton_Click;

        LoadReports();
    }

    /// <summary>
    /// Event handler tombol pencarian / muat ulang daftar setoran jukir.
    /// </summary>
    private void SearchButton_Click(object? sender, EventArgs e)
    {
        LoadReports();
    }

    /// <summary>
    /// Mengambil data seluruh pengajuan setoran harian dan menampilkannya pada DataGridView
    /// beserta nominal yang diharapkan, nominal disetor, dan status saat ini.
    /// </summary>
    private void LoadReports()
    {
        grid17.Rows.Clear();
        string filter = statusComboBox.SelectedItem?.ToString() ?? "All Status";

        var list = InMemoryDb.Deposits.AsEnumerable();
        if (filter != "All Status" && Enum.TryParse<DepositStatus>(filter, out var parsed))
            list = list.Where(d => d.Status == parsed);

        foreach (var d in list)
        {
            int rowIndex = grid17.Rows.Add(
                d.JukirName,
                d.ParkingSpotName,
                $"Rp {d.ExpectedAmount:N0}",
                $"Rp {d.DepositedAmount:N0}",
                d.Status.ToString()
            );
            grid17.Rows[rowIndex].Tag = d; // Simpan referensi objek untuk aksi seleksi
        }
    }

    /// <summary>
    /// Event handler tombol "Approve".
    /// Menyetujui setoran terpilih dan mengubah statusnya menjadi 'Matched'.
    /// Mencatat waktu validasi dan admin yang menyetujui sesuai Activity Diagram Draw.io.
    /// </summary>
    private void ApproveButton_Click(object? sender, EventArgs e)
    {
        if (grid17.CurrentRow?.Tag is not DailyDeposit deposit)
        {
            MessageBox.Show("Pilih salah satu baris setoran jukir!", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        deposit.Status = DepositStatus.Matched;
        deposit.Notes = reviewnoteTextBox.Text.Trim();
        deposit.ValidatedBy = CurrentSession?.Id ?? 1;
        deposit.ValidatedAt = DateTimeOffset.UtcNow;
        InMemoryDb.SaveChanges();

        MessageBox.Show($"Setoran #{deposit.Id} oleh {deposit.JukirName} BERHASIL DISETUJUI (MATCHED)!",
            "Validasi Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);

        reviewnoteTextBox.Clear();
        LoadReports();
    }

    /// <summary>
    /// Event handler tombol "Reject".
    /// Menolak setoran jukir dan menandainya sebagai 'NeedReview' disertai catatan revisi.
    /// Sesuai Activity Diagram Draw.io.
    /// </summary>
    private void RejectButton_Click(object? sender, EventArgs e)
    {
        if (grid17.CurrentRow?.Tag is not DailyDeposit deposit)
        {
            MessageBox.Show("Pilih salah satu baris setoran jukir!", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        deposit.Status = DepositStatus.NeedReview;
        deposit.Notes = reviewnoteTextBox.Text.Trim();
        deposit.ValidatedBy = CurrentSession?.Id ?? 1;
        deposit.ValidatedAt = DateTimeOffset.UtcNow;
        InMemoryDb.SaveChanges();

        MessageBox.Show($"Setoran #{deposit.Id} ditolak dan ditandai BUTUH REVIEW.\nCatatan: {deposit.Notes}",
            "Validasi Ditolak", MessageBoxButtons.OK, MessageBoxIcon.Information);

        reviewnoteTextBox.Clear();
        LoadReports();
    }
}
