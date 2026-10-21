using Npgsql;
using ParkingSystem;

internal static class AuthChecks
{
    internal static UserSession Admin { get; private set; } = null!;
    internal static UserSession A { get; private set; } = null!;
    internal static UserSession B { get; private set; } = null!;
    internal static UserSession C { get; private set; } = null!;
    internal static string Password { get; private set; } = string.Empty;
    internal static int SpotA { get; private set; }
    internal static int SpotB { get; private set; }
    internal static string OwnerConnectionString { get; private set; } = string.Empty;

    public static async Task RunAsync()
    {
        string ownerString = Environment.GetEnvironmentVariable("PARKING_CHECKS_OWNER_CONNECTION")
            ?? throw new InvalidOperationException("PARKING_CHECKS_OWNER_CONNECTION wajib untuk fixture autentikasi.");
        string runtimeString = DatabaseSettings.GetConnectionString();
        string fixtureData = Environment.GetEnvironmentVariable("PARKING_CHECKS_DATA_DIRECTORY")
            ?? throw new InvalidOperationException("Auth checks hanya boleh berjalan melalui wrapper fixture.");
        var ownerSettings = new NpgsqlConnectionStringBuilder(ownerString);
        var runtimeSettings = new NpgsqlConnectionStringBuilder(runtimeString);
        if (!Directory.Exists(fixtureData) || ownerSettings.Database != "parking_checks"
            || ownerSettings.Host != "127.0.0.1" || runtimeSettings.Database != ownerSettings.Database
            || runtimeSettings.Host != ownerSettings.Host || runtimeSettings.Port != ownerSettings.Port)
            throw new InvalidOperationException("Auth checks membutuhkan cluster fixture parking_checks loopback yang sama.");
        OwnerConnectionString = ownerString;

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        await using var owner = new NpgsqlConnection(ownerString);
        await owner.OpenAsync();
        Password = Environment.GetEnvironmentVariable("PARKING_CHECKS_PASSWORD")
            ?? throw new InvalidOperationException("PARKING_CHECKS_PASSWORD wajib untuk fixture autentikasi.");
        string hash = PasswordHash.Hash(Password);
        int adminId, aId, bId, cId;
        await using (var transaction = await owner.BeginTransactionAsync())
        {
            await using var seed = new NpgsqlCommand("""
                SELECT pg_advisory_xact_lock(736192004::bigint);
                INSERT INTO parking.parking_spots(location_name,address,motorcycle_rate,car_rate,capacity)
                    VALUES ('Check spot A','Fixture A',2000,5000,1),('Check spot B','Fixture B',2000,5000,10)
                    RETURNING id;
                INSERT INTO parking.admins(username,name,password_hash,role)
                    VALUES ('check-admin','Check Admin',@hash,'admin') RETURNING id;
                """, owner, transaction);
            seed.Parameters.AddWithValue("hash", hash);
            await using (var reader = await seed.ExecuteReaderAsync())
            {
                // First result belongs to advisory lock; next contains both locations.
                Require(await reader.NextResultAsync(), "Fixture lokasi tidak dikembalikan.");
                Require(await reader.ReadAsync(), "Fixture lokasi A tidak ditemukan.");
                SpotA = reader.GetInt32(0);
                Require(await reader.ReadAsync(), "Fixture lokasi B tidak ditemukan.");
                SpotB = reader.GetInt32(0);
                Require(await reader.NextResultAsync() && await reader.ReadAsync(), "Fixture admin tidak ditemukan.");
                adminId = reader.GetInt32(0);
            }
            await using var jukir = new NpgsqlCommand("""
                INSERT INTO parking.jukir(parking_spot_id,username,name,password_hash)
                VALUES (@a,'check-jukir-a','Check Jukir A',@hash),
                    (@a,'check-jukir-b','Check Jukir B',@hash),
                    (@b,'check-jukir-c','Check Jukir C',@hash)
                RETURNING id,username;
                """, owner, transaction);
            jukir.Parameters.AddWithValue("a", SpotA);
            jukir.Parameters.AddWithValue("b", SpotB);
            jukir.Parameters.AddWithValue("hash", hash);
            var ids = new Dictionary<string, int>();
            await using (var reader = await jukir.ExecuteReaderAsync())
                while (await reader.ReadAsync()) ids.Add(reader.GetString(1), reader.GetInt32(0));
            aId = ids["check-jukir-a"];
            bId = ids["check-jukir-b"];
            cId = ids["check-jukir-c"];
            await transaction.CommitAsync();
        }

        await using var source = NpgsqlDataSource.Create(runtimeString);
        var service = new ParkingService(source, TimeProvider.System);
        async Task<UserSession> LoginAsync(string username, int id, UserRole role, int? spot)
        {
            var session = await service.AuthenticateAsync(username, Password, CancellationToken.None);
            Require(session is not null && session.Id == id && session.Username == username
                && session.Role == role && session.ParkingSpotId == spot && !string.IsNullOrWhiteSpace(session.Name),
                "Autentikasi fixture gagal: " + username);
            Require(await service.AuthenticateAsync(username, Password + "wrong", CancellationToken.None) is null,
                "Password salah diterima: " + username);
            return session!;
        }
        Admin = await LoginAsync("check-admin", adminId, UserRole.Admin, null);
        A = await LoginAsync("check-jukir-a", aId, UserRole.Jukir, SpotA);
        B = await LoginAsync("check-jukir-b", bId, UserRole.Jukir, SpotA);
        C = await LoginAsync("check-jukir-c", cId, UserRole.Jukir, SpotB);
        Require(await service.AuthenticateAsync(" CHECK-JUKIR-A ", Password, CancellationToken.None) == A,
            "Normalisasi username gagal.");
        Require(await service.AuthenticateAsync("check-unknown", Password, CancellationToken.None) is null,
            "Username tidak dikenal diterima.");
        Require(await service.AuthenticateAsync("", Password, CancellationToken.None) is null,
            "Username kosong diterima.");
        Require(await service.AuthenticateAsync(A.Username, "", CancellationToken.None) is null,
            "Password kosong diterima.");

        // Owner mutations deliberately bypass service, exercising externally corrupted/inactive accounts.
        await using var updateAccount = new NpgsqlCommand(
            "UPDATE parking.jukir SET password_hash=@hash,is_active=@active WHERE id=@id", owner);
        updateAccount.Parameters.AddWithValue("hash", hash);
        updateAccount.Parameters.AddWithValue("active", true);
        updateAccount.Parameters.AddWithValue("id", aId);
        try
        {
            foreach (string malformed in new[] { "not-a-hash", "pbkdf2-sha256$600000$bad$bad", "pbkdf2-sha256$nope$AA==$AA==" })
            {
                updateAccount.Parameters["hash"].Value = malformed;
                await updateAccount.ExecuteNonQueryAsync();
                Require(await service.AuthenticateAsync(A.Username, Password, CancellationToken.None) is null,
                    "Malformed hash diterima.");
            }
            updateAccount.Parameters["hash"].Value = hash;
            updateAccount.Parameters["active"].Value = false;
            await updateAccount.ExecuteNonQueryAsync();
            Require(await service.AuthenticateAsync(A.Username, Password, CancellationToken.None) is null,
                "Jukir nonaktif diterima.");
            Require(await service.AuthenticateAsync(B.Username, Password, CancellationToken.None) == B,
                "Jukir aktif pada lokasi sama ikut ditolak.");
        }
        finally
        {
            updateAccount.Parameters["hash"].Value = hash;
            updateAccount.Parameters["active"].Value = true;
            await updateAccount.ExecuteNonQueryAsync();
        }

        await using var updateSpot = new NpgsqlCommand("UPDATE parking.parking_spots SET is_active=@active WHERE id=@id", owner);
        updateSpot.Parameters.AddWithValue("active", false);
        updateSpot.Parameters.AddWithValue("id", SpotA);
        try
        {
            await updateSpot.ExecuteNonQueryAsync();
            Require(await service.AuthenticateAsync(A.Username, Password, CancellationToken.None) is null
                && await service.AuthenticateAsync(B.Username, Password, CancellationToken.None) is null,
                "Lokasi nonaktif menerima login Jukir.");
            Require(await service.AuthenticateAsync(C.Username, Password, CancellationToken.None) == C
                && await service.AuthenticateAsync(Admin.Username, Password, CancellationToken.None) == Admin,
                "Lokasi nonaktif memblokir akun di luar lokasi.");
        }
        finally
        {
            updateSpot.Parameters["active"].Value = true;
            await updateSpot.ExecuteNonQueryAsync();
        }

        await using var ambiguous = new NpgsqlCommand("""
            INSERT INTO parking.admins(username,name,password_hash,role)
            VALUES (@username,'Ambiguous fixture',@hash,'admin') RETURNING id
            """, owner);
        ambiguous.Parameters.AddWithValue("username", A.Username);
        ambiguous.Parameters.AddWithValue("hash", hash);
        int ambiguousId = (int)(await ambiguous.ExecuteScalarAsync())!;
        try
        {
            Require(await service.AuthenticateAsync(A.Username, Password, CancellationToken.None) is null,
                "Username ambigu lintas admins/Jukir diterima.");
            updateAccount.Parameters["active"].Value = false;
            await updateAccount.ExecuteNonQueryAsync();
            Require(await service.AuthenticateAsync(A.Username, Password, CancellationToken.None) is null,
                "Username ambigu dengan Jukir nonaktif diterima sebagai admin.");
        }
        finally
        {
            updateAccount.Parameters["active"].Value = true;
            await updateAccount.ExecuteNonQueryAsync();
            await using var cleanup = new NpgsqlCommand("DELETE FROM parking.admins WHERE id=@id", owner);
            cleanup.Parameters.AddWithValue("id", ambiguousId);
            await cleanup.ExecuteNonQueryAsync();
        }

        Require(await service.AuthenticateAsync(A.Username, Password, CancellationToken.None) == A,
            "Fixture autentikasi tidak dipulihkan.");
        await using var locations = source.CreateCommand("""
            SELECT count(*) FROM parking.parking_spots
            WHERE is_active AND motorcycle_rate=2000 AND car_rate=5000
                AND ((id=@a AND capacity=1) OR (id=@b AND capacity=10))
            """);
        locations.Parameters.AddWithValue("a", SpotA);
        locations.Parameters.AddWithValue("b", SpotB);
        Require((long)(await locations.ExecuteScalarAsync())! == 2, "Fixture lokasi tidak terbaca oleh runtime.");
        Console.WriteLine("PASS: auth roles, wrong/unknown credentials, malformed hashes, inactive accounts/locations, ambiguous usernames; fixture persisted.");
    }
}
