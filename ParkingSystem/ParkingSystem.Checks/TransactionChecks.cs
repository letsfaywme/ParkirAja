using Npgsql;
using ParkingSystem;

internal static class TransactionChecks
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static async Task<TException> RejectAsync<TException>(Func<Task> action) where TException : Exception
    {
        try { await action(); }
        catch (TException ex) { return ex; }
        throw new InvalidOperationException("Expected rejection: " + typeof(TException).Name);
    }

    private static async Task<ParkingTransaction?> AttemptAsync(Func<Task<ParkingTransaction>> action)
    {
        try { return await action(); }
        catch (InvalidOperationException) { return null; }
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    public static async Task RunAsync()
    {
        if (string.IsNullOrEmpty(AuthChecks.OwnerConnectionString))
            throw new InvalidOperationException("AuthChecks.RunAsync must initialize isolated fixture first.");
        await using var source = NpgsqlDataSource.Create(DatabaseSettings.GetConnectionString());
        await using var owner = new NpgsqlConnection(AuthChecks.OwnerConnectionString);
        await owner.OpenAsync();
        var service = new ParkingService(source, TimeProvider.System);
        var a = AuthChecks.A;
        var b = AuthChecks.B;
        var c = AuthChecks.C;
        async Task ExecuteAsync(string sql, params (string Name, object Value)[] parameters)
        {
            await using var command = new NpgsqlCommand(sql, owner);
            foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Name, parameter.Value);
            await command.ExecuteNonQueryAsync();
        }
        async Task ResetAsync()
        {
            await ExecuteAsync("DELETE FROM parking.parking_transactions WHERE plate_number LIKE 'TCHK%'");
            await ExecuteAsync("UPDATE parking.parking_spots SET capacity=1,motorcycle_rate=2000,is_active=true WHERE id=@spot", ("spot", AuthChecks.SpotA));
            await ExecuteAsync("UPDATE parking.jukir SET is_active=true,parking_spot_id=@spot WHERE id=@id", ("spot", AuthChecks.SpotA), ("id", a.Id));
        }
        await ResetAsync();
        try
        {
            await RejectAsync<UnauthorizedAccessException>(() => service.EnterVehicleAsync(AuthChecks.Admin, "TCHKADMIN", VehicleType.Car, Ct));
            await RejectAsync<UnauthorizedAccessException>(() => service.GetLocationTransactionsAsync(a, AuthChecks.SpotB, Ct));
            await RejectAsync<UnauthorizedAccessException>(() => service.GetAssignedSpotAsync(AuthChecks.Admin, Ct));
            await RejectAsync<UnauthorizedAccessException>(() => service.EnterVehicleAsync(a with { ParkingSpotId = AuthChecks.SpotB }, "TCHKFORGED", VehicleType.Car, Ct));
            await RejectAsync<ArgumentException>(() => service.EnterVehicleAsync(a, "bad-plate!", VehicleType.Car, Ct));
            await RejectAsync<ArgumentException>(() => service.EnterVehicleAsync(a, "TCHKBADTYPE", (VehicleType)99, Ct));

            var duplicate = await Task.WhenAll(
                AttemptAsync(() => service.EnterVehicleAsync(a, " tchk same ", VehicleType.Motorcycle, Ct)),
                AttemptAsync(() => service.EnterVehicleAsync(a, "TCHKSAME", VehicleType.Motorcycle, Ct)));
            Require(duplicate.Count(x => x is not null) == 1, "Duplicate concurrent entry must have one winner.");
            var first = duplicate.Single(x => x is not null)!;
            Require(first.PlateNumber == "TCHKSAME" && first.Fare is null && first.ExitTime is null
                && first.ExitJukirId is null && first.Status == "active" && first.RateAtEntry == 2000,
                "Active transaction snapshot/null fields incorrect.");
            var globalDuplicate = await RejectAsync<InvalidOperationException>(() => service.EnterVehicleAsync(c, "TCHKSAME", VehicleType.Car, Ct));
            Require(globalDuplicate.Message == "Kendaraan masih terparkir.", "Duplicate must be global across locations.");
            Require(await service.FindActiveVehicleAsync(c, first.PlateNumber, Ct) is null, "Cross-location lookup leaked vehicle.");
            await RejectAsync<InvalidOperationException>(() => service.CompleteExitAsync(c, first.Id, Ct));
            Require(await service.FindActiveVehicleAsync(b, first.PlateNumber, Ct) is not null, "Shift partner cannot find active vehicle.");
            // Lookup/cancel writes nothing; UI confirmation is checked by native smoke harness.
            Require((await service.FindActiveVehicleAsync(b, first.PlateNumber, Ct))!.ExitTime is null, "Lookup changed transaction.");
            await ExecuteAsync("UPDATE parking.parking_spots SET motorcycle_rate=3000 WHERE id=@spot", ("spot", AuthChecks.SpotA));
            var exits = await Task.WhenAll(
                AttemptAsync(() => service.CompleteExitAsync(b, first.Id, Ct)),
                AttemptAsync(() => service.CompleteExitAsync(b, first.Id, Ct)));
            Require(exits.Count(x => x is not null) == 1, "Concurrent completion must have one winner.");
            var completed = exits.Single(x => x is not null)!;
            Require(completed.Fare == 2000 && completed.RateAtEntry == 2000 && completed.ExitJukirId == b.Id
                && completed.ExitTime >= completed.EntryTime && completed.Status == "completed", "Flat fare/receiver/clock incorrect.");
            Require(await service.FindActiveVehicleAsync(a, first.PlateNumber, Ct) is null, "Completed vehicle remained active.");
            await RejectAsync<InvalidOperationException>(() => service.CompleteExitAsync(a, first.Id, Ct));
            await ExecuteAsync("DELETE FROM parking.parking_transactions WHERE plate_number LIKE 'TCHK%'");

            var globalRace = await Task.WhenAll(
                AttemptAsync(() => service.EnterVehicleAsync(a, "TCHKGLOBAL", VehicleType.Motorcycle, Ct)),
                AttemptAsync(() => service.EnterVehicleAsync(c, "TCHKGLOBAL", VehicleType.Motorcycle, Ct)));
            Require(globalRace.Count(x => x is not null) == 1, "Global unique constraint allowed concurrent duplicate across locations.");
            var globalWinner = globalRace.Single(x => x is not null)!;
            await service.CompleteExitAsync(globalWinner.ParkingSpotId == AuthChecks.SpotA ? a : c, globalWinner.Id, Ct);
            await ExecuteAsync("DELETE FROM parking.parking_transactions WHERE plate_number LIKE 'TCHK%'");

            var capacity = await Task.WhenAll(
                AttemptAsync(() => service.EnterVehicleAsync(a, "TCHKCAP1", VehicleType.Car, Ct)),
                AttemptAsync(() => service.EnterVehicleAsync(b, "TCHKCAP2", VehicleType.Car, Ct)));
            Require(capacity.Count(x => x is not null) == 1, "Capacity concurrent entry exceeded one space.");
            var spot = await service.GetAssignedSpotAsync(a, Ct);
            Require(spot.Id == AuthChecks.SpotA && spot.Occupied == 1 && spot.Capacity == 1, "Assigned spot occupancy incorrect.");
            await service.CompleteExitAsync(b, capacity.Single(x => x is not null)!.Id, Ct);
            await ExecuteAsync("DELETE FROM parking.parking_transactions WHERE plate_number LIKE 'TCHK%'");

            foreach (string mutation in new[]
            {
                "UPDATE parking.jukir SET is_active=false WHERE id=@id",
                "UPDATE parking.jukir SET parking_spot_id=@other WHERE id=@id",
                "UPDATE parking.parking_spots SET is_active=false WHERE id=@spot"
            })
            {
                await ExecuteAsync(mutation, ("id", a.Id), ("other", AuthChecks.SpotB), ("spot", AuthChecks.SpotA));
                await RejectAsync<UnauthorizedAccessException>(() => service.ValidateSessionAsync(a, Ct));
                await RejectAsync<UnauthorizedAccessException>(() => service.EnterVehicleAsync(a, "TCHKSTALE", VehicleType.Car, Ct));
                await RejectAsync<UnauthorizedAccessException>(() => service.CompleteExitAsync(a, first.Id, Ct));
                await RejectAsync<UnauthorizedAccessException>(() => service.GetHistoryAsync(a, DateOnly.FromDateTime(DateTime.Today), null, null, 1, Ct));
                await ExecuteAsync("UPDATE parking.jukir SET is_active=true,parking_spot_id=@spot WHERE id=@id; UPDATE parking.parking_spots SET is_active=true WHERE id=@spot", ("spot", AuthChecks.SpotA), ("id", a.Id));
            }

            var day = new DateOnly(2020, 2, 2);
            var bounds = ParkingTime.Bounds(day, day);
            await ExecuteAsync("""
                INSERT INTO parking.parking_transactions(parking_spot_id,jukir_id,plate_number,vehicle_type,entry_time,exit_time,rate_at_entry,fare,status,exit_jukir_id)
                SELECT @spot,@a,'TCHKPAGE'||n,'motorcycle',@start+n*interval '1 minute',@start+n*interval '1 minute',2000,2000,'completed',@b
                FROM generate_series(0,50) n;
                INSERT INTO parking.parking_transactions(parking_spot_id,jukir_id,plate_number,vehicle_type,entry_time,exit_time,rate_at_entry,fare,status,exit_jukir_id)
                VALUES(@spot,@a,'TCHKPREVIOUS','car',@start-interval '1 second',@start-interval '1 second',5000,5000,'completed',@b);
                INSERT INTO parking.parking_transactions(parking_spot_id,jukir_id,plate_number,vehicle_type,entry_time,rate_at_entry)
                VALUES(@spot,@a,'TCHKACTIVE','car',@start+interval '1 second',5000);
                """, ("spot", AuthChecks.SpotA), ("a", a.Id), ("b", b.Id), ("start", bounds.From));
            await ExecuteAsync("UPDATE parking.parking_transactions SET entry_time=@before WHERE plate_number='TCHKPAGE0'", ("before", bounds.From.AddDays(-1)));
            var page1 = await service.GetHistoryAsync(a, day, null, null, 1, Ct);
            var page2 = await service.GetHistoryAsync(a, day, null, null, 2, Ct);
            Require(page1.Total == 52 && page1.Rows.Count == 50 && page2.Total == 52 && page2.Rows.Count == 2,
                "History pagination/total incorrect.");
            Require(page1.Rows.Concat(page2.Rows).Select(x => x.Id).Distinct().Count() == 52, "History page overlap.");
            Require((await service.GetHistoryAsync(a, day, null, null, 3, Ct)).Rows.Count == 0, "Beyond-last page must be empty.");
            Require((await service.GetHistoryAsync(a, day.AddDays(-1), null, null, 1, Ct)).Total == 1, "16:59:59 UTC must be previous WIB day.");
            Require((await service.GetHistoryAsync(a, day, VehicleType.Motorcycle, null, 1, Ct)).Total == 51, "Vehicle filter incorrect.");
            var platePage = await service.GetHistoryAsync(a, day, null, " tchk page0 ", 1, Ct);
            Require(platePage.Total == 1 && platePage.Rows[0].ExitTime!.Value.UtcDateTime == bounds.From
                && platePage.Rows[0].EntryTime.UtcDateTime == bounds.From.AddDays(-1), "Completed history must use payment date, including 17:00 UTC boundary.");
            Require((await service.GetHistoryAsync(b, day, null, null, 1, Ct)).Total == 51, "Receiver shift history missing.");
            Require((await service.GetHistoryAsync(c, day, null, null, 1, Ct)).Total == 0, "History cross-location leak.");
            await RejectAsync<ArgumentException>(() => service.GetHistoryAsync(a, day, null, null, 0, Ct));
            var dated = new ParkingService(source, new FixedTime(new DateTimeOffset(bounds.From.AddHours(5))));
            var dashA = await dated.GetDashboardAsync(a, Ct);
            var dashB = await dated.GetDashboardAsync(b, Ct);
            var dashAdmin = await dated.GetDashboardAsync(AuthChecks.Admin, Ct);
            Require(dashA.Entries == 51 && dashA.Exits == 0 && dashA.Revenue == 0, "Creator must not receive another shift's revenue.");
            Require(dashB.Entries == 0 && dashB.Exits == 51 && dashB.Revenue == 102000, "Receiver daily revenue incorrect.");
            Require(dashAdmin.Entries == 51 && dashAdmin.Exits == 51 && dashAdmin.Revenue == 102000
                && dashAdmin.Spots.Single(x => x.Id == AuthChecks.SpotA).Occupied == 1,
                "Admin dashboard totals include wrong day or active fare.");
            Require(dashA.Latest.Count <= 10 && dashA.Latest.All(x => x.ParkingSpotId == AuthChecks.SpotA), "Latest transaction scope incorrect.");
            var detail = await dated.GetLocationTransactionsAsync(AuthChecks.Admin, AuthChecks.SpotA, Ct);
            Require(detail.Count(x => x.PlateNumber.StartsWith("TCHK", StringComparison.Ordinal)) == 53,
                "Admin location detail omitted transactions.");
            int pendingDepositId;
            await using (var pendingDeposit = new NpgsqlCommand("INSERT INTO parking.deposits(jukir_id,deposit_date,amount_deposited,amount_expected) VALUES(@jukir,@date,2000,2000) RETURNING id", owner))
            {
                pendingDeposit.Parameters.AddWithValue("jukir", a.Id);
                pendingDeposit.Parameters.AddWithValue("date", new DateOnly(2020, 2, 1));
                pendingDepositId = (int)(await pendingDeposit.ExecuteScalarAsync(Ct))!;
            }
            await RejectAsync<UnauthorizedAccessException>(() => service.ValidateDepositAsync(a, pendingDepositId, null, Ct));
            await using (var pendingDepositState = new NpgsqlCommand("SELECT verification_status,admin_id FROM parking.deposits WHERE id=@id", owner))
            {
                pendingDepositState.Parameters.AddWithValue("id", pendingDepositId);
                await using var pendingDepositReader = await pendingDepositState.ExecuteReaderAsync(Ct);
                Require(await pendingDepositReader.ReadAsync(Ct) && pendingDepositReader.GetString(0) == "pending" && pendingDepositReader.IsDBNull(1),
                    "Jukir validation changed the pending deposit.");
            }
            var validatedDeposit = await service.ValidateDepositAsync(AuthChecks.Admin, pendingDepositId, "ERD authorization check", Ct);
            Require(validatedDeposit.Status == DepositStatus.Matched && validatedDeposit.AdminId == AuthChecks.Admin.Id,
                "Admin validation did not record the validating admin.");
            Console.WriteLine("PASS: transaction duplicate/capacity/exit concurrency, global plate, flat rate, shift receiver, stale sessions, WIB boundaries, history paging/filter/nulls, dashboard/detail scope, deposit validation authorization.");
        }
        finally { await ResetAsync(); }
        Require((await service.GetAssignedSpotAsync(a, Ct)) is { Occupied: 0, Capacity: 1, MotorcycleRate: 2000 },
            "Transaction fixture not restored for native UI smoke.");
    }
}
