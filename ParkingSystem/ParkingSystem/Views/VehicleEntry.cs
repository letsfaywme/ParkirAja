using ParkingSystem.Database;

namespace ParkingSystem;

/// <summary>
/// Tampilan form pencatatan kendaraan masuk (Vehicle Entry).
/// Sesuai diagram Draw.io:
/// - 1 - Use Case: Vehicle Entry Log
/// - 2 - Activity Diagram: Input plate &amp; type -> Data validation -> Is vehicle still parked? -> Set entry time -> Print proof
/// - 4 - Wireframe (entry): Form input plat, combo jenis kendaraan, tombol Save/Clear, dan tabel entri terakhir
/// </summary>
public partial class VehicleEntry : UserControl, ISessionAware, IDemoPage
{
    /// <summary>Sesi jukir aktif yang bertugas</summary>
    public UserSession? CurrentSession { get; private set; }

    /// <summary>ID tiket yang sedang diedit (null = mode tambah tiket baru)</summary>
    private int? _editingTxId;

    /// <inheritdoc />
    public void BindSession(UserSession session)
    {
        CurrentSession = session;
        if (session.ParkingSpotId.HasValue && parkingareaComboBox.Items.Count > 0)
        {
            parkingareaComboBox.SelectedValue = session.ParkingSpotId.Value;
            parkingareaComboBox.Enabled = false;
        }
        UpdateRateDisplay();
    }

    /// <inheritdoc />
    public void OnDemoPageLeft() => ResetEditMode();
    /// <summary>
    /// Inisialisasi form Vehicle Entry, binding enum jenis kendaraan, dan muat data tabel.
    /// </summary>
    public VehicleEntry()
    {
        InitializeComponent();
        ResponsiveLayout.Attach(this);

        // Binding dropdown jenis kendaraan (Motorcycle / Car)
        vehicletypeComboBox.Items.Clear();
        vehicletypeComboBox.DataSource = Enum.GetValues<VehicleType>();

        // Binding dropdown area parkir dari database
        parkingareaComboBox.DataSource = InMemoryDb.Spots.Where(s => s.IsActive).ToList();
        parkingareaComboBox.DisplayMember = "Name";
        parkingareaComboBox.ValueMember = "Id";
        parkingareaComboBox.SelectedIndexChanged += (s, e) => UpdateRateDisplay();
        vehicletypeComboBox.SelectedIndexChanged += (s, e) => UpdateRateDisplay();
        UpdateRateDisplay();

        // Registrasi event klik
        saveEntryButton.Click += SaveEntryButton_Click;
        clearButton.Click += ClearButton_Click;
        editEntryButton.Click += EditEntryButton_Click;
        deleteEntryButton.Click += DeleteEntryButton_Click;

        LoadLatestEntries();
    }

    /// <summary>
    /// Event handler tombol "Save Entry".
    /// Memvalidasi kelengkapan plat nomor, memastikan kendaraan tidak sedang parkir,
    /// menerbitkan tiket masuk di database, dan mencetak bukti karcis masuk.
    /// </summary>
    private void SaveEntryButton_Click(object? sender, EventArgs e)
    {
        string plateNumber = platenumberTextBox.Text.Trim();
        var vehicleType = vehicletypeComboBox.SelectedItem is VehicleType vt ? vt : VehicleType.Motorcycle;
        int spotId = parkingareaComboBox.SelectedValue is int sid ? sid : (CurrentSession?.ParkingSpotId ?? 1);

        if (_editingTxId is int editId)
        {
            var (updated, updateMsg) = InMemoryDb.UpdateEntry(editId, plateNumber, vehicleType, spotId);
            if (!updated)
            {
                MessageBox.Show(updateMsg, "Update Ditolak", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show($"Tiket #{editId} berhasil diperbarui.", "Update Berhasil", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ResetEditMode();
            LoadLatestEntries();
            return;
        }

        if (string.IsNullOrWhiteSpace(plateNumber))
        {
            MessageBox.Show("Nomor plat kendaraan wajib diisi!", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        int jukirId = CurrentSession?.Id ?? 2;
        var (success, msg, tx) = InMemoryDb.CreateEntry(spotId, jukirId, plateNumber, vehicleType);

        if (!success || tx == null)
        {
            MessageBox.Show(msg, "Entry Ditolak", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        MessageBox.Show($"Kendaraan Berhasil Dicatat!\n\nNo Tiket: #{tx.Id}\nPlat: {tx.PlateNumber}\nJenis: {tx.VehicleType}\nJam Masuk: {tx.EntryTime:HH:mm:ss}\nTarif Awal: Rp {tx.RateAtEntry:N0}/jam",
            "Karcis Masuk (Bukti Parkir)", MessageBoxButtons.OK, MessageBoxIcon.Information);

        ClearButton_Click(null, EventArgs.Empty);
        LoadLatestEntries();

        proofLabel.Text = $"#{tx.Id} · {tx.EntryTime:HH:mm}";
        proofLabel.ForeColor = AppTheme.StatHighlight;
    }

    /// <summary>
    /// Memperbarui label tarif berlaku berdasarkan jenis kendaraan dan area yang dipilih.
    /// </summary>
    private void UpdateRateDisplay()
    {
        if (parkingareaComboBox.SelectedItem is ParkingSpot spot)
        {
            var vt = vehicletypeComboBox.SelectedItem is VehicleType v ? v : VehicleType.Motorcycle;
            decimal rate = vt == VehicleType.Motorcycle ? spot.MotorcycleRate : spot.CarRate;
            rateLabel.Text = $"Rp {rate:N0}/jam";
        }
    }

    /// <summary>
    /// Event handler tombol "Clear" untuk mereset seluruh field input ke kondisi awal.
    /// </summary>
    private void ClearButton_Click(object? sender, EventArgs e)
    {
        platenumberTextBox.Clear();
        vehicletypeComboBox.SelectedIndex = 0;
        ResetEditMode();
        platenumberTextBox.Focus();
    }

    /// <summary>
    /// Memuat daftar 10 kendaraan terakhir yang masuk ke tabel DataGridView.
    /// </summary>
    private void LoadLatestEntries()
    {
        grid22.Rows.Clear();
        foreach (var t in InMemoryDb.Transactions.OrderByDescending(t => t.Id).Take(10).ToList())
        {
            int idx = grid22.Rows.Add(
                t.EntryTime.ToString("HH:mm:ss"),
                t.PlateNumber,
                t.VehicleType.ToString(),
                t.LocationName,
                t.Status
            );
            grid22.Rows[idx].Tag = t;
        }
    }

    /// <summary>Mengaktifkan mode edit untuk tiket yang dipilih pada tabel.</summary>
    private void EditEntryButton_Click(object? sender, EventArgs e)
    {
        if (grid22.CurrentRow?.Tag is not ParkingTransaction tx || tx.Status != "ACTIVE")
        {
            MessageBox.Show("Pilih tiket berstatus ACTIVE dari tabel!", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _editingTxId = tx.Id;
        platenumberTextBox.Text = tx.PlateNumber;
        vehicletypeComboBox.SelectedItem = tx.VehicleType;
        if (parkingareaComboBox.Enabled) parkingareaComboBox.SelectedValue = tx.ParkingSpotId;
        saveEntryButton.Text = "Update Tiket";
        label4.Text = $"Edit tiket #{tx.Id}";
        UpdateRateDisplay();
    }

    /// <summary>Membatalkan tiket ACTIVE yang dipilih pada tabel, setelah konfirmasi pengguna.</summary>
    private void DeleteEntryButton_Click(object? sender, EventArgs e)
    {
        if (grid22.CurrentRow?.Tag is not ParkingTransaction tx || tx.Status != "ACTIVE")
        {
            MessageBox.Show("Pilih tiket berstatus ACTIVE dari tabel!", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var confirm = MessageBox.Show($"Batalkan tiket #{tx.Id} untuk plat {tx.PlateNumber}?",
            "Konfirmasi", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        var (success, msg) = InMemoryDb.DeleteEntry(tx.Id);
        if (success)
        {
            MessageBox.Show(msg, "Tiket Dibatalkan", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ResetEditMode();
            LoadLatestEntries();
        }
        else
        {
            MessageBox.Show(msg, "Gagal Membatalkan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            LoadLatestEntries();
        }
    }

    /// <summary>Mengembalikan form ke mode tambah tiket baru.</summary>
    private void ResetEditMode()
    {
        _editingTxId = null;
        saveEntryButton.Text = "Save Entry";
        label4.Text = "Register vehicle";
        proofLabel.Text = "Belum ada";
        proofLabel.ForeColor = AppTheme.TextMuted;
    }
}
