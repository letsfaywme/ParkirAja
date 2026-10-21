namespace ParkingSystem;

public static class DatabaseSettings
{
    public static string GetConnectionString()
    {
        var value = Environment.GetEnvironmentVariable("PARKING_DB_CONNECTION");
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("Koneksi belum disiapkan. Atur PARKING_DB_CONNECTION dan jalankan Initialize-Database.ps1.");
        return value;
    }
}
