using ParkingSystem.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ParkingSystem.Database;

/// <summary>
/// Entity Framework Core DbContext untuk sistem parkir.
/// Memetakan entitas ke PostgreSQL (Npgsql) dan mengisi data awal (seed).
/// Seluruh kolom <see cref="DateTimeOffset"/> disimpan sebagai UTC lalu dibaca kembali
/// dalam zona waktu WIB (UTC+7) agar tampilan antarmuka konsisten.
/// </summary>
public class ParkingDbContext : DbContext
{
    /// <summary>Offset zona waktu Indonesia Barat (WIB, UTC+7).</summary>
    public static readonly TimeSpan WibOffset = TimeSpan.FromHours(7);

    /// <summary>
    /// Konverter kolom waktu: tulis sebagai UTC (syarat Npgsql untuk <c>timestamptz</c>),
    /// baca kembali sebagai WIB agar seluruh tampilan UI memakai waktu lokal.
    /// </summary>
    private static readonly ValueConverter<DateTimeOffset, DateTimeOffset> WibConverter =
        new(v => v.ToUniversalTime(), v => v.ToOffset(WibOffset));

    /// <summary>Varian nullable dari <see cref="WibConverter"/> untuk kolom waktu opsional.</summary>
    private static readonly ValueConverter<DateTimeOffset?, DateTimeOffset?> NullableWibConverter =
        new(v => v == null ? null : v.Value.ToUniversalTime(),
            v => v == null ? null : v.Value.ToOffset(WibOffset));

    private static ParkingDbContext? _instance;
    private static readonly object _lock = new();

    /// <summary>
    /// Singleton instance DbContext agar seluruh form berbagi satu koneksi state aplikasi.
    /// </summary>
    public static ParkingDbContext Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        var options = new DbContextOptionsBuilder<ParkingDbContext>()
                            .UseNpgsql(DatabaseSettings.GetConnectionString())
                            .Options;
                        var created = new ParkingDbContext(options);
                        DatabaseInitializer.EnsureSchema(created);
                        _instance = created;
                    }
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// Mengganti singleton dengan konteks terisolasi untuk pengujian, sehingga test tidak
    /// menyentuh database PostgreSQL pengembangan.
    /// </summary>
    internal static void UseInstanceForTests(ParkingDbContext db)
    {
        lock (_lock)
        {
            _instance = db;
        }
    }

    /// <summary>
    /// Konstruktor menerima opsi konfigurasi database dari DbContextOptions.
    /// </summary>
    public ParkingDbContext(DbContextOptions<ParkingDbContext> options) : base(options)
    {
    }

    /// <summary>Tabel Pengguna (Admin &amp; Jukir) - Sesuai tabel ERD Admins &amp; Jukir</summary>
    public DbSet<AppUser> Users => Set<AppUser>();

    /// <summary>Tabel Titik Area Parkir - Sesuai tabel ERD Parking Spots</summary>
    public DbSet<ParkingSpot> Spots => Set<ParkingSpot>();

    /// <summary>Tabel Transaksi Parkir - Sesuai tabel ERD Parking Transaction</summary>
    public DbSet<ParkingTransaction> Transactions => Set<ParkingTransaction>();

    /// <summary>Tabel Setoran Harian Jukir - Sesuai tabel ERD Deposits</summary>
    public DbSet<DailyDeposit> Deposits => Set<DailyDeposit>();

    /// <summary>
    /// Mengonfigurasi pemetaan entitas ke skema database (Fluent API) dan mengisi data awal (seed).
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Konfigurasi tabel ParkingSpot (Primary Key, panjang string, dan presisi desimal)
        modelBuilder.Entity<ParkingSpot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Address).HasMaxLength(200);
            entity.Property(e => e.MotorcycleRate).HasPrecision(12, 2);
            entity.Property(e => e.CarRate).HasPrecision(12, 2);
        });

        // Konfigurasi tabel AppUser (Primary Key, username unik, relasi ke ParkingSpot)
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Role).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(e => e.ParkingSpot)
                .WithMany(s => s.Jukirs)
                .HasForeignKey(e => e.ParkingSpotId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Konfigurasi tabel ParkingTransaction (Primary Key, relasi ke ParkingSpot & AppUser, presisi biaya)
        modelBuilder.Entity<ParkingTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PlateNumber, e.Status });
            entity.Property(e => e.PlateNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.LocationName).HasMaxLength(100);
            entity.Property(e => e.JukirName).HasMaxLength(100);
            entity.Property(e => e.ExitJukirName).HasMaxLength(100);
            entity.Property(e => e.VehicleType).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.RateAtEntry).HasPrecision(12, 2);
            entity.Property(e => e.Fare).HasPrecision(12, 2);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.EntryTime).HasConversion(WibConverter);
            entity.Property(e => e.ExitTime).HasConversion(NullableWibConverter);

            entity.HasOne(e => e.ParkingSpot)
                .WithMany(s => s.Transactions)
                .HasForeignKey(e => e.ParkingSpotId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Jukir)
                .WithMany(u => u.EntryTransactions)
                .HasForeignKey(e => e.JukirId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ExitJukir)
                .WithMany(u => u.ExitTransactions)
                .HasForeignKey(e => e.ExitJukirId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Konfigurasi tabel DailyDeposit (Primary Key, relasi ke Jukir, ParkingSpot, Validator)
        modelBuilder.Entity<DailyDeposit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.JukirName).HasMaxLength(100);
            entity.Property(e => e.ParkingSpotName).HasMaxLength(100);
            entity.Property(e => e.ExpectedAmount).HasPrecision(12, 2);
            entity.Property(e => e.DepositedAmount).HasPrecision(12, 2);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.ValidatedAt).HasConversion(NullableWibConverter);

            entity.HasOne(e => e.Jukir)
                .WithMany(u => u.Deposits)
                .HasForeignKey(e => e.JukirId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ParkingSpot)
                .WithMany(s => s.Deposits)
                .HasForeignKey(e => e.ParkingSpotId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Validator)
                .WithMany(u => u.ValidatedDeposits)
                .HasForeignKey(e => e.ValidatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Panggil pengisian data awal
        SeedData(modelBuilder);
    }

    /// <summary>
    /// Mengisi data dummy awal untuk area parkir, pengguna demo, transaksi sampel, dan setoran awal.
    /// </summary>
    private static void SeedData(ModelBuilder modelBuilder)
    {
        // 1. Seed data titik area parkir
        modelBuilder.Entity<ParkingSpot>().HasData(
            new ParkingSpot
            {
                Id = 1,
                Name = "Area Parkir Utama",
                Address = "Jl. Merdeka No. 1, Jakarta Pusat",
                MotorcycleRate = 2000,
                CarRate = 5000,
                Capacity = 100,
                IsActive = true,
                Occupied = 2
            },
            new ParkingSpot
            {
                Id = 2,
                Name = "Area Parkir Timur",
                Address = "Jl. Pemuda No. 45, Jakarta Timur",
                MotorcycleRate = 2000,
                CarRate = 4000,
                Capacity = 50,
                IsActive = true,
                Occupied = 1
            }
        );

        // 2. Seed data akun pengguna (Admin dan Juru Parkir)
        modelBuilder.Entity<AppUser>().HasData(
            new AppUser
            {
                Id = 1,
                Username = "admin",
                PasswordHash = PasswordHasher.HashPassword("adminpassword"),
                Name = "Administrator Sistem",
                Role = UserRole.Admin,
                ParkingSpotId = null,
                IsActive = true
            },
            new AppUser
            {
                Id = 2,
                Username = "jukir1",
                PasswordHash = PasswordHasher.HashPassword("jukirpassword"),
                Name = "Budi Santoso",
                Role = UserRole.Jukir,
                ParkingSpotId = 1,
                IsActive = true
            },
            new AppUser
            {
                Id = 3,
                Username = "jukir2",
                PasswordHash = PasswordHasher.HashPassword("jukirpassword"),
                Name = "Siti Rahma",
                Role = UserRole.Jukir,
                ParkingSpotId = 2,
                IsActive = true
            }
        );

        // 3. Seed data contoh transaksi aktif dan selesai
        var now = new DateTimeOffset(2026, 10, 27, 8, 0, 0, TimeSpan.FromHours(7));
        modelBuilder.Entity<ParkingTransaction>().HasData(
            new ParkingTransaction
            {
                Id = 1,
                ParkingSpotId = 1,
                LocationName = "Area Parkir Utama",
                JukirId = 2,
                JukirName = "Budi Santoso",
                PlateNumber = "B 1234 ABC",
                VehicleType = VehicleType.Motorcycle,
                EntryTime = now.AddHours(-2),
                ExitTime = now.AddHours(-1),
                RateAtEntry = 2000,
                Fare = 2000,
                Status = "COMPLETED",
                ExitJukirId = 2,
                ExitJukirName = "Budi Santoso"
            },
            new ParkingTransaction
            {
                Id = 2,
                ParkingSpotId = 1,
                LocationName = "Area Parkir Utama",
                JukirId = 2,
                JukirName = "Budi Santoso",
                PlateNumber = "B 5678 DEF",
                VehicleType = VehicleType.Car,
                EntryTime = now.AddHours(-3),
                ExitTime = now.AddHours(-1),
                RateAtEntry = 5000,
                Fare = 10000,
                Status = "COMPLETED",
                ExitJukirId = 2,
                ExitJukirName = "Budi Santoso"
            },
            new ParkingTransaction
            {
                Id = 3,
                ParkingSpotId = 1,
                LocationName = "Area Parkir Utama",
                JukirId = 2,
                JukirName = "Budi Santoso",
                PlateNumber = "B 9999 XYZ",
                VehicleType = VehicleType.Car,
                EntryTime = now.AddHours(-1.5),
                ExitTime = null,
                RateAtEntry = 5000,
                Fare = null,
                Status = "ACTIVE"
            },
            new ParkingTransaction
            {
                Id = 4,
                ParkingSpotId = 1,
                LocationName = "Area Parkir Utama",
                JukirId = 2,
                JukirName = "Budi Santoso",
                PlateNumber = "B 3321 KMN",
                VehicleType = VehicleType.Motorcycle,
                EntryTime = now.AddMinutes(-40),
                ExitTime = null,
                RateAtEntry = 2000,
                Fare = null,
                Status = "ACTIVE"
            }
        );

        // 4. Seed data contoh setoran harian menunggu verifikasi
        modelBuilder.Entity<DailyDeposit>().HasData(
            new DailyDeposit
            {
                Id = 1,
                JukirId = 2,
                JukirName = "Budi Santoso",
                ParkingSpotId = 1,
                ParkingSpotName = "Area Parkir Utama",
                ReportDate = new DateOnly(2026, 10, 27),
                ExpectedAmount = 12000,
                DepositedAmount = 12000,
                Status = DepositStatus.Pending,
                Notes = "Setoran tepat"
            }
        );
    }
}
