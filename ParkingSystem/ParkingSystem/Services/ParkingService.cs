using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Database;
using ParkingSystem.Security;

namespace ParkingSystem.Services;

/// <summary>
/// Implementasi thread-safe layanan sistem parkir berbasis IDbContextFactory.
/// Seluruh query baca menggunakan AsNoTracking dan seluruh timestamp berstandar UTC.
/// </summary>
public class ParkingService : IParkingService
{
    private readonly IDbContextFactory<ParkingDbContext> _contextFactory;
    private static readonly Regex PlateRegex = new(@"^[A-Z]{1,2}\s\d{1,4}\s[A-Z]{1,3}$", RegexOptions.Compiled);

    /// <summary>
    /// Inisialisasi service dengan dependency injection IDbContextFactory.
    /// </summary>
    public ParkingService(IDbContextFactory<ParkingDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    /// <inheritdoc />
    public async Task<UserSession?> AuthenticateAsync(string username, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return null;

        string normUser = username.Trim().ToLower();
        await using var db = await _contextFactory.CreateDbContextAsync(ct);

        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username.ToLower() == normUser, ct);

        if (user == null || !user.IsActive)
            return null;

        if (!PasswordHasher.VerifyPassword(password, user.PasswordHash))
            return null;

        return new UserSession(user.Id, user.Username, user.Name, user.Role, user.ParkingSpotId);
    }

    /// <inheritdoc />
    public async Task<bool> IsVehicleParkedAsync(string plateNumber, CancellationToken ct = default)
    {
        string normPlate = plateNumber.Trim().ToUpperInvariant();
        await using var db = await _contextFactory.CreateDbContextAsync(ct);

        return await db.Transactions
            .AsNoTracking()
            .AnyAsync(t => t.PlateNumber == normPlate && t.Status == "ACTIVE", ct);
    }

    /// <inheritdoc />
    public async Task<(bool Success, string Message, ParkingTransaction? Tx)> CreateEntryAsync(
        int spotId, int jukirId, string plateNumber, VehicleType type, CancellationToken ct = default)
    {
        string normPlate = plateNumber.Trim().ToUpperInvariant();

        if (!PlateRegex.IsMatch(normPlate))
        {
            return (false, $"Format nomor plat '{normPlate}' tidak valid. Contoh standar: 'B 1234 ABC' atau 'D 99 X'.", null);
        }

        await using var db = await _contextFactory.CreateDbContextAsync(ct);

        bool alreadyParked = await db.Transactions
            .AnyAsync(t => t.PlateNumber == normPlate && t.Status == "ACTIVE", ct);

        if (alreadyParked)
        {
            return (false, $"Kendaraan dengan plat '{normPlate}' masih tercatat aktif di area parkir!", null);
        }

        var spot = await db.Spots.FirstOrDefaultAsync(s => s.Id == spotId, ct)
                   ?? await db.Spots.FirstOrDefaultAsync(ct);

        if (spot == null)
        {
            return (false, "Area parkir tidak ditemukan.", null);
        }

        if (spot.Occupied >= spot.Capacity)
        {
            return (false, $"Area parkir '{spot.Name}' sudah penuh ({spot.Occupied}/{spot.Capacity})!", null);
        }

        var jukir = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == jukirId, ct);
        decimal rate = type == VehicleType.Motorcycle ? spot.MotorcycleRate : spot.CarRate;

        int nextId = (await db.Transactions.MaxAsync(t => (int?)t.Id, ct) ?? 0) + 1;

        var tx = new ParkingTransaction
        {
            Id = nextId,
            ParkingSpotId = spot.Id,
            LocationName = spot.Name,
            JukirId = jukirId,
            JukirName = jukir?.Name ?? "Jukir",
            PlateNumber = normPlate,
            VehicleType = type,
            EntryTime = DateTimeOffset.UtcNow,
            RateAtEntry = rate,
            Status = "ACTIVE"
        };

        db.Transactions.Add(tx);
        spot.Occupied++;
        await db.SaveChangesAsync(ct);

        return (true, "Tiket berhasil diterbitkan.", tx);
    }

    /// <inheritdoc />
    public async Task<(ParkingTransaction? Tx, decimal CalculatedFare)> CalculateExitAsync(string plateNumber, CancellationToken ct = default)
    {
        string normPlate = plateNumber.Trim().ToUpperInvariant();
        await using var db = await _contextFactory.CreateDbContextAsync(ct);

        var tx = await db.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.PlateNumber == normPlate && t.Status == "ACTIVE", ct);

        if (tx == null)
            return (null, 0);

        var duration = DateTimeOffset.UtcNow - tx.EntryTime;
        int hours = Math.Max(1, (int)Math.Ceiling(duration.TotalHours));
        decimal fare = tx.RateAtEntry * hours;

        return (tx, fare);
    }

    /// <inheritdoc />
    public async Task<bool> ProcessExitAsync(int txId, int exitJukirId, decimal fare, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);

        var tx = await db.Transactions.FirstOrDefaultAsync(t => t.Id == txId && t.Status == "ACTIVE", ct);
        if (tx == null)
            return false;

        var jukir = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == exitJukirId, ct);

        tx.ExitTime = DateTimeOffset.UtcNow;
        tx.Fare = fare;
        tx.Status = "COMPLETED";
        tx.ExitJukirId = exitJukirId;
        tx.ExitJukirName = jukir?.Name ?? "Jukir";

        var spot = await db.Spots.FirstOrDefaultAsync(s => s.Id == tx.ParkingSpotId, ct);
        if (spot != null && spot.Occupied > 0)
        {
            spot.Occupied--;
        }

        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <inheritdoc />
    public async Task<DashboardSummary> GetJukirSummaryAsync(int jukirId, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);

        var today = DateTimeOffset.UtcNow.Date;
        var myTx = await db.Transactions
            .AsNoTracking()
            .Where(t => (t.JukirId == jukirId || t.ExitJukirId == jukirId) && t.EntryTime.Date == today)
            .ToListAsync(ct);

        int entries = myTx.Count(t => t.JukirId == jukirId);
        int exits = myTx.Count(t => t.ExitJukirId == jukirId && t.Status == "COMPLETED");
        decimal revenue = myTx.Where(t => t.ExitJukirId == jukirId && t.Status == "COMPLETED").Sum(t => t.Fare ?? 0);

        var latest = await db.Transactions
            .AsNoTracking()
            .OrderByDescending(t => t.Id)
            .Take(10)
            .ToListAsync(ct);

        var spots = await db.Spots
            .AsNoTracking()
            .ToListAsync(ct);

        return new DashboardSummary(entries, exits, revenue, latest, spots);
    }

    /// <inheritdoc />
    public async Task<DashboardSummary> GetAdminSummaryAsync(CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);

        var today = DateTimeOffset.UtcNow.Date;
        var todayTx = await db.Transactions
            .AsNoTracking()
            .Where(t => t.EntryTime.Date == today)
            .ToListAsync(ct);

        int entries = todayTx.Count;
        int exits = todayTx.Count(t => t.Status == "COMPLETED");
        decimal revenue = todayTx.Where(t => t.Status == "COMPLETED").Sum(t => t.Fare ?? 0);

        var latest = await db.Transactions
            .AsNoTracking()
            .OrderByDescending(t => t.Id)
            .Take(10)
            .ToListAsync(ct);

        var spots = await db.Spots
            .AsNoTracking()
            .ToListAsync(ct);

        return new DashboardSummary(entries, exits, revenue, latest, spots);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ParkingSpot>> GetActiveSpotsAsync(CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.Spots.AsNoTracking().Where(s => s.IsActive).ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task AddDepositAsync(DailyDeposit deposit, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        if (deposit.Id == 0)
        {
            deposit.Id = (await db.Deposits.MaxAsync(d => (int?)d.Id, ct) ?? 0) + 1;
        }
        db.Deposits.Add(deposit);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<bool> ValidateDepositAsync(int depositId, int adminId, DepositStatus status, string notes, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var dep = await db.Deposits.FirstOrDefaultAsync(d => d.Id == depositId, ct);
        if (dep == null) return false;

        dep.Status = status;
        dep.Notes = notes;
        dep.ValidatedBy = adminId;
        dep.ValidatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <inheritdoc />
    public async Task AddSpotAsync(ParkingSpot spot, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        if (spot.Id == 0)
        {
            spot.Id = (await db.Spots.MaxAsync(s => (int?)s.Id, ct) ?? 0) + 1;
        }
        db.Spots.Add(spot);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task AddUserAsync(AppUser user, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        if (user.Id == 0)
        {
            user.Id = (await db.Users.MaxAsync(u => (int?)u.Id, ct) ?? 0) + 1;
        }
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AppUser>> GetUsersAsync(CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.Users.AsNoTracking().ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DailyDeposit>> GetDepositsAsync(CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.Deposits.AsNoTracking().ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ParkingTransaction>> GetTransactionsAsync(CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.Transactions.AsNoTracking().ToListAsync(ct);
    }
}
