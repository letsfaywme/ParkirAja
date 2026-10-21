using Npgsql;
using NpgsqlTypes;

namespace ParkingSystem;

public sealed record DailyReportSummary(DateOnly Date, decimal AmountExpected, decimal? AmountDeposited, decimal? Discrepancy, DepositStatus? Status, bool HasTransactions, int TransactionCount, int MotorcycleCount = 0, decimal MotorcycleRevenue = 0, int CarCount = 0, decimal CarRevenue = 0);
public sealed record DepositRecord(int Id, int JukirId, string JukirName, DateOnly Date, decimal AmountDeposited, decimal AmountExpected, decimal Discrepancy, DepositStatus Status, int? AdminId, string? AdminName, string? Notes);

public sealed partial class ParkingService
{
    private static string DepositStatusDb(DepositStatus status) => status switch
    {
        DepositStatus.Pending => "pending", DepositStatus.Matched => "matched", DepositStatus.NeedReview => "need_review",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };
    private static DepositStatus DepositStatusValue(string value) => value switch
    {
        "pending" => DepositStatus.Pending, "matched" => DepositStatus.Matched, "need_review" => DepositStatus.NeedReview,
        _ => throw new InvalidOperationException("Status setoran tidak valid.")
    };
    private async Task AuthorizeDepositReadAsync(UserSession session, CancellationToken ct) => await AuthorizeAsync(session, session.Role, ct);
    private static void EnsurePastDate(DateOnly date, TimeProvider provider)
    {
        if (date > ParkingTime.Today(provider)) throw new InvalidOperationException("Tanggal laporan tidak boleh melebihi hari ini.");
    }
    private async Task<(decimal Amount, int Count)> ExpectedAsync(NpgsqlTransaction? tx, UserSession session, DateOnly date, CancellationToken ct)
    {
        var (from, to) = ParkingTime.Bounds(date, date);
        await using var c = source.CreateCommand("SELECT COALESCE(sum(fare),0), count(*) FROM parking.parking_transactions WHERE exit_jukir_id=@jid AND status='completed' AND exit_time >= @from AND exit_time < @to");
        if (tx is not null) c.Transaction = tx;
        c.Parameters.AddWithValue("jid", session.Id); c.Parameters.AddWithValue("from", NpgsqlDbType.TimestampTz, new DateTimeOffset(from, TimeSpan.Zero)); c.Parameters.AddWithValue("to", NpgsqlDbType.TimestampTz, new DateTimeOffset(to, TimeSpan.Zero));
        await using var reader = await c.ExecuteReaderAsync(ct); await reader.ReadAsync(ct); return (reader.GetDecimal(0), reader.GetInt32(1));
    }
    public async Task<DailyReportSummary> GetDailyReportAsync(UserSession session, DateOnly date, CancellationToken ct)
    {
        await AuthorizeDepositReadAsync(session, ct);
        EnsurePastDate(date, timeProvider);
        var expectedResult = await ExpectedAsync(null, session, date, ct);
        var expected = expectedResult.Amount;
        var (from, to) = ParkingTime.Bounds(date, date);
        int motorCount = 0, carCount = 0;
        decimal motorRev = 0, carRev = 0;
        await using (var cVehicles = source.CreateCommand("SELECT vehicle_type, count(*), COALESCE(sum(fare),0) FROM parking.parking_transactions WHERE exit_jukir_id=@jid AND status='completed' AND exit_time >= @from AND exit_time < @to GROUP BY vehicle_type"))
        {
            cVehicles.Parameters.AddWithValue("jid", session.Id);
            cVehicles.Parameters.AddWithValue("from", NpgsqlDbType.TimestampTz, new DateTimeOffset(from, TimeSpan.Zero));
            cVehicles.Parameters.AddWithValue("to", NpgsqlDbType.TimestampTz, new DateTimeOffset(to, TimeSpan.Zero));
            await using var vReader = await cVehicles.ExecuteReaderAsync(ct);
            while (await vReader.ReadAsync(ct))
            {
                var vType = vReader.GetString(0);
                if (vType == "motorcycle") { motorCount = vReader.GetInt32(1); motorRev = vReader.GetDecimal(2); }
                else { carCount = vReader.GetInt32(1); carRev = vReader.GetDecimal(2); }
            }
        }
        await using var c = source.CreateCommand("SELECT d.id,d.amount_deposited,d.discrepancy,d.verification_status,d.jukir_id,d.admin_id,d.notes FROM parking.deposits d WHERE d.jukir_id=@j AND d.deposit_date=@date");
        c.Parameters.AddWithValue("j", session.Id); c.Parameters.AddWithValue("date", date);
        await using var r = await c.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct))
            return new DailyReportSummary(date, expected, null, null, null, expectedResult.Count != 0, expectedResult.Count, motorCount, motorRev, carCount, carRev);
        var status = DepositStatusValue(r.GetString(3));
        return new DailyReportSummary(date, expected, r.GetDecimal(1), r.GetDecimal(2), status, expectedResult.Count != 0, expectedResult.Count, motorCount, motorRev, carCount, carRev);
    }
    public Task<DepositRecord?> SubmitDepositAsync(UserSession session, DateOnly date, decimal amountDeposited, CancellationToken ct) => SubmitDepositAsync(session, date, amountDeposited, false, ct);
    public async Task<DepositRecord?> SubmitDepositAsync(UserSession session, DateOnly date, decimal amountDeposited, bool confirmPendingUpdate, CancellationToken ct)
    {
        await AuthorizeAsync(session, UserRole.Jukir, ct); EnsurePastDate(date, timeProvider);
        if (amountDeposited < 0) throw new ArgumentException("Nominal setoran tidak boleh negatif.", nameof(amountDeposited));
        await using var connection = await source.OpenConnectionAsync(ct);
        await using var tx = await connection.BeginTransactionAsync(ct);
        await using (var lockCmd = new NpgsqlCommand("SELECT verification_status FROM parking.deposits WHERE jukir_id=@j AND deposit_date=@d FOR UPDATE", connection, tx))
        { lockCmd.Parameters.AddWithValue("j",session.Id); lockCmd.Parameters.AddWithValue("d",date); var old=await lockCmd.ExecuteScalarAsync(ct); if (old is string s && s != "pending") throw new InvalidOperationException("Setoran yang sudah divalidasi tidak dapat diubah."); if (old is string && !confirmPendingUpdate) throw new InvalidOperationException("Konfirmasi diperlukan untuk memperbarui setoran pending."); }
        var expected = (await ExpectedAsync(tx, session, date, ct)).Amount;
        await using var cmd = new NpgsqlCommand("INSERT INTO parking.deposits(jukir_id,deposit_date,amount_deposited,amount_expected) VALUES(@j,@d,@a,@e) ON CONFLICT(jukir_id,deposit_date) DO UPDATE SET amount_deposited=excluded.amount_deposited,amount_expected=excluded.amount_expected,admin_id=NULL,verification_status='pending',notes=NULL RETURNING id,amount_deposited,amount_expected,discrepancy,verification_status", connection, tx);
        cmd.Parameters.AddWithValue("j",session.Id); cmd.Parameters.AddWithValue("d",date); cmd.Parameters.AddWithValue("a",amountDeposited); cmd.Parameters.AddWithValue("e",expected);
        await using var r=await cmd.ExecuteReaderAsync(ct); await r.ReadAsync(ct); var result=new DepositRecord(r.GetInt32(0),session.Id,session.Name,date,r.GetDecimal(1),r.GetDecimal(2),r.GetDecimal(3),DepositStatusValue(r.GetString(4)),null,null,null); await r.CloseAsync(); await tx.CommitAsync(ct); return result;
    }
    public async Task<IReadOnlyList<DepositRecord>> GetDepositsAsync(UserSession session, DateOnly date, DepositStatus? status, CancellationToken ct)
    {
        await AuthorizeDepositReadAsync(session, ct); EnsurePastDate(date, timeProvider);
        await using var c=source.CreateCommand("SELECT d.id,d.jukir_id,j.name,d.deposit_date,d.amount_deposited,d.amount_expected,d.discrepancy,d.verification_status,d.admin_id,a.name,d.notes FROM parking.deposits d JOIN parking.jukir j ON j.id=d.jukir_id LEFT JOIN parking.admins a ON a.id=d.admin_id WHERE d.deposit_date=@d AND (@admin OR d.jukir_id=@j) AND (@status::text IS NULL OR d.verification_status=@status::text) ORDER BY d.id");
        c.Parameters.AddWithValue("d",date); c.Parameters.AddWithValue("admin",session.Role==UserRole.Admin); c.Parameters.AddWithValue("j",session.Id); var statusParameter=c.Parameters.Add("status", NpgsqlTypes.NpgsqlDbType.Text); statusParameter.Value=status is null ? DBNull.Value : DepositStatusDb(status.Value);
        var list=new List<DepositRecord>(); await using var r=await c.ExecuteReaderAsync(ct); while(await r.ReadAsync(ct)) list.Add(new DepositRecord(r.GetInt32(0),r.GetInt32(1),r.GetString(2),DateOnly.FromDateTime(r.GetDateTime(3)),r.GetDecimal(4),r.GetDecimal(5),r.GetDecimal(6),DepositStatusValue(r.GetString(7)),r.IsDBNull(8)?null:r.GetInt32(8),r.IsDBNull(9)?null:r.GetString(9),r.IsDBNull(10)?null:r.GetString(10))); return list;
    }
    public async Task<DepositRecord> ValidateDepositAsync(UserSession session, int depositId, string? notes, CancellationToken ct)
    {
        await AuthorizeAsync(session, UserRole.Admin, ct);
        await using var connection = await source.OpenConnectionAsync(ct); await using var tx=await connection.BeginTransactionAsync(ct);
        await using var c=new NpgsqlCommand("SELECT d.id,d.jukir_id,j.name,d.deposit_date,d.amount_deposited,d.amount_expected,d.discrepancy,d.verification_status FROM parking.deposits d JOIN parking.jukir j ON j.id=d.jukir_id WHERE d.id=@id FOR UPDATE",connection,tx); c.Parameters.AddWithValue("id",depositId); await using var r=await c.ExecuteReaderAsync(ct); if(!await r.ReadAsync(ct)) throw new InvalidOperationException("Setoran tidak ditemukan."); var st=r.GetString(7); if(st!="pending") throw new InvalidOperationException("Setoran sudah divalidasi."); var vals=(r.GetInt32(0),r.GetInt32(1),r.GetString(2),DateOnly.FromDateTime(r.GetDateTime(3)),r.GetDecimal(4),r.GetDecimal(5),r.GetDecimal(6)); await r.CloseAsync(); var next=vals.Item7==0?"matched":"need_review";
        await using var u=new NpgsqlCommand("UPDATE parking.deposits SET admin_id=@a,verification_status=@s,notes=@n WHERE id=@id RETURNING discrepancy",connection,tx); u.Parameters.AddWithValue("a",session.Id);u.Parameters.AddWithValue("s",next);u.Parameters.AddWithValue("n",(object?)notes??DBNull.Value);u.Parameters.AddWithValue("id",depositId); var discrepancy=(decimal)(await u.ExecuteScalarAsync(ct))!; await tx.CommitAsync(ct); return new DepositRecord(vals.Item1,vals.Item2,vals.Item3,vals.Item4,vals.Item5,vals.Item6,discrepancy,DepositStatusValue(next),session.Id,session.Name,notes);
    }
}
