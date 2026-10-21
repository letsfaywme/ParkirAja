using Npgsql;

namespace ParkingSystem
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length != 0)
            {
                Environment.ExitCode = args.Length == 1 && args[0] == "--create-admin" ? AdminBootstrap.Run() : 1;
                return;
            }
            NpgsqlDataSource? source = null;
            ParkingService? service = null;
            string? setupError = null;
            try
            {
                source = NpgsqlDataSource.Create(DatabaseSettings.GetConnectionString());
                service = new ParkingService(source, TimeProvider.System);
            }
            catch (InvalidOperationException ex) { setupError = ex.Message; }
            catch (ArgumentException) { setupError = "Konfigurasi koneksi tidak valid. Periksa PARKING_DB_CONNECTION."; }
            ApplicationConfiguration.Initialize();
            Application.SetColorMode(SystemColorMode.System);
            try { Application.Run(new Login(service, setupError)); }
            finally { source?.Dispose(); }
        }
    }
}