namespace ParkingSystem.Services;

/// <summary>
/// Kontrak layanan inti sistem parkir yang menyediakan operasi bisnis dan akses data asynchronous.
/// Menggantikan fasad statis demi menjamin thread-safety dan decoupling Clean Architecture.
/// </summary>
public interface IParkingService
{
    /// <summary>Memvalidasi kredensial login (username &amp; password) menggunakan hash PBKDF2</summary>
    Task<UserSession?> AuthenticateAsync(string username, string password, CancellationToken ct = default);

    /// <summary>Memeriksa apakah plat kendaraan sedang aktif parkir</summary>
    Task<bool> IsVehicleParkedAsync(string plateNumber, CancellationToken ct = default);

    /// <summary>Mencatat kendaraan masuk dan menerbitkan tiket baru</summary>
    Task<(bool Success, string Message, ParkingTransaction? Tx)> CreateEntryAsync(int spotId, int jukirId, string plateNumber, VehicleType type, CancellationToken ct = default);

    /// <summary>Menghitung durasi dan tagihan biaya keluar kendaraan</summary>
    Task<(ParkingTransaction? Tx, decimal CalculatedFare)> CalculateExitAsync(string plateNumber, CancellationToken ct = default);

    /// <summary>Memproses pembayaran tiket keluar dan menyelesaikan transaksi</summary>
    Task<bool> ProcessExitAsync(int txId, int exitJukirId, decimal fare, CancellationToken ct = default);

    /// <summary>Mengambil ringkasan metrik dashboard untuk jukir tertentu</summary>
    Task<DashboardSummary> GetJukirSummaryAsync(int jukirId, CancellationToken ct = default);

    /// <summary>Mengambil ringkasan metrik dashboard global untuk admin</summary>
    Task<DashboardSummary> GetAdminSummaryAsync(CancellationToken ct = default);

    /// <summary>Mengambil seluruh titik area parkir aktif</summary>
    Task<IReadOnlyList<ParkingSpot>> GetActiveSpotsAsync(CancellationToken ct = default);

    /// <summary>Mengajukan setoran pendapatan harian jukir</summary>
    Task AddDepositAsync(DailyDeposit deposit, CancellationToken ct = default);

    /// <summary>Memvalidasi atau menolak setoran harian oleh admin</summary>
    Task<bool> ValidateDepositAsync(int depositId, int adminId, DepositStatus status, string notes, CancellationToken ct = default);

    /// <summary>Menambahkan titik area parkir baru</summary>
    Task AddSpotAsync(ParkingSpot spot, CancellationToken ct = default);

    /// <summary>Menambahkan akun pengguna baru</summary>
    Task AddUserAsync(AppUser user, CancellationToken ct = default);

    /// <summary>Mengambil seluruh daftar akun pengguna</summary>
    Task<IReadOnlyList<AppUser>> GetUsersAsync(CancellationToken ct = default);

    /// <summary>Mengambil seluruh daftar setoran harian</summary>
    Task<IReadOnlyList<DailyDeposit>> GetDepositsAsync(CancellationToken ct = default);

    /// <summary>Mengambil seluruh daftar transaksi parkir</summary>
    Task<IReadOnlyList<ParkingTransaction>> GetTransactionsAsync(CancellationToken ct = default);
}
