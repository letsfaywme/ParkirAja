using ParkingSystem.Database;

namespace ParkingSystem;

/// <summary>
/// Tampilan form pencatatan kendaraan masuk (Vehicle Entry).
/// Sesuai diagram Draw.io:
/// - 1 - Use Case: Vehicle Entry Log
/// - 2 - Activity Diagram: Input plate &amp; type -> Data validation -> Is vehicle still parked? -> Set entry time -> Print proof
/// - 4 - Wireframe (entry): Form input plat, combo jenis kendaraan, tombol Save/Clear, dan tabel entri terakhir
/// </summary>
/// <remarks>
/// ALUR PRESENTASI (urutan yang disarankan saat demo):
/// 1. Konstruktor      -> menyiapkan dropdown area & jenis kendaraan dari database
/// 2. UpdateRateDisplay -> tarif berubah otomatis saat area/jenis diganti
/// 3. SaveEntryButton_Click -> inti: validasi input lalu simpan tiket
/// 4. InMemoryDb.CreateEntry -> aturan bisnis (ada di file Database/InMemoryDb.cs)
/// 5. LoadLatestEntries -> tabel diperbarui dari database
/// </remarks>
public partial class VehicleEntry : UserControl, ISessionAware, IDemoPage
{
    /// <summary>Sesi jukir aktif yang bertugas</summary>
    public UserSession? CurrentSession { get; private set; }

    /// <summary>ID tiket yang sedang diedit (null = mode tambah tiket baru)</summary>
    private int? _editingTxId;

    /// <inheritdoc />
    /// <remarks>
    /// PRESENTASI: dipanggil sekali oleh MainForm setelah login berhasil.
    /// Yang dilakukan: menyimpan sesi, memilih area awal sesuai area tugas jukir,
    /// lalu membuka kunci dropdown supaya petugas bebas memilih lokasi parkir lain.
    /// </remarks>
    public void BindSession(UserSession session)
    {
        CurrentSession = session;

        // Area parkir dipilih bebas oleh petugas: satu jukir dapat mencatat kendaraan
        // ke area mana pun yang aktif. Area milik sesi hanya dipakai sebagai pilihan awal.
        if (session.ParkingSpotId.HasValue && parkingareaComboBox.Items.Count > 0)
        {
            parkingareaComboBox.SelectedValue = session.ParkingSpotId.Value;
        }

        parkingareaComboBox.Enabled = true;
        UpdateRateDisplay();
    }

    /// <inheritdoc />
    public void OnDemoPageLeft() => ResetEditMode();

    /// <summary>
    /// Inisialisasi form Vehicle Entry, binding enum jenis kendaraan, dan muat data tabel.
    /// </summary>
    /// <remarks>
    /// PRESENTASI: di sini sumber data dropdown ditentukan.
    /// - Jenis kendaraan berasal dari enum VehicleType (Motorcycle / Car).
    /// - Area parkir diambil dari tabel Spots di PostgreSQL, hanya yang IsActive = true.
    ///   Jadi kalau area ditambahkan di database, otomatis muncul di dropdown ini
    ///   tanpa mengubah kode.
    /// </remarks>
    public VehicleEntry()
    {
        InitializeComponent();
        ResponsiveLayout.Attach(this);

        // Binding dropdown jenis kendaraan (Motorcycle / Car)
        vehicletypeComboBox.Items.Clear();
        vehicletypeComboBox.DataSource = Enum.GetValues<VehicleType>();

        // Binding dropdown area parkir dari database (hanya area aktif)
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
    /// <remarks>
    /// PRESENTASI: ini handler utama yang ditunjukkan saat demo.
    /// Tiga tahap penting:
    ///   (1) Ambil input dari form.
    ///   (2) Tentukan mode: edit tiket lama, atau buat tiket baru.
    ///   (3) Panggil lapisan bisnis, lalu tampilkan hasilnya ke pengguna.
    /// Validasi format plat, cek plat ganda, dan cek kapasitas TIDAK di sini,
    /// melainkan di InMemoryDb.CreateEntry supaya aturan bisnis tetap satu tempat.
    /// </remarks>
    private void SaveEntryButton_Click(object? sender, EventArgs e)
    {
        // (1) Ambil seluruh input pengguna.
        //     Trim() membuang spasi tak sengaja di awal/akhir plat.
        string plateNumber = platenumberTextBox.Text.Trim();
        var vehicleType = vehicletypeComboBox.SelectedItem is VehicleType vt ? vt : VehicleType.Motorcycle;
        int spotId = parkingareaComboBox.SelectedValue is int sid ? sid : (CurrentSession?.ParkingSpotId ?? 1);

        // (2a) MODE EDIT: kalau ada tiket yang sedang diedit, perbarui tiket itu
        //      dan langsung keluar (tidak membuat tiket baru).
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

        // (2b) VALIDASI RINGAN DI UI: hanya memastikan plat tidak kosong.
        //      Aturan lain (format, duplikat, kapasitas) ada di lapisan bisnis.
        if (string.IsNullOrWhiteSpace(plateNumber))
        {
            MessageBox.Show("Nomor plat kendaraan wajib diisi!", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // (3) Serahkan ke lapisan bisnis.
        //     InMemoryDb.CreateEntry yang memutuskan tiket diterima atau ditolak,
        //     lalu menuliskannya ke PostgreSQL. Hasilnya tiga nilai:
        //     success = berhasil/gagal, msg = alasan penolakan, tx = tiket yang dibuat.
        int jukirId = CurrentSession?.Id ?? 2;
        var (success, msg, tx) = InMemoryDb.CreateEntry(spotId, jukirId, plateNumber, vehicleType);

        // Kalau ditolak, tampilkan alasannya (format salah / plat ganda / area penuh).
        if (!success || tx == null)
        {
            MessageBox.Show(msg, "Entry Ditolak", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Tiket berhasil dibuat: tampilkan karcis sebagai bukti parkir.
        MessageBox.Show($"Kendaraan Berhasil Dicatat!\n\nNo Tiket: #{tx.Id}\nPlat: {tx.PlateNumber}\nJenis: {tx.VehicleType}\nJam Masuk: {tx.EntryTime:HH:mm:ss}\nTarif Awal: Rp {tx.RateAtEntry:N0}/jam",
            "Karcis Masuk (Bukti Parkir)", MessageBoxButtons.OK, MessageBoxIcon.Information);

        // Bersihkan form dan segarkan tabel supaya tiket baru langsung terlihat.
        ClearButton_Click(null, EventArgs.Empty);
        LoadLatestEntries();

        proofLabel.Text = $"#{tx.Id} · {tx.EntryTime:HH:mm}";
        proofLabel.ForeColor = AppTheme.StatHighlight;
    }

    /// <summary>
    /// Memperbarui label tarif berlaku berdasarkan jenis kendaraan dan area yang dipilih.
    /// </summary>
    /// <remarks>
    /// PRESENTASI: contoh bahwa tarif tidak di-hardcode.
    /// Nilainya dibaca dari kolom MotorcycleRate / CarRate milik area yang dipilih,
    /// sehingga tarif tiap area bisa berbeda (misal Basement lebih mahal dari Timur).
    /// Demo cepat: ganti dropdown jenis kendaraan, label tarif langsung berubah.
    /// </remarks>
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
    /// <remarks>
    /// PRESENTASI: bukti data benar-benar tersimpan.
    /// Tabel ini membaca ulang dari PostgreSQL, bukan dari variabel di memori.
    /// Kolom "Area" diambil dari transaksi, sehingga lokasi parkir tiap kendaraan terlihat.
    /// Objek transaksi disimpan di Tag setiap baris agar tombol Edit/Delete tahu
    /// baris mana yang dipilih.
    /// </remarks>
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
    /// <remarks>
    /// PRESENTASI: hanya tiket berstatus ACTIVE yang boleh diedit.
    /// Data tiket disalin ke form, lalu tombol Save berubah menjadi "Update Tiket".
    /// </remarks>
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
        parkingareaComboBox.SelectedValue = tx.ParkingSpotId;
        saveEntryButton.Text = "Update Tiket";
        label4.Text = $"Edit tiket #{tx.Id}";
        UpdateRateDisplay();
    }

    /// <summary>Membatalkan tiket ACTIVE yang dipilih pada tabel, setelah konfirmasi pengguna.</summary>
    /// <remarks>
    /// PRESENTASI: pembatalan mengurangi lagi counter Occupied pada area terkait,
    /// sehingga slot yang tadinya terpakai menjadi tersedia kembali.
    /// </remarks>
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
