using ParkingSystem.Database;

namespace ParkingSystem;

/// <summary>
/// Tampilan modul checkout dan pembayaran kendaraan keluar (Vehicle Exit).
/// Sesuai diagram Draw.io:
/// - 1 - Use Case: Vehicle Exit Log
/// - 2 - Activity Diagram: Find vehicle -> Calculate duration &amp; fare -> Show fare -> Accept payment -> Update status completed
/// - 4 - Wireframe (exit): Form pencarian plat, detail durasi, total tarif besar, tombol proses exit
/// </summary>
public partial class VehicleExit : UserControl, ISessionAware
{
    /// <summary>Sesi jukir aktif yang bertugas</summary>
    public UserSession? CurrentSession { get; private set; }

    /// <inheritdoc />
    public void BindSession(UserSession session)
    {
        CurrentSession = session;
    }

    private ParkingTransaction? _currentTx;
    private decimal _currentFare;

    /// <summary>
    /// Inisialisasi modul Vehicle Exit dan mengaitkan event handler.
    /// </summary>
    public VehicleExit()
    {
        InitializeComponent();
        ResponsiveLayout.Attach(this);

        findButton.Click += FindButton_Click;
        processVehicleExitButton.Click += ProcessVehicleExitButton_Click;
    }

    /// <summary>
    /// Event handler tombol "Find".
    /// Mencari data transaksi aktif berdasarkan nomor plat, menghitung durasi parkir
    /// serta total biaya tagihan yang harus dibayar.
    /// </summary>
    private void FindButton_Click(object? sender, EventArgs e)
    {
        string plateNumber = plateTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(plateNumber))
        {
            MessageBox.Show("Masukkan nomor plat kendaraan!", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var (tx, fare) = InMemoryDb.CalculateExit(plateNumber);
        if (tx == null)
        {
            MessageBox.Show($"Kendaraan dengan plat '{plateNumber}' tidak ditemukan atau sudah keluar.",
                "Tidak Ditemukan", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearDetails();
            return;
        }

        _currentTx = tx;
        _currentFare = fare;

        var duration = DateTimeOffset.Now - tx.EntryTime;
        int hours = Math.Max(1, (int)Math.Ceiling(duration.TotalHours));

        platenumberTextBox.Text = tx.PlateNumber;
        vehicletypeTextBox.Text = tx.VehicleType.ToString();
        parkingareaTextBox.Text = tx.LocationName;
        entrytimeTextBox.Text = tx.EntryTime.ToString("yyyy-MM-dd HH:mm:ss");
        exittimeTextBox.Text = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss");
        durationTextBox.Text = $"{hours} jam ({duration.Hours}j {duration.Minutes}m)";

        label41.Text = $"Rp {fare:N0}";
        label43.Text = $"{hours} jam · {tx.VehicleType}";
        label46.Text = "Status: Siap diproses untuk checkout & pembayaran";
    }

    /// <summary>
    /// Event handler tombol "Process Vehicle Exit".
    /// Memproses transaksi pelunasan, memperbarui status tiket menjadi 'COMPLETED',
    /// mencatat jukir yang melayani, dan menampilkan struk pembayaran.
    /// </summary>
    private void ProcessVehicleExitButton_Click(object? sender, EventArgs e)
    {
        if (_currentTx == null)
        {
            MessageBox.Show("Cari kendaraan yang aktif terlebih dahulu!", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        int jukirId = CurrentSession?.Id ?? 2;
        bool success = InMemoryDb.ProcessExit(_currentTx.Id, jukirId, _currentFare);
        if (success)
        {
            MessageBox.Show($"Pembayaran Berhasil Diproses!\n\nPlat: {_currentTx.PlateNumber}\nTotal Bayar: Rp {_currentFare:N0}\nStatus: LUNAS",
                "Struk Pembayaran Parkir", MessageBoxButtons.OK, MessageBoxIcon.Information);

            ClearDetails();
            plateTextBox.Clear();
            plateTextBox.Focus();
        }
    }

    /// <summary>
    /// Mereset seluruh teks rincian kendaraan dan tarif kembali ke kondisi kosong/default.
    /// </summary>
    private void ClearDetails()
    {
        _currentTx = null;
        _currentFare = 0;

        platenumberTextBox.Clear();
        vehicletypeTextBox.Clear();
        parkingareaTextBox.Clear();
        entrytimeTextBox.Clear();
        exittimeTextBox.Clear();
        durationTextBox.Clear();

        label41.Text = "Rp 0";
        label43.Text = "0 jam";
        label46.Text = "Status: Belum ada kendaraan dipilih";
    }
}
