using Microsoft.EntityFrameworkCore;

namespace ParkingSystem.Database;

/// <summary>
/// Penyiapan skema database saat aplikasi mulai.
/// <see cref="DbContext.Database.EnsureCreated()"/> hanya memeriksa apakah database sudah memiliki
/// tabel apa pun; bila database tujuan sudah terisi objek lain (misalnya schema sisa proyek lain),
/// pembuatan tabel dilewati tanpa pesan dan aplikasi gagal saat query pertama.
/// Kelas ini menutup celah tersebut dengan memverifikasi setiap tabel yang dibutuhkan benar-benar ada.
/// </summary>
public static class DatabaseInitializer
{
    /// <summary>Tabel yang wajib tersedia agar aplikasi dapat beroperasi.</summary>
    private static readonly string[] RequiredTables =
    [
        "Users", "Spots", "Transactions", "Deposits"
    ];

    /// <summary>
    /// Memastikan skema terbentuk dan seluruh tabel wajib tersedia.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Dilempar bila tabel wajib tidak ada, disertai petunjuk perbaikan yang dapat langsung dijalankan.
    /// </exception>
    public static void EnsureSchema(ParkingDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);

        db.Database.EnsureCreated();

        var missing = RequiredTables
            .Where(name => !TableExists(db, name))
            .ToList();

        if (missing.Count == 0)
            return;

        string database = db.Database.GetDbConnection().Database;

        throw new InvalidOperationException(
            $"Skema database '{database}' tidak lengkap. Tabel tidak ditemukan: {string.Join(", ", missing)}. " +
            "Penyebab umum: database tujuan sudah berisi tabel/schema lain sehingga EnsureCreated melewati pembuatan tabel. " +
            "Perbaikan: arahkan ke database kosong yang baru, misalnya dengan mengubah " +
            $"{DatabaseSettings.NameVariable} di berkas .env menjadi {database}_app");
    }

    /// <summary>
    /// Memeriksa keberadaan satu tabel melalui katalog PostgreSQL.
    /// </summary>
    private static bool TableExists(ParkingDbContext db, string tableName)
    {
        var connection = db.Database.GetDbConnection();
        bool openedHere = connection.State != System.Data.ConnectionState.Open;

        if (openedHere)
            connection.Open();

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText =
                "SELECT EXISTS (SELECT 1 FROM information_schema.tables " +
                "WHERE table_schema = current_schema() AND table_name = @name)";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@name";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);

            return command.ExecuteScalar() is true;
        }
        finally
        {
            if (openedHere)
                connection.Close();
        }
    }
}
