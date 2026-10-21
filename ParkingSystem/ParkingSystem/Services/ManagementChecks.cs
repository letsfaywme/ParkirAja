namespace ParkingSystem;

/// <summary>Focused, database-backed management invariants for the standalone business harness.</summary>
public static class ManagementChecks
{
    public static async Task RunAsync(ParkingService service, UserSession admin, CancellationToken cancellationToken = default)
    {
        if (admin.Role != UserRole.Admin) throw new InvalidOperationException("Management checks require an admin session.");
        var before = await service.GetParkingSpotsAsync(admin, cancellationToken);
        const string marker = "Management Check Location";
        var existing = before.FirstOrDefault(x => x.Name == marker);
        var spot = await service.SaveParkingSpotAsync(admin, existing?.Id, marker, "Test address", 2000m, 5000m, 2, cancellationToken);
        if (spot.Capacity != 2 || spot.MotorcycleRate != 2000m || spot.CarRate != 5000m) throw new InvalidOperationException("Parking spot persistence check failed.");
        var username = "management-check-jukir";
        var accounts = await service.GetJukirAsync(admin, cancellationToken);
        var account = accounts.FirstOrDefault(x => x.Username == username);
        account = await service.SaveJukirAsync(admin, account?.Id, "Management Check", username, account is null ? "Management-Check-123" : null, spot.Id, cancellationToken);
        var reloaded = (await service.GetJukirAsync(admin, cancellationToken)).First(x => x.Id == account.Id);
        if (reloaded.ParkingSpotId != spot.Id || reloaded.Username != username) throw new InvalidOperationException("Jukir persistence check failed.");
        // Cleanup is intentionally conservative: active accounts/locations cannot be removed by this check.
        // Callers may deactivate these records after ensuring no fixture transactions or pending deposits remain.
    }
}
