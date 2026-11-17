using FluentAssertions;
using ParkingSystem.Database;
using Xunit;

namespace ParkingSystem.Tests;

public class InMemoryDbEntryCrudTests
{
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
