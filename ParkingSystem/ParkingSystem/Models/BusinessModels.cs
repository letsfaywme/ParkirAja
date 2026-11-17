namespace ParkingSystem;

/// <summary>
/// Peran hak akses pengguna dalam sistem parkir.
/// Sesuai Diagram Draw.io (1 - Use Case &amp; 3 - ERD):
/// - Admin: Mengelola dashboard global, area parkir, akun jukir, validasi setoran, ekspor laporan.
/// - Jukir: Mengelola entry kendaraan, exit/pembayaran, riwayat harian, pengajuan setoran harian.
/// </summary>
public enum UserRole { Admin, Jukir }

/// <summary>
/// Jenis kendaraan yang didukung sistem parkir.
/// Sesuai ERD Draw.io enum ('motorcycle', 'car').
/// </summary>
public enum VehicleType { Motorcycle, Car }

/// <summary>
/// Status verifikasi setoran harian oleh Admin.
/// Sesuai ERD Draw.io verificationStatus enum ('pending', 'matched', 'need_review').
/// </summary>
public enum DepositStatus { Pending, Matched, NeedReview }

/// <summary>
/// Sesi pengguna aktif saat login untuk membatasi navigasi dan akses data.
/// </summary>
/// <param name="Id">ID pengguna di database</param>
/// <param name="Username">Username untuk login</param>
/// <param name="Name">Nama lengkap pengguna</param>
/// <param name="Role">Peran pengguna (Admin atau Jukir)</param>
/// <param name="ParkingSpotId">ID area tugas (khusus jukir, null jika admin)</param>
public sealed record UserSession(int Id, string Username, string Name, UserRole Role, int? ParkingSpotId);

/// <summary>
/// Entitas akun pengguna (Admin &amp; Jukir).
/// Merepresentasikan tabel 'Admins' dan 'Jukir' di ERD Draw.io.
/// </summary>
public class AppUser
{
    /// <summary>Primary Key ID pengguna</summary>
    public int Id { get; set; }

    /// <summary>Username unik untuk autentikasi</summary>
    public string Username { get; set; } = "";

    /// <summary>Hash kata sandi akun (disimpan menggunakan PBKDF2 bergaram)</summary>
    public string PasswordHash { get; set; } = "";

    /// <summary>Nama lengkap pengguna/petugas</summary>
    public string Name { get; set; } = "";

    /// <summary>Peran hak akses (Admin / Jukir)</summary>
    public UserRole Role { get; set; }

    /// <summary>Foreign Key titik area parkir tempat jukir ditugaskan (null untuk admin)</summary>
    public int? ParkingSpotId { get; set; }

    /// <summary>Status keaktifan akun (true = aktif, false = dinonaktifkan)</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Relasi navigasi ke area parkir tempat jukir bertugas</summary>
    public virtual ParkingSpot? ParkingSpot { get; set; }

    /// <summary>Koleksi transaksi parkir masuk yang dicatat oleh pengguna ini</summary>
    public virtual ICollection<ParkingTransaction> EntryTransactions { get; set; } = new List<ParkingTransaction>();

    /// <summary>Koleksi transaksi parkir keluar yang diproses checkout-nya oleh pengguna ini</summary>
    public virtual ICollection<ParkingTransaction> ExitTransactions { get; set; } = new List<ParkingTransaction>();

    /// <summary>Koleksi pengajuan setoran harian yang diajukan oleh pengguna ini</summary>
    public virtual ICollection<DailyDeposit> Deposits { get; set; } = new List<DailyDeposit>();

    /// <summary>Koleksi validasi setoran yang ditinjau oleh admin ini</summary>
    public virtual ICollection<DailyDeposit> ValidatedDeposits { get; set; } = new List<DailyDeposit>();
}

/// <summary>
/// Entitas titik/lokasi area parkir.
/// Merepresentasikan tabel 'Parking Spots' di ERD Draw.io.
/// </summary>
public class ParkingSpot
{
    /// <summary>Primary Key ID area parkir</summary>
    public int Id { get; set; }

    /// <summary>Nama lokasi area parkir (misal: Area Parkir Utama)</summary>
    public string Name { get; set; } = "";

    /// <summary>Alamat lengkap lokasi parkir</summary>
    public string Address { get; set; } = "";

    /// <summary>Tarif parkir motor per jam (numeric 12,2)</summary>
    public decimal MotorcycleRate { get; set; }

    /// <summary>Tarif parkir mobil per jam (numeric 12,2)</summary>
    public decimal CarRate { get; set; }

    /// <summary>Kapasitas daya tampung maksimal kendaraan</summary>
    public int Capacity { get; set; }

    /// <summary>Status operasional area parkir</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Jumlah slot kendaraan yang sedang terisi saat ini</summary>
    public int Occupied { get; set; }

    /// <summary>Koleksi jukir yang ditugaskan pada area ini</summary>
    public virtual ICollection<AppUser> Jukirs { get; set; } = new List<AppUser>();

    /// <summary>Koleksi transaksi parkir pada area ini</summary>
    public virtual ICollection<ParkingTransaction> Transactions { get; set; } = new List<ParkingTransaction>();

    /// <summary>Koleksi setoran harian untuk area ini</summary>
    public virtual ICollection<DailyDeposit> Deposits { get; set; } = new List<DailyDeposit>();
}

/// <summary>
/// Entitas transaksi parkir kendaraan.
/// Merepresentasikan tabel 'Parking Transaction' di ERD Draw.io.
/// </summary>
public class ParkingTransaction
{
    /// <summary>Primary Key ID transaksi / nomor tiket</summary>
    public int Id { get; set; }

    /// <summary>Foreign Key ke area parkir tempat kendaraan masuk</summary>
    public int ParkingSpotId { get; set; }

    /// <summary>Nama lokasi parkir saat kendaraan masuk (denormalisasi untuk display cepat)</summary>
    public string LocationName { get; set; } = "";

    /// <summary>Foreign Key jukir yang mencatat kendaraan masuk</summary>
    public int JukirId { get; set; }

    /// <summary>Nama jukir yang mencatat tiket masuk</summary>
    public string JukirName { get; set; } = "";

    /// <summary>Nomor plat kendaraan (misal: B 1234 ABC)</summary>
    public string PlateNumber { get; set; } = "";

    /// <summary>Jenis kendaraan (Motorcycle / Car)</summary>
    public VehicleType VehicleType { get; set; }

    /// <summary>Waktu saat kendaraan masuk</summary>
    public DateTimeOffset EntryTime { get; set; }

    /// <summary>Waktu saat kendaraan keluar (null jika masih parkir/aktif)</summary>
    public DateTimeOffset? ExitTime { get; set; }

    /// <summary>Tarif per jam yang berlaku saat kendaraan masuk</summary>
    public decimal RateAtEntry { get; set; }

    /// <summary>Total biaya parkir yang dibayar saat checkout (null jika masih aktif)</summary>
    public decimal? Fare { get; set; }

    /// <summary>Status transaksi: 'ACTIVE' (masih parkir) atau 'COMPLETED' (sudah keluar)</summary>
    public string Status { get; set; } = "ACTIVE";

    /// <summary>Foreign Key jukir yang memproses pembayaran saat kendaraan keluar</summary>
    public int? ExitJukirId { get; set; }

    /// <summary>Nama jukir yang memproses kendaraan keluar</summary>
    public string? ExitJukirName { get; set; }

    /// <summary>Relasi navigasi ke area parkir tempat kendaraan masuk</summary>
    public virtual ParkingSpot ParkingSpot { get; set; } = null!;

    /// <summary>Relasi navigasi ke petugas jukir yang mencatat tiket masuk</summary>
    public virtual AppUser Jukir { get; set; } = null!;

    /// <summary>Relasi navigasi ke petugas jukir yang memproses kendaraan keluar</summary>
    public virtual AppUser? ExitJukir { get; set; }
}

/// <summary>
/// Entitas laporan dan setoran harian juru parkir ke admin.
/// Merepresentasikan tabel 'Deposits' di ERD Draw.io.
/// </summary>
public class DailyDeposit
{
    /// <summary>Primary Key ID setoran</summary>
    public int Id { get; set; }

    /// <summary>Foreign Key jukir yang menyetorkan uang</summary>
    public int JukirId { get; set; }

    /// <summary>Nama jukir penyetor</summary>
    public string JukirName { get; set; } = "";

    /// <summary>Foreign Key area parkir terkait setoran</summary>
    public int ParkingSpotId { get; set; }

    /// <summary>Nama area parkir terkait</summary>
    public string ParkingSpotName { get; set; } = "";

    /// <summary>Tanggal laporan setoran</summary>
    public DateOnly ReportDate { get; set; }

    /// <summary>Jumlah pendapatan yang seharusnya disetor berdasarkan catatan sistem (amountExpected)</summary>
    public decimal ExpectedAmount { get; set; }

    /// <summary>Jumlah uang fisik yang disetorkan oleh jukir (amountDeposited)</summary>
    public decimal DepositedAmount { get; set; }

    /// <summary>
    /// Selisih antara uang yang disetor dengan yang diharapkan sistem (discrepancy).
    /// Dihitung otomatis: DepositedAmount - ExpectedAmount.
    /// </summary>
    public decimal Discrepancy => DepositedAmount - ExpectedAmount;

    /// <summary>Status verifikasi oleh admin (Pending, Matched, NeedReview)</summary>
    public DepositStatus Status { get; set; } = DepositStatus.Pending;

    /// <summary>Catatan peninjauan atau alasan revisi</summary>
    public string Notes { get; set; } = "";

    /// <summary>Foreign Key admin yang memvalidasi setoran (adminId)</summary>
    public int? ValidatedBy { get; set; }

    /// <summary>Waktu saat admin memvalidasi setoran</summary>
    public DateTimeOffset? ValidatedAt { get; set; }

    /// <summary>Relasi navigasi ke jukir yang mengajukan setoran</summary>
    public virtual AppUser Jukir { get; set; } = null!;

    /// <summary>Relasi navigasi ke area parkir terkait setoran</summary>
    public virtual ParkingSpot ParkingSpot { get; set; } = null!;

    /// <summary>Relasi navigasi ke admin yang memvalidasi setoran</summary>
    public virtual AppUser? Validator { get; set; }
}

/// <summary>
/// Record pembungkus pagination untuk riwayat transaksi.
/// </summary>
public sealed record HistoryPage(IReadOnlyList<ParkingTransaction> Rows, int Total, int Page);

/// <summary>
/// Record ringkasan KPI dashboard (total masuk, keluar, pendapatan, transaksi terakhir, daftar area).
/// Digunakan oleh JukirDashboard dan AdminDashboard.
/// </summary>
public sealed record DashboardSummary(int Entries, int Exits, decimal Revenue, IReadOnlyList<ParkingTransaction> Latest, IReadOnlyList<ParkingSpot> Spots);
