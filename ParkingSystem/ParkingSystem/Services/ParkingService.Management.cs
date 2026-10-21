using Npgsql;

namespace ParkingSystem;

public sealed record JukirAccount(int Id, int ParkingSpotId, string LocationName, string Username, string Name, bool IsActive);

public sealed partial class ParkingService
{
    private const long UsernameAdvisoryLock = 736192004L;

    public async Task<IReadOnlyList<ParkingSpot>> GetParkingSpotsAsync(UserSession session, CancellationToken ct)
    {
        await AuthorizeAsync(session, UserRole.Admin, ct);
        await using var cmd = source.CreateCommand("""
            SELECT p.id,p.location_name,p.address,p.motorcycle_rate,p.car_rate,p.capacity,p.is_active,
                   (SELECT count(*) FROM parking.parking_transactions t WHERE t.parking_spot_id=p.id AND t.status='active')
            FROM parking.parking_spots p ORDER BY p.is_active DESC, lower(p.location_name), p.id
            """);
        var result = new List<ParkingSpot>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct)) result.Add(new ParkingSpot(r.GetInt32(0),r.GetString(1),r.GetString(2),r.GetDecimal(3),r.GetDecimal(4),r.GetInt32(5),r.GetBoolean(6),checked((int)r.GetInt64(7))));
        return result;
    }

    public async Task<ParkingSpot> SaveParkingSpotAsync(UserSession session, int? id, string name, string address, decimal motorcycleRate, decimal carRate, int capacity, CancellationToken ct)
    {
        await AuthorizeAsync(session, UserRole.Admin, ct);
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(address)) throw new ArgumentException("Nama lokasi dan alamat wajib diisi.");
        if (motorcycleRate < 0 || carRate < 0) throw new ArgumentException("Tarif tidak boleh negatif.");
        if (capacity <= 0) throw new ArgumentException("Kapasitas harus lebih besar dari nol.");
        name=name.Trim(); address=address.Trim();
        await using var c = await source.OpenConnectionAsync(ct); await using var tx = await c.BeginTransactionAsync(ct);
        int spotId;
        try
        {
            if (id is null)
            {
                await using var q = new NpgsqlCommand("INSERT INTO parking.parking_spots(location_name,address,motorcycle_rate,car_rate,capacity) VALUES(@name,@address,@motor,@car,@capacity) RETURNING id",c,tx);
                q.Parameters.AddWithValue("name",name); q.Parameters.AddWithValue("address",address); q.Parameters.AddWithValue("motor",motorcycleRate); q.Parameters.AddWithValue("car",carRate); q.Parameters.AddWithValue("capacity",capacity); spotId=(int)(await q.ExecuteScalarAsync(ct))!;
            }
            else
            {
                await using var q = new NpgsqlCommand("UPDATE parking.parking_spots SET location_name=@name,address=@address,motorcycle_rate=@motor,car_rate=@car,capacity=@capacity WHERE id=@id RETURNING id",c,tx);
                q.Parameters.AddWithValue("id",id.Value); q.Parameters.AddWithValue("name",name); q.Parameters.AddWithValue("address",address); q.Parameters.AddWithValue("motor",motorcycleRate); q.Parameters.AddWithValue("car",carRate); q.Parameters.AddWithValue("capacity",capacity); var value=await q.ExecuteScalarAsync(ct); if(value is null) throw new InvalidOperationException("Lokasi tidak ditemukan."); spotId=(int)value;
            }
            await using var check = new NpgsqlCommand("SELECT count(*) FROM parking.parking_transactions WHERE parking_spot_id=@id AND status='active'",c,tx); check.Parameters.AddWithValue("id",spotId); if((long)(await check.ExecuteScalarAsync(ct))! > capacity) throw new InvalidOperationException("Kapasitas tidak boleh kurang dari kendaraan aktif.");
            await tx.CommitAsync(ct);
        }
        catch(PostgresException ex) when(ex.SqlState==PostgresErrorCodes.UniqueViolation) { throw new InvalidOperationException("Nama lokasi sudah digunakan.",ex); }
        return (await GetParkingSpotsAsync(session,ct)).First(x=>x.Id==spotId);
    }

    public async Task DeactivateParkingSpotAsync(UserSession session, int id, CancellationToken ct)
    {
        await AuthorizeAsync(session, UserRole.Admin, ct);
        await using var c=await source.OpenConnectionAsync(ct); await using var tx=await c.BeginTransactionAsync(ct);
        await using(var q=new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM parking.parking_transactions WHERE parking_spot_id=@id AND status='active'), EXISTS(SELECT 1 FROM parking.jukir WHERE parking_spot_id=@id AND is_active)",c,tx)){q.Parameters.AddWithValue("id",id); await using var r=await q.ExecuteReaderAsync(ct); if(!await r.ReadAsync(ct)) throw new InvalidOperationException("Lokasi tidak ditemukan."); if(r.GetBoolean(0)) throw new InvalidOperationException("Lokasi memiliki kendaraan aktif."); if(r.GetBoolean(1)) throw new InvalidOperationException("Nonaktifkan Jukir aktif terlebih dahulu.");}
        await using var u=new NpgsqlCommand("UPDATE parking.parking_spots SET is_active=false WHERE id=@id",c,tx);u.Parameters.AddWithValue("id",id);if(await u.ExecuteNonQueryAsync(ct)==0)throw new InvalidOperationException("Lokasi tidak ditemukan.");await tx.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<JukirAccount>> GetJukirAsync(UserSession session, CancellationToken ct)
    {
        await AuthorizeAsync(session,UserRole.Admin,ct); await using var q=source.CreateCommand("SELECT j.id,j.parking_spot_id,p.location_name,j.username,j.name,j.is_active FROM parking.jukir j JOIN parking.parking_spots p ON p.id=j.parking_spot_id ORDER BY j.is_active DESC,lower(j.name),j.id");var a=new List<JukirAccount>();await using var r=await q.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))a.Add(new JukirAccount(r.GetInt32(0),r.GetInt32(1),r.GetString(2),r.GetString(3),r.GetString(4),r.GetBoolean(5)));return a;
    }

    public async Task<JukirAccount> SaveJukirAsync(UserSession session,int? id,string name,string username,string? password,int parkingSpotId,CancellationToken ct)
    {
        await AuthorizeAsync(session,UserRole.Admin,ct); if(string.IsNullOrWhiteSpace(name)||string.IsNullOrWhiteSpace(username))throw new ArgumentException("Nama dan username wajib diisi."); username=username.Trim().ToLowerInvariant(); name=name.Trim(); if(id is null && string.IsNullOrEmpty(password))throw new ArgumentException("Password wajib diisi."); if(password is not null && (password.Length<8||password.Length>128))throw new ArgumentException("Password harus 8–128 karakter.");
        await using var c=await source.OpenConnectionAsync(ct);await using var tx=await c.BeginTransactionAsync(ct);await using(var l=new NpgsqlCommand("SELECT pg_advisory_xact_lock(@lock)",c,tx)){l.Parameters.AddWithValue("lock",UsernameAdvisoryLock);await l.ExecuteNonQueryAsync(ct);} 
        try { await using(var exists=new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM parking.admins WHERE username=@u) OR EXISTS(SELECT 1 FROM parking.jukir WHERE username=@u AND (@id::integer IS NULL OR id<>@id::integer))",c,tx)){exists.Parameters.AddWithValue("u",username);exists.Parameters.AddWithValue("id",(object?)id??DBNull.Value);if((bool)(await exists.ExecuteScalarAsync(ct))!)throw new InvalidOperationException("Username sudah digunakan.");}
            if (id is not null)
            {
                await using var active = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM parking.parking_transactions WHERE jukir_id=@id AND status='active')", c, tx);
                active.Parameters.AddWithValue("id", id.Value);
                if ((bool)(await active.ExecuteScalarAsync(ct))!) throw new InvalidOperationException("Jukir memiliki kendaraan aktif; reassignment ditolak.");
            }
            int jid; if(id is null){await using var q=new NpgsqlCommand("INSERT INTO parking.jukir(parking_spot_id,username,name,password_hash) SELECT @spot,@u,@name,@hash WHERE EXISTS(SELECT 1 FROM parking.parking_spots WHERE id=@spot AND is_active) RETURNING id",c,tx);q.Parameters.AddWithValue("spot",parkingSpotId);q.Parameters.AddWithValue("u",username);q.Parameters.AddWithValue("name",name);q.Parameters.AddWithValue("hash",PasswordHash.Hash(password!));var v=await q.ExecuteScalarAsync(ct);if(v is null)throw new InvalidOperationException("Lokasi aktif tidak ditemukan.");jid=(int)v;}else{var sql="UPDATE parking.jukir SET parking_spot_id=@spot,username=@u,name=@name"+(password is null?"":" ,password_hash=@hash")+" WHERE id=@id AND EXISTS(SELECT 1 FROM parking.parking_spots WHERE id=@spot AND is_active)";await using var q=new NpgsqlCommand(sql,c,tx);q.Parameters.AddWithValue("id",id.Value);q.Parameters.AddWithValue("spot",parkingSpotId);q.Parameters.AddWithValue("u",username);q.Parameters.AddWithValue("name",name);if(password is not null)q.Parameters.AddWithValue("hash",PasswordHash.Hash(password));if(await q.ExecuteNonQueryAsync(ct)==0)throw new InvalidOperationException("Jukir atau lokasi aktif tidak ditemukan.");jid=id.Value;}await tx.CommitAsync(ct);return (await GetJukirAsync(session,ct)).First(x=>x.Id==jid);}
        catch(PostgresException ex) when(ex.SqlState==PostgresErrorCodes.UniqueViolation){throw new InvalidOperationException("Username sudah digunakan.",ex);}
    }

    public async Task DeactivateJukirAsync(UserSession session,int id,CancellationToken ct){await AuthorizeAsync(session,UserRole.Admin,ct);await using var c=await source.OpenConnectionAsync(ct);await using var tx=await c.BeginTransactionAsync(ct);await using(var q=new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM parking_transactions WHERE jukir_id=@id AND status='active'), EXISTS(SELECT 1 FROM deposits WHERE jukir_id=@id AND verification_status='pending')",c,tx)){q.Parameters.AddWithValue("id",id);await using var r=await q.ExecuteReaderAsync(ct);if(!await r.ReadAsync(ct))throw new InvalidOperationException("Jukir tidak ditemukan.");if(r.GetBoolean(0))throw new InvalidOperationException("Jukir memiliki kendaraan aktif.");if(r.GetBoolean(1))throw new InvalidOperationException("Jukir memiliki laporan setoran pending.");}await using var u=new NpgsqlCommand("UPDATE parking.jukir SET is_active=false WHERE id=@id",c,tx);u.Parameters.AddWithValue("id",id);if(await u.ExecuteNonQueryAsync(ct)==0)throw new InvalidOperationException("Jukir tidak ditemukan.");await tx.CommitAsync(ct);}
}
