using System.Data;
using Npgsql;

namespace ParkingSystem;

public sealed partial class ParkingService
{
    private const string TransactionColumns = """
        t.id,t.parking_spot_id,p.location_name,t.jukir_id,j.name,t.plate_number,t.vehicle_type,
        t.entry_time,t.exit_time,t.rate_at_entry,t.fare,t.status,t.exit_jukir_id,e.name
        """;
    private const string TransactionJoins = """
        FROM parking.parking_transactions t
        JOIN parking.parking_spots p ON p.id=t.parking_spot_id
        JOIN parking.jukir j ON j.id=t.jukir_id
        LEFT JOIN parking.jukir e ON e.id=t.exit_jukir_id
        """;

    private static string NormalizePlate(string plate)
    {
        ArgumentNullException.ThrowIfNull(plate);
        string value = string.Concat(plate.Where(c => !char.IsWhiteSpace(c))).ToUpperInvariant();
        if (value.Length is < 1 or > 20 || value.Any(c => !(c is >= 'A' and <= 'Z' or >= '0' and <= '9')))
            throw new ArgumentException("Plat harus berisi 1–20 huruf atau angka.", nameof(plate));
        return value;
    }

    private static ParkingTransaction ReadParkingTransaction(NpgsqlDataReader r) => new(
        r.GetInt32(0), r.GetInt32(1), r.GetString(2), r.GetInt32(3), r.GetString(4), r.GetString(5),
        r.GetString(6) == "motorcycle" ? VehicleType.Motorcycle : VehicleType.Car,
        r.GetFieldValue<DateTimeOffset>(7), r.IsDBNull(8) ? null : r.GetFieldValue<DateTimeOffset>(8),
        r.GetDecimal(9), r.IsDBNull(10) ? null : r.GetDecimal(10), r.GetString(11),
        r.IsDBNull(12) ? null : r.GetInt32(12), r.IsDBNull(13) ? null : r.GetString(13));

    private static async Task<List<ParkingTransaction>> ReadParkingTransactionsAsync(NpgsqlCommand command, CancellationToken ct)
    {
        var rows = new List<ParkingTransaction>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) rows.Add(ReadParkingTransaction(reader));
        return rows;
    }

    // All mutations take location before account and transaction locks, including stale-session checks.
    private static async Task<(decimal Motorcycle, decimal Car, int Capacity)> LockAssignedLocationAsync(
        UserSession session, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken ct)
    {
        if (session.Role != UserRole.Jukir || session.ParkingSpotId is null)
            throw new UnauthorizedAccessException("Akses tidak diizinkan.");
        decimal motorcycle, car;
        int capacity;
        await using (var location = new NpgsqlCommand("SELECT motorcycle_rate,car_rate,capacity,is_active FROM parking.parking_spots WHERE id=@spot FOR UPDATE", connection, transaction))
        {
            location.Parameters.AddWithValue("spot", session.ParkingSpotId.Value);
            await using var reader = await location.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct) || !reader.GetBoolean(3))
                throw new UnauthorizedAccessException("Akun atau lokasi tidak aktif. Silakan masuk kembali.");
            motorcycle = reader.GetDecimal(0);
            car = reader.GetDecimal(1);
            capacity = reader.GetInt32(2);
        }
        await using var account = new NpgsqlCommand("SELECT id FROM parking.jukir WHERE id=@id AND username=@username AND parking_spot_id=@spot AND is_active FOR UPDATE", connection, transaction);
        account.Parameters.AddWithValue("id", session.Id);
        account.Parameters.AddWithValue("username", session.Username);
        account.Parameters.AddWithValue("spot", session.ParkingSpotId.Value);
        if (await account.ExecuteScalarAsync(ct) is null)
            throw new UnauthorizedAccessException("Akun atau lokasi tidak aktif. Silakan masuk kembali.");
        return (motorcycle, car, capacity);
    }

    private static async Task<ParkingTransaction> LoadTransactionAsync(int id, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand($"SELECT {TransactionColumns} {TransactionJoins} WHERE t.id=@id", connection, transaction);
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) throw new InvalidOperationException("Transaksi tidak ditemukan.");
        return ReadParkingTransaction(reader);
    }

    public async Task<ParkingTransaction> EnterVehicleAsync(UserSession session, string plate, VehicleType type, CancellationToken ct)
    {
        await AuthorizeAsync(session, UserRole.Jukir, ct);
        plate = NormalizePlate(plate);
        if (!Enum.IsDefined(type)) throw new ArgumentException("Jenis kendaraan tidak valid.", nameof(type));
        await using var connection = await source.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        var location = await LockAssignedLocationAsync(session, connection, transaction, ct);
        await using (var duplicate = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM parking.parking_transactions WHERE plate_number=@plate AND status='active')", connection, transaction))
        {
            duplicate.Parameters.AddWithValue("plate", plate);
            if ((bool)(await duplicate.ExecuteScalarAsync(ct))!) throw new InvalidOperationException("Kendaraan masih terparkir.");
        }
        await using (var occupancy = new NpgsqlCommand("SELECT count(*) FROM parking.parking_transactions WHERE parking_spot_id=@spot AND status='active'", connection, transaction))
        {
            occupancy.Parameters.AddWithValue("spot", session.ParkingSpotId!.Value);
            if ((long)(await occupancy.ExecuteScalarAsync(ct))! >= location.Capacity)
                throw new InvalidOperationException("Kapasitas lokasi parkir penuh.");
        }
        await using var insert = new NpgsqlCommand("""
            INSERT INTO parking.parking_transactions(parking_spot_id,jukir_id,plate_number,vehicle_type,entry_time,rate_at_entry)
            VALUES (@spot,@jukir,@plate,@type,clock_timestamp(),@rate) RETURNING id
            """, connection, transaction);
        insert.Parameters.AddWithValue("spot", session.ParkingSpotId!.Value);
        insert.Parameters.AddWithValue("jukir", session.Id);
        insert.Parameters.AddWithValue("plate", plate);
        insert.Parameters.AddWithValue("type", type == VehicleType.Motorcycle ? "motorcycle" : "car");
        insert.Parameters.AddWithValue("rate", type == VehicleType.Motorcycle ? location.Motorcycle : location.Car);
        int id;
        try { id = (int)(await insert.ExecuteScalarAsync(ct))!; }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        { throw new InvalidOperationException("Kendaraan masih terparkir.", ex); }
        var result = await LoadTransactionAsync(id, connection, transaction, ct);
        await transaction.CommitAsync(ct);
        return result;
    }

    public async Task<ParkingTransaction?> FindActiveVehicleAsync(UserSession session, string plate, CancellationToken ct)
    {
        await AuthorizeAsync(session, UserRole.Jukir, ct);
        plate = NormalizePlate(plate);
        await using var command = source.CreateCommand($"SELECT {TransactionColumns} {TransactionJoins} WHERE t.parking_spot_id=@spot AND t.plate_number=@plate AND t.status='active'");
        command.Parameters.AddWithValue("spot", session.ParkingSpotId!.Value);
        command.Parameters.AddWithValue("plate", plate);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadParkingTransaction(reader) : null;
    }
    public async Task<ParkingTransaction?> FindLatestVehicleAsync(UserSession session, string plate, CancellationToken ct)
    {
        await AuthorizeAsync(session, UserRole.Jukir, ct);
        plate = NormalizePlate(plate);
        await using var command = source.CreateCommand($"SELECT {TransactionColumns} {TransactionJoins} WHERE t.parking_spot_id=@spot AND t.plate_number=@plate ORDER BY t.id DESC LIMIT 1");
        command.Parameters.AddWithValue("spot", session.ParkingSpotId!.Value);
        command.Parameters.AddWithValue("plate", plate);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadParkingTransaction(reader) : null;
    }

    public async Task<ParkingTransaction> CompleteExitAsync(UserSession session, int transactionId, CancellationToken ct)
    {
        await AuthorizeAsync(session, UserRole.Jukir, ct);
        await using var connection = await source.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await LockAssignedLocationAsync(session, connection, transaction, ct);
        await using (var target = new NpgsqlCommand("SELECT id FROM parking.parking_transactions WHERE id=@id AND parking_spot_id=@spot AND status='active' FOR UPDATE", connection, transaction))
        {
            target.Parameters.AddWithValue("id", transactionId);
            target.Parameters.AddWithValue("spot", session.ParkingSpotId!.Value);
            if (await target.ExecuteScalarAsync(ct) is null)
                throw new InvalidOperationException("Transaksi sudah selesai atau tidak ditemukan.");
        }
        await using (var update = new NpgsqlCommand("UPDATE parking.parking_transactions SET exit_time=GREATEST(clock_timestamp(),entry_time),fare=rate_at_entry,exit_jukir_id=@jukir,status='completed' WHERE id=@id", connection, transaction))
        {
            update.Parameters.AddWithValue("id", transactionId);
            update.Parameters.AddWithValue("jukir", session.Id);
            await update.ExecuteNonQueryAsync(ct);
        }
        var result = await LoadTransactionAsync(transactionId, connection, transaction, ct);
        await transaction.CommitAsync(ct);
        return result;
    }

    public async Task<HistoryPage> GetHistoryAsync(UserSession session, DateOnly date, VehicleType? type, string? plate, int page, CancellationToken ct)
    {
        await AuthorizeAsync(session, UserRole.Jukir, ct);
        if (page < 1 || page > int.MaxValue / 50) throw new ArgumentException("Halaman tidak valid.", nameof(page));
        if (type.HasValue && !Enum.IsDefined(type.Value)) throw new ArgumentException("Jenis kendaraan tidak valid.", nameof(type));
        var bounds = ParkingTime.Bounds(date, date);
        string filter = "t.parking_spot_id=@spot AND (t.jukir_id=@jukir OR t.exit_jukir_id=@jukir) AND COALESCE(t.exit_time,t.entry_time)>=@from AND COALESCE(t.exit_time,t.entry_time)<@to";
        if (type.HasValue) filter += " AND t.vehicle_type=@type";
        if (!string.IsNullOrWhiteSpace(plate)) filter += " AND t.plate_number=@plate";
        await using var connection = await source.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        await using var command = new NpgsqlCommand("SELECT count(*) FROM parking.parking_transactions t WHERE " + filter, connection, transaction);
        command.Parameters.AddWithValue("spot", session.ParkingSpotId!.Value);
        command.Parameters.AddWithValue("jukir", session.Id);
        command.Parameters.AddWithValue("from", bounds.From);
        command.Parameters.AddWithValue("to", bounds.To);
        if (type.HasValue) command.Parameters.AddWithValue("type", type == VehicleType.Motorcycle ? "motorcycle" : "car");
        if (!string.IsNullOrWhiteSpace(plate)) command.Parameters.AddWithValue("plate", NormalizePlate(plate));
        int total = checked((int)(long)(await command.ExecuteScalarAsync(ct))!);
        command.CommandText = $"SELECT {TransactionColumns} {TransactionJoins} WHERE {filter} ORDER BY COALESCE(t.exit_time,t.entry_time) DESC,t.id DESC LIMIT 50 OFFSET @offset";
        command.Parameters.AddWithValue("offset", (page - 1) * 50);
        var rows = await ReadParkingTransactionsAsync(command, ct);
        await transaction.CommitAsync(ct);
        return new HistoryPage(rows, total, page);
    }

    public async Task<ParkingSpot> GetAssignedSpotAsync(UserSession session, CancellationToken ct)
    {
        await AuthorizeAsync(session, UserRole.Jukir, ct);
        await using var command = source.CreateCommand("""
            SELECT p.id,p.location_name,p.address,p.motorcycle_rate,p.car_rate,p.capacity,p.is_active,
                (SELECT count(*)::integer FROM parking.parking_transactions t WHERE t.parking_spot_id=p.id AND t.status='active')
            FROM parking.parking_spots p WHERE p.id=@spot AND p.is_active
            """);
        command.Parameters.AddWithValue("spot", session.ParkingSpotId!.Value);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) throw new UnauthorizedAccessException("Lokasi tidak aktif.");
        return ReadTransactionSpot(reader);
    }

    private static ParkingSpot ReadTransactionSpot(NpgsqlDataReader reader) => new(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetDecimal(3), reader.GetDecimal(4), reader.GetInt32(5), reader.GetBoolean(6), reader.GetInt32(7));

    public async Task<IReadOnlyList<ParkingTransaction>> GetLocationTransactionsAsync(UserSession session, int spotId, CancellationToken ct)
    {
        await AuthorizeAsync(session, UserRole.Admin, ct);
        await using var command = source.CreateCommand($"SELECT {TransactionColumns} {TransactionJoins} WHERE t.parking_spot_id=@spot ORDER BY COALESCE(t.exit_time,t.entry_time) DESC,t.id DESC");
        command.Parameters.AddWithValue("spot", spotId);
        return await ReadParkingTransactionsAsync(command, ct);
    }

    public async Task<DashboardSummary> GetDashboardAsync(UserSession session, CancellationToken ct)
    {
        await AuthorizeAsync(session, session.Role, ct);
        bool admin = session.Role == UserRole.Admin;
        var today = ParkingTime.Today(timeProvider);
        var bounds = ParkingTime.Bounds(today, today);
        await using var connection = await source.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        await using var command = new NpgsqlCommand($"""
            SELECT count(*) FILTER(WHERE t.entry_time>=@from AND t.entry_time<@to {(admin ? "" : "AND t.jukir_id=@jukir")})::integer,
                count(*) FILTER(WHERE t.status='completed' AND t.exit_time>=@from AND t.exit_time<@to {(admin ? "" : "AND t.exit_jukir_id=@jukir")})::integer,
                COALESCE(sum(t.fare) FILTER(WHERE t.status='completed' AND t.exit_time>=@from AND t.exit_time<@to {(admin ? "" : "AND t.exit_jukir_id=@jukir")}),0)
            FROM parking.parking_transactions t {(admin ? "" : "WHERE t.parking_spot_id=@spot")}
            """, connection, transaction);
        command.Parameters.AddWithValue("from", bounds.From);
        command.Parameters.AddWithValue("to", bounds.To);
        if (!admin)
        {
            command.Parameters.AddWithValue("jukir", session.Id);
            command.Parameters.AddWithValue("spot", session.ParkingSpotId!.Value);
        }
        int entries, exits;
        decimal revenue;
        await using (var reader = await command.ExecuteReaderAsync(ct))
        {
            await reader.ReadAsync(ct);
            entries = reader.GetInt32(0); exits = reader.GetInt32(1); revenue = reader.GetDecimal(2);
        }
        command.CommandText = $"SELECT {TransactionColumns} {TransactionJoins} {(admin ? "" : "WHERE t.parking_spot_id=@spot AND (t.jukir_id=@jukir OR t.exit_jukir_id=@jukir)")} ORDER BY COALESCE(t.exit_time,t.entry_time) DESC,t.id DESC LIMIT 10";
        var latest = await ReadParkingTransactionsAsync(command, ct);
        command.CommandText = $"""
            SELECT p.id,p.location_name,p.address,p.motorcycle_rate,p.car_rate,p.capacity,p.is_active,
                (SELECT count(*)::integer FROM parking.parking_transactions t WHERE t.parking_spot_id=p.id AND t.status='active')
            FROM parking.parking_spots p WHERE p.is_active {(admin ? "" : "AND p.id=@spot")} ORDER BY p.location_name,p.id
            """;
        var spots = new List<ParkingSpot>();
        await using (var reader = await command.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct)) spots.Add(ReadTransactionSpot(reader));
        await transaction.CommitAsync(ct);
        return new DashboardSummary(entries, exits, revenue, latest, spots);
    }
}
