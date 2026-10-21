using Npgsql;
using ParkingSystem;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    Console.WriteLine("PASS " + message);
}
var password = Guid.NewGuid().ToString("N");
var hash = PasswordHash.Hash(password);
Check(PasswordHash.Verify(password, hash), "PBKDF2 password benar");
Check(!PasswordHash.Verify("wrong-password", hash), "PBKDF2 password salah");
Check(!PasswordHash.Verify(password, "pbkdf2-sha256$600000$bad$bad"), "PBKDF2 malformed ditolak");
var configured = Environment.GetEnvironmentVariable("PARKING_DB_CONNECTION");
try
{
    Environment.SetEnvironmentVariable("PARKING_DB_CONNECTION", null);
    try { DatabaseSettings.GetConnectionString(); throw new Exception("missing connection accepted"); }
    catch (InvalidOperationException) { Console.WriteLine("PASS missing connection ditolak"); }
}
finally { Environment.SetEnvironmentVariable("PARKING_DB_CONNECTION", configured); }
using var source = NpgsqlDataSource.Create(DatabaseSettings.GetConnectionString());
await using var connection = await source.OpenConnectionAsync();
await using var version = new NpgsqlCommand("SELECT version FROM parking.schema_version", connection);
Check(Convert.ToInt32(await version.ExecuteScalarAsync()) == 1, "schema versi 1");
await using var privileges = new NpgsqlCommand("SELECT NOT rolsuper AND NOT rolcreatedb AND NOT rolcreaterole AND NOT rolbypassrls FROM pg_roles WHERE rolname=current_user", connection);
Check((bool)(await privileges.ExecuteScalarAsync())!, "runtime bukan privileged role");
foreach (var sql in new[] { "DELETE FROM parking.admins", "TRUNCATE parking.admins", "CREATE TABLE parking.forbidden(id integer)" })
{
    try
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
        throw new Exception("Runtime privilege terlalu luas: " + sql);
    }
    catch (PostgresException ex) when (ex.SqlState == "42501")
    {
        Console.WriteLine("PASS runtime privilege denied: " + sql);
    }
}
Console.WriteLine("Persistence checks selesai.");
await SchemaChecks.RunAsync();
await AuthChecks.RunAsync();
await TransactionChecks.RunAsync();
await ManagementChecks.RunAsync(new ParkingService(NpgsqlDataSource.Create(DatabaseSettings.GetConnectionString()), TimeProvider.System), AuthChecks.Admin);
await PdfChecks.RunAsync();
