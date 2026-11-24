using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Database;
using Xunit;

namespace ParkingSystem.Tests;

/// <summary>
/// Pengujian CRUD tiket pada fasad <see cref="InMemoryDb"/>.
/// Setiap test memasang DbContext EF Core InMemory yang terisolasi sehingga
/// tidak menyentuh database PostgreSQL pengembangan.
/// </summary>
public class InMemoryDbEntryCrudTests : IDisposable
{
    private readonly ParkingDbContext _db;

    public InMemoryDbEntryCrudTests()
    {
        var options = new DbContextOptionsBuilder<ParkingDbContext>()
            .UseInMemoryDatabase("CrudTest_" + Guid.NewGuid())
            .Options;

        _db = new ParkingDbContext(options);
        _db.Database.EnsureCreated();
        ParkingDbContext.UseInstanceForTests(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void UpdateEntry_ChangesPlateAndRate()
    {
        var (success, _, tx) = InMemoryDb.CreateEntry(1, 2, "B 4471 AAA", VehicleType.Motorcycle);
        success.Should().BeTrue();

        var (updated, message) = InMemoryDb.UpdateEntry(tx!.Id, "B 4471 BBB", VehicleType.Car, 1);

        updated.Should().BeTrue();
        message.Should().Be("Tiket berhasil diperbarui.");
        tx.PlateNumber.Should().Be("B 4471 BBB");
        tx.RateAtEntry.Should().Be(5000m);
    }

    [Fact]
    public void UpdateEntry_WithDuplicateActivePlate_IsRejected()
    {
        InMemoryDb.CreateEntry(1, 2, "B 4472 AAA", VehicleType.Car).Success.Should().BeTrue();
        var (_, _, tx) = InMemoryDb.CreateEntry(1, 2, "B 4472 BBB", VehicleType.Car);

        var (updated, message) = InMemoryDb.UpdateEntry(tx!.Id, "B 4472 AAA", VehicleType.Car, 1);

        updated.Should().BeFalse();
        message.Should().Contain("masih tercatat aktif");
    }

    [Fact]
    public void DeleteEntry_FreesOccupiedSlot()
    {
        int before = InMemoryDb.Spots.First(s => s.Id == 2).Occupied;
        var (_, _, tx) = InMemoryDb.CreateEntry(2, 3, "B 4473 AAA", VehicleType.Motorcycle);
        InMemoryDb.Spots.First(s => s.Id == 2).Occupied.Should().Be(before + 1);

        var (deleted, _) = InMemoryDb.DeleteEntry(tx!.Id);

        deleted.Should().BeTrue();
        InMemoryDb.Spots.First(s => s.Id == 2).Occupied.Should().Be(before);
    }

    [Fact]
    public void ExitFlow_CalculatesFareAndReleasesSlot()
    {
        var (_, _, tx) = InMemoryDb.CreateEntry(1, 2, "B 4480 EXI", VehicleType.Car);
        tx.Should().NotBeNull();

        int occupiedAfterEntry = InMemoryDb.Spots.First(s => s.Id == 1).Occupied;

        var (found, fare) = InMemoryDb.CalculateExit("B 4480 EXI");

        found.Should().NotBeNull();
        fare.Should().Be(found!.RateAtEntry, "durasi di bawah 1 jam dibulatkan ke 1 jam");

        InMemoryDb.ProcessExit(found.Id, 2, fare).Should().BeTrue();

        found.Status.Should().Be("COMPLETED");
        found.Fare.Should().Be(fare);
        found.ExitTime.Should().NotBeNull();
        InMemoryDb.Spots.First(s => s.Id == 1).Occupied
            .Should().Be(occupiedAfterEntry - 1, "slot dilepas saat checkout");
    }

    [Fact]
    public void CalculateExit_ForUnknownPlate_ReturnsNull()
    {
        var (tx, fare) = InMemoryDb.CalculateExit("B 0000 NUN");

        tx.Should().BeNull();
        fare.Should().Be(0m);
    }

    [Fact]
    public void DeleteEntry_OnCompletedTicket_IsRejected()
    {
        var (_, _, tx) = InMemoryDb.CreateEntry(2, 3, "B 4474 AAA", VehicleType.Motorcycle);
        tx!.Status = "COMPLETED";
        InMemoryDb.SaveChanges();

        var (deleted, message) = InMemoryDb.DeleteEntry(tx.Id);

        deleted.Should().BeFalse();
        message.Should().Be("Tiket tidak ditemukan atau sudah selesai.");
    }
}
