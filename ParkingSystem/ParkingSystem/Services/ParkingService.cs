using Npgsql;

namespace ParkingSystem;

public sealed partial class ParkingService(NpgsqlDataSource source, TimeProvider timeProvider)
{
    private readonly NpgsqlDataSource source = source;
    private readonly TimeProvider timeProvider = timeProvider;

    public Task ValidateSessionAsync(UserSession session, CancellationToken ct) => AuthorizeAsync(session, session.Role, ct);

    public async Task<UserSession?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken)
    {
        username = username.Trim().ToLowerInvariant();
        await using var command = source.CreateCommand("""
            SELECT a.id,a.username,a.name,a.password_hash,'admin',NULL::integer,true
            FROM parking.admins a WHERE a.username=@username
            UNION ALL
            SELECT j.id,j.username,j.name,j.password_hash,'jukir',j.parking_spot_id,j.is_active AND p.is_active
            FROM parking.jukir j JOIN parking.parking_spots p ON p.id=j.parking_spot_id WHERE j.username=@username
            """);
        command.Parameters.AddWithValue("username", username);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        var session = new UserSession(reader.GetInt32(0), reader.GetString(1), reader.GetString(2),
            reader.GetString(4) == "admin" ? UserRole.Admin : UserRole.Jukir, reader.IsDBNull(5) ? null : reader.GetInt32(5));
        var valid = reader.GetBoolean(6) && PasswordHash.Verify(password, reader.GetString(3));
        if (await reader.ReadAsync(cancellationToken)) return null;
        return valid ? session : null;
    }

    private async Task AuthorizeAsync(UserSession session, UserRole role, CancellationToken ct)
    {
        if (!Enum.IsDefined(role) || session.Role != role) throw new UnauthorizedAccessException("Akses tidak diizinkan.");
        await using var command = source.CreateCommand(role == UserRole.Admin
            ? "SELECT EXISTS(SELECT 1 FROM parking.admins WHERE id=@id AND username=@username AND role='admin')"
            : "SELECT EXISTS(SELECT 1 FROM parking.jukir j JOIN parking.parking_spots p ON p.id=j.parking_spot_id WHERE j.id=@id AND j.username=@username AND j.parking_spot_id=@spot AND j.is_active AND p.is_active)");
        command.Parameters.AddWithValue("id", session.Id);
        command.Parameters.AddWithValue("username", session.Username);
        if (role == UserRole.Jukir) command.Parameters.AddWithValue("spot", session.ParkingSpotId ?? -1);
        if (!(bool)(await command.ExecuteScalarAsync(ct))!)
            throw new UnauthorizedAccessException("Akun atau lokasi tidak aktif. Silakan masuk kembali.");
    }
}
