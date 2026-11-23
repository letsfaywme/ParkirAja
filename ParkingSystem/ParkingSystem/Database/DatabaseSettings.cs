using dotenv.net;
using Npgsql;

namespace ParkingSystem.Database;

/// <summary>
/// Sumber tunggal konfigurasi koneksi PostgreSQL, mengikuti pola proyek UangKu:
/// nilai dibaca dari berkas <c>.env</c> (di-gitignore) atau dari variabel lingkungan.
/// Urutan prioritas: <c>DB_CONNECTION_STRING</c> utuh, lalu komponen
/// <c>DB_HOST</c> / <c>DB_PORT</c> / <c>DB_NAME</c> / <c>DB_USER</c> / <c>DB_PASSWORD</c>.
/// </summary>
public static class DatabaseSettings
{
    /// <summary>Connection string utuh bila ingin menimpa seluruh komponen.</summary>
    public const string ConnectionStringVariable = "DB_CONNECTION_STRING";

    /// <summary>Nama variabel komponen koneksi.</summary>
    public const string HostVariable = "DB_HOST";
    public const string PortVariable = "DB_PORT";
    public const string NameVariable = "DB_NAME";
    public const string UserVariable = "DB_USER";
    public const string PasswordVariable = "DB_PASSWORD";

    private static string? _connectionString;
    private static readonly object _lock = new();

    /// <summary>
    /// Connection string aktif. Dimuat sekali lalu di-cache.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Dilempar bila password tidak tersedia sama sekali, agar kegagalan konfigurasi
    /// terlihat jelas alih-alih menghasilkan koneksi anonim.
    /// </exception>
    public static string ConnectionString
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                lock (_lock)
                {
                    _connectionString ??= LoadConnectionString();
                }
            }
            return _connectionString;
        }
    }

    /// <summary>Mengembalikan connection string aktif (alias untuk kompatibilitas pemanggil lama).</summary>
    public static string GetConnectionString() => ConnectionString;

    /// <summary>
    /// Membaca berkas <c>.env</c> lalu menyusun connection string dari variabel lingkungan.
    /// </summary>
    private static string LoadConnectionString()
    {
        LoadEnvFile();

        string? direct = Environment.GetEnvironmentVariable(ConnectionStringVariable);
        if (!string.IsNullOrWhiteSpace(direct))
            return direct;

        string host = Environment.GetEnvironmentVariable(HostVariable) ?? "localhost";
        string portText = Environment.GetEnvironmentVariable(PortVariable) ?? "5432";
        string database = Environment.GetEnvironmentVariable(NameVariable) ?? "parkiraja";
        string user = Environment.GetEnvironmentVariable(UserVariable) ?? "postgres";
        string? password = Environment.GetEnvironmentVariable(PasswordVariable);

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Password database tidak ditemukan. Buat berkas .env di folder proyek berisi " +
                $"{PasswordVariable}=... atau isi {ConnectionStringVariable} secara utuh. " +
                "Contoh: lihat .env.example.");
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = int.TryParse(portText, out int port) ? port : 5432,
            Database = database,
            Username = user,
            Password = password
        };

        return builder.ConnectionString;
    }

    /// <summary>
    /// Memuat <c>.env</c> dari direktori kerja, direktori aplikasi, atau leluhurnya.
    /// Nilai yang sudah ada di lingkungan tidak ditimpa.
    /// </summary>
    private static void LoadEnvFile()
    {
        var candidates = new List<string>
        {
            Path.Combine(AppContext.BaseDirectory, ".env"),
            Path.Combine(Directory.GetCurrentDirectory(), ".env")
        };

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            candidates.Add(Path.Combine(dir.FullName, ".env"));
            dir = dir.Parent;
        }

        string? envPath = candidates.FirstOrDefault(File.Exists);

        if (envPath != null)
        {
            DotEnv.Load(new DotEnvOptions(
                envFilePaths: [envPath],
                ignoreExceptions: true
            ));
            ParseEnvFileManually(envPath);
        }
        else
        {
            DotEnv.Load(new DotEnvOptions(
                ignoreExceptions: true,
                probeForEnv: true,
                probeLevelsToSearch: 6
            ));
        }
    }

    /// <summary>
    /// Pembaca <c>.env</c> cadangan: menangani baris komentar, tanda kutip, dan spasi.
    /// Variabel lingkungan yang sudah terisi tidak ditimpa.
    /// </summary>
    private static void ParseEnvFileManually(string envPath)
    {
        try
        {
            foreach (string rawLine in File.ReadAllLines(envPath))
            {
                string line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#') || line.StartsWith(';'))
                    continue;

                int separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                    continue;

                string key = line[..separatorIndex].Trim();
                string value = line[(separatorIndex + 1)..].Trim();

                if (value.Length >= 2 &&
                    ((value.StartsWith('"') && value.EndsWith('"')) ||
                     (value.StartsWith('\'') && value.EndsWith('\''))))
                {
                    value = value[1..^1];
                }

                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                    Environment.SetEnvironmentVariable(key, value);
            }
        }
        catch (IOException)
        {
            // Berkas tidak terbaca: variabel lingkungan yang sudah ada tetap dipakai.
        }
        catch (UnauthorizedAccessException)
        {
            // Idem.
        }
    }
}
