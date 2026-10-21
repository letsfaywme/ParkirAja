using System.Runtime.InteropServices;
using System.Text;
using Npgsql;

namespace ParkingSystem;

internal static class AdminBootstrap
{
    internal const long UsernameLock = 736192004;

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AttachConsole(uint processId);

    public static int Run() => RunAsync().GetAwaiter().GetResult();

    private static async Task<int> RunAsync()
    {
        AttachConsole(unchecked((uint)-1));
        try
        {
            Console.Write("Username: ");
            var username = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();
            Console.Write("Nama: ");
            var name = (Console.ReadLine() ?? "").Trim();
            Console.Write("Password (8–128 karakter): ");
            var password = new StringBuilder();
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter) break;
                if (key.Key == ConsoleKey.Backspace) { if (password.Length > 0) password.Length--; }
                else if (!char.IsControl(key.KeyChar)) password.Append(key.KeyChar);
            }
            Console.WriteLine();
            if (username.Length == 0 || name.Length == 0) throw new ArgumentException("Username dan nama wajib diisi.");
            var hash = PasswordHash.Hash(password.ToString());
            password.Clear();
            await using var source = NpgsqlDataSource.Create(DatabaseSettings.GetConnectionString());
            await using var connection = await source.OpenConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            await using (var command = new NpgsqlCommand("SELECT pg_advisory_xact_lock(@lock)", connection, transaction))
            {
                command.Parameters.AddWithValue("lock", UsernameLock);
                await command.ExecuteNonQueryAsync();
            }
            await using (var command = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM parking.admins), EXISTS(SELECT 1 FROM parking.admins WHERE username=@u UNION ALL SELECT 1 FROM parking.jukir WHERE username=@u)", connection, transaction))
            {
                command.Parameters.AddWithValue("u", username);
                await using var reader = await command.ExecuteReaderAsync();
                await reader.ReadAsync();
                if (reader.GetBoolean(0)) throw new InvalidOperationException("Admin sudah ada; bootstrap ditolak.");
                if (reader.GetBoolean(1)) throw new InvalidOperationException("Username sudah digunakan.");
            }
            await using (var command = new NpgsqlCommand("INSERT INTO parking.admins(username,name,password_hash,role) VALUES(@u,@n,@h,'admin')", connection, transaction))
            {
                command.Parameters.AddWithValue("u", username);
                command.Parameters.AddWithValue("n", name);
                command.Parameters.AddWithValue("h", hash);
                await command.ExecuteNonQueryAsync();
            }
            await transaction.CommitAsync();
            Console.WriteLine("Admin awal tersimpan.");
            return 0;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
        catch (Exception)
        {
            Console.Error.WriteLine("Bootstrap gagal. Periksa koneksi, schema, dan hak akses database.");
            return 1;
        }
    }
}
