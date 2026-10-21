using Npgsql;

namespace ParkingSystem;

public sealed record ReportRow(int TransactionId, string PlateNumber, VehicleType VehicleType,
    string LocationName, string EntryJukirName, string? ExitJukirName,
    DateTimeOffset EntryTime, DateTimeOffset? ExitTime, decimal Fare, string Status);

public sealed record ReportDocument(DateOnly From, DateOnly To, string? JukirFilter, string? ParkingSpotFilter,
    IReadOnlyList<ReportRow> Rows)
{
    public decimal Total => Rows.Sum(row => row.Fare);
}

public sealed partial class ParkingService
{
    public async Task<ReportDocument> GetExportReportAsync(UserSession session, DateOnly from, DateOnly to,
        int? jukirId, int? parkingSpotId, CancellationToken ct)
    {
        if (from > to) throw new ArgumentException("Tanggal awal tidak boleh setelah tanggal akhir.");
        await AuthorizeAsync(session, session.Role, ct);
        var bounds = ParkingTime.Bounds(from, to);
        await using var command = source.CreateCommand();
        var sql = """
            SELECT t.id,t.plate_number,t.vehicle_type,p.location_name,j.name,e.name,
                   t.entry_time,t.exit_time,COALESCE(t.fare,0),t.status,
                   j.id,p.id
            FROM parking.parking_transactions t
            JOIN parking.parking_spots p ON p.id=t.parking_spot_id
            JOIN parking.jukir j ON j.id=t.jukir_id
            LEFT JOIN parking.jukir e ON e.id=t.exit_jukir_id
            WHERE COALESCE(t.exit_time,t.entry_time) >= @fromUtc
              AND COALESCE(t.exit_time,t.entry_time) < @toUtc
            """;
        command.Parameters.AddWithValue("fromUtc", bounds.From);
        command.Parameters.AddWithValue("toUtc", bounds.To);
        if (session.Role == UserRole.Admin)
        {
            if (jukirId is not null)
            {
                sql += " AND (t.jukir_id=@jukir OR t.exit_jukir_id=@jukir)";
                command.Parameters.AddWithValue("jukir", jukirId.Value);
                await EnsureFilterExistsAsync("jukir", jukirId.Value, ct);
            }
            if (parkingSpotId is not null)
            {
                sql += " AND t.parking_spot_id=@spot";
                command.Parameters.AddWithValue("spot", parkingSpotId.Value);
                await EnsureFilterExistsAsync("parking_spots", parkingSpotId.Value, ct);
            }
        }
        else
        {
            sql += " AND p.id=@assignedSpot AND (t.jukir_id=@owner OR t.exit_jukir_id=@owner)";
            command.Parameters.AddWithValue("assignedSpot", session.ParkingSpotId ?? -1);
            command.Parameters.AddWithValue("owner", session.Id);
            jukirId = null;
            parkingSpotId = null;
        }
        sql += " ORDER BY COALESCE(t.exit_time,t.entry_time), t.id";
        command.CommandText = sql;
        var rows = new List<ReportRow>();
        string? jukirName = null, spotName = null;
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            rows.Add(new ReportRow(reader.GetInt32(0), reader.GetString(1),
                reader.GetString(2) == "motorcycle" ? VehicleType.Motorcycle : VehicleType.Car,
                reader.GetString(3), reader.GetString(4), reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.GetFieldValue<DateTimeOffset>(6), reader.IsDBNull(7) ? null : reader.GetFieldValue<DateTimeOffset>(7),
                reader.GetDecimal(8), reader.GetString(9)));
            if (jukirId is not null && jukirName is null) jukirName = reader.GetString(4);
            if (parkingSpotId is not null && spotName is null) spotName = reader.GetString(3);
        }
        return new ReportDocument(from, to, jukirName, spotName, rows);
    }

    private async Task EnsureFilterExistsAsync(string table, int id, CancellationToken ct)
    {
        await using var check = source.CreateCommand($"SELECT EXISTS (SELECT 1 FROM parking.{table} WHERE id=@id)");
        check.Parameters.AddWithValue("id", id);
        if ((bool?)await check.ExecuteScalarAsync(ct) != true)
            throw new ArgumentException("Filter laporan tidak ditemukan.");
    }
}
