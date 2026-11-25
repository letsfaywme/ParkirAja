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

    /// <summary>Queryable untuk seluruh akun pengguna</summary>
    public static IQueryable<AppUser> Users => Db.Users;

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
    /// <remarks>
    /// PRESENTASI: ini JANTUNG dari modul Vehicle Entry.
    /// Empat aturan dijalankan berurutan, dan setiap penolakan mengembalikan
    /// pesan yang langsung bisa ditampilkan ke pengguna:
    ///   ATURAN 1 (baris "Format nomor plat")  -> tolak plat yang tidak sesuai format Indonesia
    ///   ATURAN 2 (baris "Cek duplikasi")      -> tolak kendaraan yang masih parkir
    ///   ATURAN 3 (baris "Cek daya tampung")   -> tolak kalau area sudah penuh
    ///   ATURAN 4 (baris "Tarif")              -> tentukan tarif per jam sesuai jenis kendaraan
    /// Setelah lolos semua aturan, tiket ditulis ke PostgreSQL dan counter area ditambah.
    /// </remarks>
    /// <param name="spotId">ID area parkir</param>
    /// <param name="jukirId">ID petugas jukir yang bertugas</param>
    /// <param name="plateNumber">Nomor plat kendaraan</param>
    /// <param name="type">Jenis kendaraan (Motorcycle/Car)</param>
    /// <returns>Tuple berisi status keberhasilan, pesan notifikasi, dan objek transaksi</returns>
    public static (bool Success, string Message, ParkingTransaction? Tx) CreateEntry(int spotId, int jukirId, string plateNumber, VehicleType type)
    {
        // Normalisasi: buang spasi tepi dan ubah ke huruf besar.
        // Tujuannya agar "b 1234 abc" dan "B 1234 ABC" dianggap plat yang sama.
        string normalizedPlate = plateNumber.Trim().ToUpperInvariant();

        // ATURAN 1 - Validasi format plat nomor Indonesia standar.
        // Pola: 1-2 huruf, spasi, 1-4 angka, spasi, 1-3 huruf. Contoh: B 1234 ABC, D 99 X.
        if (!Regex.IsMatch(normalizedPlate, @"^[A-Z]{1,2}\s\d{1,4}\s[A-Z]{1,3}$"))
        {
            return (false, $"Format nomor plat '{normalizedPlate}' tidak valid. Contoh format standar: 'B 1234 ABC'.", null);
        }

        // ATURAN 2 - Tolak duplikasi: satu plat hanya boleh punya satu tiket ACTIVE.
        // Mencegah kendaraan yang sama tercatat masuk dua kali sebelum keluar.
        if (IsVehicleParked(normalizedPlate))
        {
            return (false, $"Kendaraan dengan plat {normalizedPlate} masih tercatat aktif di area parkir!", null);
        }

        // Ambil area parkir yang dipilih. Bila ID tidak ditemukan, pakai area pertama
        // sebagai cadangan agar aplikasi tidak berhenti karena data area bermasalah.
        var spot = Db.Spots.FirstOrDefault(s => s.Id == spotId) ?? Db.Spots.First();

        // ATURAN 3 - Cek daya tampung area.
        // Occupied adalah jumlah kendaraan yang sedang parkir di area tersebut.
        if (spot.Occupied >= spot.Capacity)
        {
            return (false, $"Area parkir '{spot.Name}' sudah penuh ({spot.Occupied}/{spot.Capacity})!", null);
        }

        // ATURAN 4 - Tentukan tarif per jam berdasarkan jenis kendaraan.
        // Motor memakai MotorcycleRate, mobil memakai CarRate. Tarif disimpan
        // di tiket (RateAtEntry) supaya perubahan tarif nanti tidak mengubah tiket lama.
        var jukir = Db.Users.FirstOrDefault(u => u.Id == jukirId);
        decimal rate = type == VehicleType.Motorcycle ? spot.MotorcycleRate : spot.CarRate;

        // Nomor tiket berikutnya. Dihitung dari ID terbesar + 1.
        int nextId = (Db.Transactions.Max(t => (int?)t.Id) ?? 0) + 1;

        // Susun data tiket baru. Status awal selalu ACTIVE (kendaraan masih di dalam).
        var tx = new ParkingTransaction
        {
            Id = nextId,
            ParkingSpotId = spot.Id,
            LocationName = spot.Name,   // nama lokasi disalin agar riwayat tetap terbaca
            JukirId = jukirId,
            JukirName = jukir?.Name ?? "Jukir",
            PlateNumber = normalizedPlate,
            VehicleType = type,
            EntryTime = DateTimeOffset.Now,
            RateAtEntry = rate,
            Status = "ACTIVE"
        };

        Db.Transactions.Add(tx);   // daftarkan tiket baru
        spot.Occupied++;           // tambah counter kendaraan terisi di area ini
        Db.SaveChanges();          // tulis ke PostgreSQL (satu transaksi)

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
    /// Menyimpan seluruh perubahan entitas yang sedang dilacak ke database.
    /// </summary>
    public static void SaveChanges() => Db.SaveChanges();
}
