using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ParkingSystem.Database;
using ParkingSystem.Services;

namespace ParkingSystem;

internal static class Program
{
    /// <summary>
    /// Service provider global untuk dependency injection pada Windows Forms.
    /// </summary>
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();

        // Inisialisasi database schema & data awal via DbContextFactory
        using (var scope = ServiceProvider.CreateScope())
        {
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ParkingDbContext>>();
            using var db = factory.CreateDbContext();
            db.Database.EnsureCreated();
        }

        Application.Run(new Login());
    }

    /// <summary>
    /// Mengonfigurasi seluruh layanan, DbContextFactory, dan Form ke DI container.
    /// </summary>
    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContextFactory<ParkingDbContext>(options =>
        {
            options.UseInMemoryDatabase("ParkingSystemDb");
        });

        services.AddSingleton<IParkingService, ParkingService>();
        services.AddTransient<Login>();
        services.AddTransient<MainForm>();
    }
}
