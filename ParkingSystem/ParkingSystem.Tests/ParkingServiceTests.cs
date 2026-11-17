using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Database;
using ParkingSystem.Services;
using Xunit;

namespace ParkingSystem.Tests;

public class ParkingServiceTests
{
    private class TestDbContextFactory : IDbContextFactory<ParkingDbContext>
    {
        private readonly DbContextOptions<ParkingDbContext> _options;

        public TestDbContextFactory(string dbName)
        {
            _options = new DbContextOptionsBuilder<ParkingDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            using var db = new ParkingDbContext(_options);
            db.Database.EnsureCreated();
        }

        public ParkingDbContext CreateDbContext() => new(_options);
        public Task<ParkingDbContext> CreateDbContextAsync(CancellationToken ct = default) => Task.FromResult(new ParkingDbContext(_options));
    }

    private static (IParkingService service, IDbContextFactory<ParkingDbContext> factory) CreateIsolatedService()
    {
        string uniqueDbName = "TestDb_" + Guid.NewGuid();
        var factory = new TestDbContextFactory(uniqueDbName);
        var service = new ParkingService(factory);
        return (service, factory);
    }

    [Fact]
    public async Task AuthenticateAsync_WithValidCredentials_ShouldReturnUserSession()
    {
        // Arrange
        var (service, _) = CreateIsolatedService();

        // Act
        var adminSession = await service.AuthenticateAsync("admin", "adminpassword");
        var jukirSession = await service.AuthenticateAsync("jukir1", "jukirpassword");

        // Assert
        adminSession.Should().NotBeNull();
        adminSession!.Role.Should().Be(UserRole.Admin);
        adminSession.Username.Should().Be("admin");

        jukirSession.Should().NotBeNull();
        jukirSession!.Role.Should().Be(UserRole.Jukir);
        jukirSession.ParkingSpotId.Should().Be(1);
    }

    [Fact]
    public async Task AuthenticateAsync_WithInvalidPassword_ShouldReturnNull()
    {
        // Arrange
        var (service, _) = CreateIsolatedService();

        // Act
        var session = await service.AuthenticateAsync("admin", "wrong_password");

        // Assert
        session.Should().BeNull();
    }

    [Fact]
    public async Task CreateEntryAsync_WithValidPlate_ShouldCreateActiveTicket()
    {
        // Arrange
        var (service, _) = CreateIsolatedService();
        string plate = "B 1234 XYZ";

        // Act
        var (success, msg, tx) = await service.CreateEntryAsync(1, 2, plate, VehicleType.Motorcycle);

        // Assert
        success.Should().BeTrue();
        tx.Should().NotBeNull();
        tx!.PlateNumber.Should().Be(plate);
        tx.Status.Should().Be("ACTIVE");
        tx.RateAtEntry.Should().Be(2000m);
        tx.Id.Should().BePositive();
    }

    [Fact]
    public async Task CreateEntryAsync_WithDuplicateActivePlate_ShouldBeRejected()
    {
        // Arrange
        var (service, _) = CreateIsolatedService();
        string plate = "B 8888 DUP";

        // First Entry
        var (firstSuccess, _, _) = await service.CreateEntryAsync(1, 2, plate, VehicleType.Car);
        firstSuccess.Should().BeTrue();

        // Second Entry with same plate while still active
        var (secondSuccess, msg, secondTx) = await service.CreateEntryAsync(1, 2, plate, VehicleType.Car);

        // Assert
        secondSuccess.Should().BeFalse();
        secondTx.Should().BeNull();
        msg.Should().Contain("masih tercatat aktif");
    }

    [Fact]
    public async Task CreateEntryAsync_WithInvalidPlateFormat_ShouldBeRejected()
    {
        // Arrange
        var (service, _) = CreateIsolatedService();

        // Act
        var (success, msg, tx) = await service.CreateEntryAsync(1, 2, "INVALID123", VehicleType.Motorcycle);

        // Assert
        success.Should().BeFalse();
        tx.Should().BeNull();
        msg.Should().Contain("Format nomor plat");
    }

    [Fact]
    public async Task CreateEntryAsync_WhenAreaCapacityIsFull_ShouldBeRejected()
    {
        // Arrange
        var (service, factory) = CreateIsolatedService();
        await using (var db = await factory.CreateDbContextAsync())
        {
            var spot = await db.Spots.FirstAsync(s => s.Id == 1);
            spot.Occupied = spot.Capacity; // Penuhkan kapasitas
            await db.SaveChangesAsync();
        }

        // Act
        var (success, msg, tx) = await service.CreateEntryAsync(1, 2, "B 5555 FUL", VehicleType.Motorcycle);

        // Assert
        success.Should().BeFalse();
        tx.Should().BeNull();
        msg.Should().Contain("penuh");
    }

    [Fact]
    public async Task CalculateExitAsync_ShouldComputeCeilingDurationAndFare()
    {
        // Arrange
        var (service, _) = CreateIsolatedService();
        string plate = "B 9999 CAL";
        await service.CreateEntryAsync(1, 2, plate, VehicleType.Motorcycle);

        // Act
        var (tx, fare) = await service.CalculateExitAsync(plate);

        // Assert
        tx.Should().NotBeNull();
        fare.Should().BeGreaterThanOrEqualTo(2000m, "Minimal tarif adalah 1 jam (Rp 2.000)");
    }

    [Fact]
    public async Task ProcessExitAsync_ShouldTransitionTicketToCompletedAndDecrementOccupied()
    {
        // Arrange
        var (service, factory) = CreateIsolatedService();
        string plate = "B 7777 EXT";
        var (_, _, entryTx) = await service.CreateEntryAsync(1, 2, plate, VehicleType.Motorcycle);
        entryTx.Should().NotBeNull();

        int initialOccupied;
        await using (var db = await factory.CreateDbContextAsync())
        {
            initialOccupied = (await db.Spots.FirstAsync(s => s.Id == 1)).Occupied;
        }

        // Act
        bool exitOk = await service.ProcessExitAsync(entryTx!.Id, 2, 2000m);

        // Assert
        exitOk.Should().BeTrue();

        await using (var db = await factory.CreateDbContextAsync())
        {
            var updatedTx = await db.Transactions.FirstAsync(t => t.Id == entryTx.Id);
            updatedTx.Status.Should().Be("COMPLETED");
            updatedTx.Fare.Should().Be(2000m);
            updatedTx.ExitTime.Should().NotBeNull();
            updatedTx.ExitJukirId.Should().Be(2);

            var spot = await db.Spots.FirstAsync(s => s.Id == 1);
            spot.Occupied.Should().Be(initialOccupied - 1);
        }
    }

    [Fact]
    public async Task ValidateDepositAsync_ShouldUpdateStatusAndNotes()
    {
        // Arrange
        var (service, factory) = CreateIsolatedService();
        var deposit = new DailyDeposit
        {
            JukirId = 2,
            JukirName = "Budi Santoso",
            ParkingSpotId = 1,
            ParkingSpotName = "Area Parkir Utama",
            ReportDate = DateOnly.FromDateTime(DateTime.Today),
            ExpectedAmount = 50000m,
            DepositedAmount = 50000m,
            Status = DepositStatus.Pending
        };
        await service.AddDepositAsync(deposit);

        // Act
        bool result = await service.ValidateDepositAsync(deposit.Id, 1, DepositStatus.Matched, "Setoran valid");

        // Assert
        result.Should().BeTrue();
        await using (var db = await factory.CreateDbContextAsync())
        {
            var updated = await db.Deposits.FirstAsync(d => d.Id == deposit.Id);
            updated.Status.Should().Be(DepositStatus.Matched);
            updated.Notes.Should().Be("Setoran valid");
            updated.ValidatedBy.Should().Be(1);
            updated.ValidatedAt.Should().NotBeNull();
        }
    }
}
