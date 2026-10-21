using System.Diagnostics;
using Npgsql;

internal static class SchemaChecks
{
    public static async Task RunAsync()
    {
        string ownerString = Environment.GetEnvironmentVariable("PARKING_CHECKS_OWNER_CONNECTION")
            ?? throw new InvalidOperationException("PARKING_CHECKS_OWNER_CONNECTION wajib untuk fixture schema.");
        string runtimeString = Environment.GetEnvironmentVariable("PARKING_DB_CONNECTION")
            ?? throw new InvalidOperationException("PARKING_DB_CONNECTION wajib untuk fixture schema.");
        string postgresBin = Environment.GetEnvironmentVariable("PARKING_CHECKS_POSTGRES_BIN")
            ?? throw new InvalidOperationException("PARKING_CHECKS_POSTGRES_BIN wajib untuk fixture schema.");
        // This owner connection is provided only by disposable-cluster wrapper.
        string fixtureData = Environment.GetEnvironmentVariable("PARKING_CHECKS_DATA_DIRECTORY")
            ?? throw new InvalidOperationException("Schema checks hanya boleh berjalan melalui wrapper fixture.");
        if (!Directory.Exists(fixtureData))
            throw new InvalidOperationException("Direktori cluster fixture tidak ditemukan.");
        var ownerSettings = new NpgsqlConnectionStringBuilder(ownerString);
        var runtimeSettings = new NpgsqlConnectionStringBuilder(runtimeString);
        if (ownerSettings.Database != "parking_checks" || ownerSettings.Host != "127.0.0.1"
            || runtimeSettings.Database != ownerSettings.Database || runtimeSettings.Host != ownerSettings.Host
            || runtimeSettings.Port != ownerSettings.Port)
            throw new InvalidOperationException("Schema checks membutuhkan koneksi cluster parking_checks loopback yang sama.");
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Initialize-Database.ps1")))
            directory = directory.Parent;
        string initializer = directory is null
            ? throw new InvalidOperationException("Initialize-Database.ps1 tidak ditemukan.")
            : Path.Combine(directory.FullName, "Initialize-Database.ps1");
        string psql = Path.Combine(postgresBin, "psql.exe");
        if (!File.Exists(psql)) throw new InvalidOperationException("psql.exe fixture tidak ditemukan.");
        await using var owner = new NpgsqlConnection(ownerString);
        await owner.OpenAsync();
        async Task ExecuteAsync(string sql)
        {
            await using var command = new NpgsqlCommand(sql, owner);
            await command.ExecuteNonQueryAsync();
        }
        async Task<bool> InitializeAsync()
        {
            static string Quote(string value) => "'" + value.Replace("\\", "\\\\").Replace("'", "\\'") + "'";
            string connection = $"host={Quote(ownerSettings.Host)} port={ownerSettings.Port} dbname={Quote(ownerSettings.Database!)} user={Quote(ownerSettings.Username!)} connect_timeout=10";
            var start = new ProcessStartInfo("powershell.exe")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            foreach (string argument in new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", initializer,
                "-ConnectionString", connection, "-RuntimeRole", runtimeSettings.Username!, "-PsqlPath", psql })
                start.ArgumentList.Add(argument);
            start.Environment["PGPASSWORD"] = ownerSettings.Password;
            start.Environment["PGCONNECT_TIMEOUT"] = "10";
            using var process = Process.Start(start) ?? throw new InvalidOperationException("Bootstrap fixture tidak dapat dijalankan.");
            Task<string> stdout = process.StandardOutput.ReadToEndAsync();
            Task<string> stderr = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
                throw new InvalidOperationException("Bootstrap fixture timeout.");
            }
            await Task.WhenAll(stdout, stderr);
            // Never propagate process output: it may contain operator connection details.
            return process.ExitCode == 0;
        }
        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        string name = "schema-check-" + Guid.NewGuid().ToString("N");
        int spotId;
        await using (var insert = new NpgsqlCommand("INSERT INTO parking.parking_spots(location_name,address,motorcycle_rate,car_rate,capacity) VALUES (@name,'Fixture schema',2000,5000,1) RETURNING id", owner))
        {
            insert.Parameters.AddWithValue("name", name);
            spotId = (int)(await insert.ExecuteScalarAsync())!;
        }
        try
        {
            Require(await InitializeAsync(), "Bootstrap idempotent ditolak pada schema kompatibel.");
            await using (var query = new NpgsqlCommand("SELECT count(*) FROM parking.parking_spots WHERE id=@id AND location_name=@name AND motorcycle_rate=2000 AND capacity=1", owner))
            {
                query.Parameters.AddWithValue("id", spotId);
                query.Parameters.AddWithValue("name", name);
                Require((long)(await query.ExecuteScalarAsync())! == 1, "Bootstrap mengubah data fixture.");
            }
            await ExecuteAsync("ALTER TABLE parking.parking_spots ALTER COLUMN motorcycle_rate TYPE numeric(13,2)");
            try { Require(!await InitializeAsync(), "Bootstrap menerima tipe kolom incompatible."); }
            finally { await ExecuteAsync("ALTER TABLE parking.parking_spots ALTER COLUMN motorcycle_rate TYPE numeric(12,2)"); }

            await ExecuteAsync("ALTER TABLE parking.parking_spots DROP CONSTRAINT parking_spots_capacity_check");
            try { Require(!await InitializeAsync(), "Bootstrap menerima CHECK constraint hilang."); }
            finally { await ExecuteAsync("ALTER TABLE parking.parking_spots ADD CONSTRAINT parking_spots_capacity_check CHECK(capacity > 0)"); }

            await ExecuteAsync("ALTER TABLE parking.schema_version DROP CONSTRAINT schema_version_version_check; UPDATE parking.schema_version SET version=2");
            try { Require(!await InitializeAsync(), "Bootstrap menerima versi schema berbeda."); }
            finally { await ExecuteAsync("UPDATE parking.schema_version SET version=1; ALTER TABLE parking.schema_version ADD CONSTRAINT schema_version_version_check CHECK(version=1)"); }
            Require(await InitializeAsync(), "Schema yang dipulihkan tidak kompatibel.");

            await using var runtime = new NpgsqlConnection(runtimeString);
            await runtime.OpenAsync();
            foreach (string sql in new[] { "DELETE FROM parking.parking_spots WHERE false", "TRUNCATE parking.deposits", "CREATE TABLE parking.forbidden_schema_check(id integer)", "UPDATE parking.schema_version SET version=1" })
            {
                bool denied = false;
                try
                {
                    await using var command = new NpgsqlCommand(sql, runtime);
                    await command.ExecuteNonQueryAsync();
                }
                catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.InsufficientPrivilege) { denied = true; }
                Require(denied, "Runtime menerima operasi terlarang: " + sql);
            }
        }
        finally
        {
            await using var cleanup = new NpgsqlCommand("DELETE FROM parking.parking_spots WHERE id=@id", owner);
            cleanup.Parameters.AddWithValue("id", spotId);
            await cleanup.ExecuteNonQueryAsync();
        }
        Console.WriteLine("PASS: schema idempotence, data preservation, incompatible type/constraint/version rejection, runtime grants.");
    }
}
