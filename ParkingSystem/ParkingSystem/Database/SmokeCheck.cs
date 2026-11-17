using System.Diagnostics;
using ParkingSystem.Database;

namespace ParkingSystem.Checks;

/// <summary>
/// Kelas penguji otomatis (smoke test) tanpa framework tambahan.
/// Dijalankan saat aplikasi startup untuk memastikan logika autentikasi, transaksi masuk,
/// pencegahan duplikasi plat aktif, dan kalkulasi pembayaran berjalan valid sesuai Draw.io.
/// </summary>
public static class SmokeCheck
{
    /// <summary>
    /// Menjalankan seluruh skenario verifikasi in-memory.
    /// Melemparkan exception AssertionError jika ada salah satu assertion yang gagal.
    /// </summary>
    public static void Run()
    {
        // 1. Verifikasi Autentikasi Pengguna
        var admin = InMemoryDb.Authenticate("admin", "adminpassword");
        Debug.Assert(admin != null, "Admin auth must succeed");
        Debug.Assert(admin.Role == UserRole.Admin, "Role must be Admin");

        var jukir = InMemoryDb.Authenticate("jukir1", "jukirpassword");
        Debug.Assert(jukir != null, "Jukir auth must succeed");
        Debug.Assert(jukir.Role == UserRole.Jukir, "Role must be Jukir");

        var invalid = InMemoryDb.Authenticate("invalid", "wrong");
        Debug.Assert(invalid == null, "Invalid login must fail");

        // 2. Verifikasi Siklus Kendaraan Masuk & Keluar
        int initialTxCount = InMemoryDb.Transactions.Count();
        var (entrySuccess, _, entryTx) = InMemoryDb.CreateEntry(1, 2, "B 7777 TST", VehicleType.Motorcycle);
        Debug.Assert(entrySuccess && entryTx != null, "Entry ticket creation must succeed");
        Debug.Assert(entryTx.Id > 0, "Entry ticket ID must be positive");
        Debug.Assert(entryTx.Status == "ACTIVE", "New ticket status must be ACTIVE");
        Debug.Assert(InMemoryDb.Transactions.Count() == initialTxCount + 1, "Transaction count must increase");

        // Verifikasi pencegahan duplikasi plat aktif (Draw.io: Is the vehicle still parked?)
        var (duplicateSuccess, _, _) = InMemoryDb.CreateEntry(1, 2, "B 7777 TST", VehicleType.Motorcycle);
        Debug.Assert(!duplicateSuccess, "Duplicate active vehicle entry must be rejected");

        // Verifikasi kalkulasi tarif keluar (Draw.io: Calculate parking duration and vehicle fare)
        var (foundTx, fare) = InMemoryDb.CalculateExit("B 7777 TST");
        Debug.Assert(foundTx != null, "Active ticket must be found for exit");
        Debug.Assert(fare >= 2000, "Calculated fare must be >= base rate");

        // Verifikasi checkout pembayaran (Draw.io: Update status to completed)
        bool exitOk = InMemoryDb.ProcessExit(foundTx.Id, 2, fare);
        Debug.Assert(exitOk, "Exit process must succeed");
        Debug.Assert(foundTx.Status == "COMPLETED", "Ticket must transition to COMPLETED");

        // 3. Verifikasi Ringkasan Dashboard
        var summary = InMemoryDb.GetJukirSummary(2);
        Debug.Assert(summary.Entries > 0, "Summary entries must be > 0");

        Console.WriteLine("ALL IN-MEMORY SMOKE CHECKS PASSED!");
    }
}
