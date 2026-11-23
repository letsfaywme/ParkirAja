using System.Text.RegularExpressions;
using ParkingSystem.Security;
namespace ParkingSystem.Database;

/// <summary>
/// Lapisan repositori fasad yang membungkus ParkingDbContext EF Core (PostgreSQL).
/// Menyediakan query LINQ dan method mutasi data bisnis yang dipanggil oleh antarmuka Views.
/// </summary>
/// <remarks>
/// Cakupan method sengaja dipersempit ke alur yang didemonstrasikan:
/// autentikasi dan Vehicle Entry (buat / ubah / batalkan tiket).
/// </remarks>
public static class InMemoryDb
{
    /// <summary>Akses ke singleton ParkingDbContext</summary>
    private static ParkingDbContext Db => ParkingDbContext.Instance;

    /// <summary>Queryable untuk seluruh titik area parkir</summary>
    public static IQueryable<ParkingSpot> Spots => Db.Spots;

    /// <summary>Queryable untuk seluruh transaksi tiket parkir</summary>
    public static IQueryable<ParkingTransaction> Transactions => Db.Transactions;

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
    /// Menyimpan seluruh perubahan entitas yang sedang dilacak ke database.
    /// </summary>
    public static void SaveChanges() => Db.SaveChanges();
}
