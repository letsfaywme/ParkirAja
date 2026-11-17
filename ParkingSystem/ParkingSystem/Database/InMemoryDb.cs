using System.Text.RegularExpressions;
using ParkingSystem.Security;
namespace ParkingSystem.Database;

/// <summary>
/// Lapisan repositori fasad in-memory yang membungkus ParkingDbContext EF Core.
/// Menyediakan query LINQ dan method mutasi data bisnis yang dipanggil oleh antarmuka Views.
/// </summary>
public static class InMemoryDb
{
    /// <summary>Akses ke singleton ParkingDbContext</summary>
    private static ParkingDbContext Db => ParkingDbContext.Instance;

    /// <summary>Queryable untuk seluruh akun pengguna</summary>
    public static IQueryable<AppUser> Users => Db.Users;

    /// <summary>Queryable untuk seluruh titik area parkir</summary>
    public static IQueryable<ParkingSpot> Spots => Db.Spots;

    /// <summary>Queryable untuk seluruh transaksi tiket parkir</summary>
    public static IQueryable<ParkingTransaction> Transactions => Db.Transactions;

    /// <summary>Queryable untuk seluruh data pengajuan setoran harian</summary>
    public static IQueryable<DailyDeposit> Deposits => Db.Deposits;

    /// <summary>
    /// Memvalidasi kredensial login (username &amp; password).
    /// Sesuai alur Login di Draw.io Activity Diagram.
    /// </summary>
    public static UserSession? Authenticate(string username, string password)
    {
        string normUser = username.Trim().ToLower();
        var user = Db.Users.FirstOrDefault(u => u.Username.ToLower() == normUser);

        if (user == null || !user.IsActive) return null;
        if (!PasswordHasher.VerifyPassword(password, user.PasswordHash)) return null;

        return new UserSession(user.Id, user.Username, user.Name, user.Role, user.ParkingSpotId);
    }

    /// <summary>
    /// Memeriksa apakah suatu nomor plat masih berstatus parkir aktif di sistem.
    /// Sesuai diagram alur Draw.io keputusan: "Is the vehicle still parked?".
    /// </summary>
    public static bool IsVehicleParked(string plateNumber)
    {
        string normalized = plateNumber.Trim().ToUpperInvariant();
        return Db.Transactions.Any(t => t.PlateNumber == normalized && t.Status == "ACTIVE");
    }

    /// <summary>
    /// Mencatat kendaraan masuk dan menerbitkan tiket parkir baru.
    /// Memvalidasi:
    /// 1. Nomor plat tidak boleh sedang aktif parkir.
    /// 2. Kapasitas area parkir tidak boleh terlampaui.
    /// Sesuai diagram Draw.io alur: "Vehicle Entry Log".
    /// </summary>
    /// <param name="spotId">ID area parkir</param>
    /// <param name="jukirId">ID petugas jukir yang bertugas</param>
    /// <param name="plateNumber">Nomor plat kendaraan</param>
    /// <param name="type">Jenis kendaraan (Motorcycle/Car)</param>
    /// <returns>Tuple berisi status keberhasilan, pesan notifikasi, dan objek transaksi</returns>
    public static (bool Success, string Message, ParkingTransaction? Tx) CreateEntry(int spotId, int jukirId, string plateNumber, VehicleType type)
    {
        string normalizedPlate = plateNumber.Trim().ToUpperInvariant();

        // Validasi format plat nomor Indonesia standar (misal: B 1234 ABC, D 99 X, AB 1 A)
        if (!Regex.IsMatch(normalizedPlate, @"^[A-Z]{1,2}\s\d{1,4}\s[A-Z]{1,3}$"))
        {
            return (false, $"Format nomor plat '{normalizedPlate}' tidak valid. Contoh format standar: 'B 1234 ABC'.", null);
        }

        // Cek duplikasi kendaraan yang belum checkout
        if (IsVehicleParked(normalizedPlate))
        {
            return (false, $"Kendaraan dengan plat {normalizedPlate} masih tercatat aktif di area parkir!", null);
        }

        var spot = Db.Spots.FirstOrDefault(s => s.Id == spotId) ?? Db.Spots.First();

        // Cek daya tampung area
        if (spot.Occupied >= spot.Capacity)
        {
            return (false, $"Area parkir '{spot.Name}' sudah penuh ({spot.Occupied}/{spot.Capacity})!", null);
        }

        var jukir = Db.Users.FirstOrDefault(u => u.Id == jukirId);
        decimal rate = type == VehicleType.Motorcycle ? spot.MotorcycleRate : spot.CarRate;

        // Auto-increment ID transaksi berikutnya
        int nextId = (Db.Transactions.Max(t => (int?)t.Id) ?? 0) + 1;

        var tx = new ParkingTransaction
        {
            Id = nextId,
            ParkingSpotId = spot.Id,
            LocationName = spot.Name,
            JukirId = jukirId,
            JukirName = jukir?.Name ?? "Jukir",
            PlateNumber = normalizedPlate,
            VehicleType = type,
            EntryTime = DateTimeOffset.Now,
            RateAtEntry = rate,
            Status = "ACTIVE"
        };

        Db.Transactions.Add(tx);
        spot.Occupied++; // Tambah counter kendaraan terisi
        Db.SaveChanges();

        return (true, "Tiket berhasil dibuat.", tx);
    }

    /// <summary>
    /// Memperbarui data tiket masuk yang masih berstatus ACTIVE
    /// (plat nomor, jenis kendaraan, dan area parkir) lalu menyimpan perubahan.
    /// </summary>
    public static (bool Success, string Message) UpdateEntry(int txId, string plateNumber, VehicleType type, int spotId)
    {
        string normalizedPlate = plateNumber.Trim().ToUpperInvariant();

        if (!Regex.IsMatch(normalizedPlate, @"^[A-Z]{1,2}\s\d{1,4}\s[A-Z]{1,3}$"))
        {
            return (false, $"Format nomor plat '{normalizedPlate}' tidak valid. Contoh format standar: 'B 1234 ABC'.");
        }

        var tx = Db.Transactions.FirstOrDefault(t => t.Id == txId);
        if (tx == null || tx.Status != "ACTIVE")
        {
            return (false, "Tiket tidak ditemukan atau sudah selesai.");
        }

        if (tx.PlateNumber != normalizedPlate && IsVehicleParked(normalizedPlate))
        {
            return (false, $"Kendaraan dengan plat {normalizedPlate} masih tercatat aktif di area parkir!");
        }

        var targetSpot = Db.Spots.FirstOrDefault(s => s.Id == spotId);
        if (targetSpot == null)
        {
            return (false, "Area parkir tidak ditemukan.");
        }

        if (targetSpot.Id != tx.ParkingSpotId && targetSpot.Occupied >= targetSpot.Capacity)
        {
            return (false, $"Area parkir '{targetSpot.Name}' sudah penuh ({targetSpot.Occupied}/{targetSpot.Capacity})!");
        }

        if (targetSpot.Id != tx.ParkingSpotId)
        {
            var oldSpot = Db.Spots.FirstOrDefault(s => s.Id == tx.ParkingSpotId);
            if (oldSpot != null && oldSpot.Occupied > 0) oldSpot.Occupied--;
            targetSpot.Occupied++;
        }

        tx.ParkingSpotId = targetSpot.Id;
        tx.LocationName = targetSpot.Name;
        tx.PlateNumber = normalizedPlate;
        tx.VehicleType = type;
        tx.RateAtEntry = type == VehicleType.Motorcycle ? targetSpot.MotorcycleRate : targetSpot.CarRate;

        Db.SaveChanges();
        return (true, "Tiket berhasil diperbarui.");
    }

    /// <summary>
    /// Membatalkan (menghapus) tiket masuk yang masih berstatus ACTIVE
    /// dan melepaskan slot pada area parkir terkait.
    /// </summary>
    public static (bool Success, string Message) DeleteEntry(int txId)
    {
        var tx = Db.Transactions.FirstOrDefault(t => t.Id == txId);
        if (tx == null || tx.Status != "ACTIVE")
        {
            return (false, "Tiket tidak ditemukan atau sudah selesai.");
        }

        var spot = Db.Spots.FirstOrDefault(s => s.Id == tx.ParkingSpotId);
        if (spot != null && spot.Occupied > 0) spot.Occupied--;

        Db.Transactions.Remove(tx);
        Db.SaveChanges();
        return (true, "Tiket berhasil dibatalkan.");
    }

    /// <summary>
    /// Menghitung durasi parkir dan biaya tagihan saat kendaraan ingin keluar.
    /// Durasi dibulatkan ke atas per jam penuh (minimal 1 jam) dikalikan tarif masuk.
    /// Sesuai diagram Draw.io alur: "Calculate parking duration and vehicle fare".
    /// </summary>
    /// <param name="plateNumber">Nomor plat kendaraan yang dicari</param>
    /// <returns>Transaksi aktif yang ditemukan dan estimasi total biaya</returns>
    public static (ParkingTransaction? Tx, decimal CalculatedFare) CalculateExit(string plateNumber)
    {
        string normalized = plateNumber.Trim().ToUpperInvariant();
        var tx = Db.Transactions.FirstOrDefault(t => t.PlateNumber == normalized && t.Status == "ACTIVE");
        if (tx == null) return (null, 0);

        var duration = DateTimeOffset.Now - tx.EntryTime;
        var hours = Math.Max(1, (int)Math.Ceiling(duration.TotalHours));
        decimal fare = tx.RateAtEntry * hours;
        return (tx, fare);
    }

    /// <summary>
    /// Menyelesaikan proses pembayaran kendaraan keluar (checkout).
    /// Mengubah status menjadi 'COMPLETED', mencatat waktu keluar, dan mengurangi kapasitas area.
    /// Sesuai diagram Draw.io alur: "Update status to completed and save transaction".
    /// </summary>
    /// <param name="txId">ID transaksi parkir</param>
    /// <param name="exitJukirId">ID jukir yang menerima pembayaran keluar</param>
    /// <param name="fare">Jumlah biaya yang dibayarkan</param>
    /// <returns>True jika transaksi berhasil diselesaikan</returns>
    public static bool ProcessExit(int txId, int exitJukirId, decimal fare)
    {
        var tx = Db.Transactions.FirstOrDefault(t => t.Id == txId && t.Status == "ACTIVE");
        if (tx == null) return false;

        var jukir = Db.Users.FirstOrDefault(u => u.Id == exitJukirId);
        tx.ExitTime = DateTimeOffset.Now;
        tx.Fare = fare;
        tx.Status = "COMPLETED";
        tx.ExitJukirId = exitJukirId;
        tx.ExitJukirName = jukir?.Name ?? "Jukir";

        var spot = Db.Spots.FirstOrDefault(s => s.Id == tx.ParkingSpotId);
        if (spot != null && spot.Occupied > 0)
            spot.Occupied--; // Kurangi counter kendaraan terisi

        Db.SaveChanges();
        return true;
    }

    /// <summary>
    /// Mengambil ringkasan statistik harian untuk jukir tertentu.
    /// Meliputi kendaraan masuk, kendaraan keluar, total omzet, dan 10 transaksi terakhir.
    /// </summary>
    public static DashboardSummary GetJukirSummary(int jukirId)
    {
        var today = DateTimeOffset.Now.Date;
        var allTx = Db.Transactions.ToList();
        var myTx = allTx.Where(t =>
            (t.JukirId == jukirId || t.ExitJukirId == jukirId) &&
            t.EntryTime.Date == today).ToList();

        int entries = myTx.Count(t => t.JukirId == jukirId);
        int exits = myTx.Count(t => t.ExitJukirId == jukirId && t.Status == "COMPLETED");
        decimal revenue = myTx.Where(t => t.ExitJukirId == jukirId && t.Status == "COMPLETED").Sum(t => t.Fare ?? 0);

        return new DashboardSummary(entries, exits, revenue, allTx.OrderByDescending(t => t.Id).Take(10).ToList(), Db.Spots.ToList());
    }

    /// <summary>
    /// Mengambil ringkasan statistik real-time global untuk Admin.
    /// Sesuai diagram Draw.io alur: "Calculate parking statistics and current revenue".
    /// </summary>
    public static DashboardSummary GetAdminSummary()
    {
        var today = DateTimeOffset.Now.Date;
        var allTx = Db.Transactions.ToList();
        var todayTx = allTx.Where(t => t.EntryTime.Date == today).ToList();

        int entries = todayTx.Count;
        int exits = todayTx.Count(t => t.Status == "COMPLETED");
        decimal revenue = todayTx.Where(t => t.Status == "COMPLETED").Sum(t => t.Fare ?? 0);

        return new DashboardSummary(entries, exits, revenue, allTx.OrderByDescending(t => t.Id).Take(10).ToList(), Db.Spots.ToList());
    }

    /// <summary>Menambahkan area parkir baru ke database</summary>
    public static void AddSpot(ParkingSpot spot)
    {
        Db.Spots.Add(spot);
        Db.SaveChanges();
    }

    /// <summary>Menambahkan akun jukir/admin baru ke database</summary>
    public static void AddUser(AppUser user)
    {
        Db.Users.Add(user);
        Db.SaveChanges();
    }

    /// <summary>Menyimpan pengajuan setoran harian baru ke database</summary>
    public static void AddDeposit(DailyDeposit deposit)
    {
        Db.Deposits.Add(deposit);
        Db.SaveChanges();
    }

    /// <summary>Menyimpan seluruh perubahan entitas yang sedang dilacak ke database</summary>
    public static void SaveChanges() => Db.SaveChanges();

    /// <summary>Menghasilkan ID berikutnya untuk area parkir baru</summary>
    public static int NextSpotId() => (Db.Spots.Max(s => (int?)s.Id) ?? 0) + 1;

    /// <summary>Menghasilkan ID berikutnya untuk akun pengguna baru</summary>
    public static int NextUserId() => (Db.Users.Max(u => (int?)u.Id) ?? 0) + 1;

    /// <summary>Menghasilkan ID berikutnya untuk data setoran baru</summary>
    public static int NextDepositId() => (Db.Deposits.Max(d => (int?)d.Id) ?? 0) + 1;
}
