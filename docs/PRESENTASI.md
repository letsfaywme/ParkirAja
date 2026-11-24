# Panduan Presentasi ParkirAja

Cheat sheet untuk demonstrasi modul **Vehicle Entry**.
Semua angka dan lokasi baris di dokumen ini sudah diverifikasi terhadap kode saat ini.

---

## Persiapan 15 Menit Sebelum

### 1. Nyalakan database

```powershell
# Cek PostgreSQL Laragon (port 5433)
netstat -an | findstr :5433
```

Jika kosong, nyalakan lewat Laragon → tray → PostgreSQL → Start.

### 2. Buka Navicat

| Field | Nilai |
|---|---|
| Host | `localhost` |
| Port | `5433` |
| Database | `parkiraja` |
| User | `postgres` |
| Password | `postgres` |

### 3. Jalankan aplikasi

```powershell
cd "D:\Dokumen\Produktif Fayakun\Personal Project\Parking System\ParkingSystem"
dotnet run --project ParkingSystem/ParkingSystem.csproj
```

### 4. Buka 4 file di editor

| Tab | File | Untuk apa |
|---|---|---|
| 1 | `ParkingSystem/Views/VehicleEntry.cs` | Handler UI (baris 68) |
| 2 | `ParkingSystem/Database/InMemoryDb.cs` | Aturan bisnis (baris 61) |
| 3 | `ParkingSystem/Database/ParkingDbContext.cs` | Skema & konversi waktu |
| 4 | `ParkingSystem/Security/PasswordHasher.cs` | PBKDF2 |

### 5. Reset data demo

Jalankan di Navicat supaya mulai dari kondisi bersih:

```sql
DELETE FROM "Transactions" WHERE "Id" > 4;
UPDATE "Spots" SET "Occupied" = 2 WHERE "Id" = 1;
UPDATE "Spots" SET "Occupied" = 1 WHERE "Id" = 2;
```

---

## Urutan Presentasi (12 menit)

### Menit 1 — Login

Login: `jukir1` / `jukirpassword`

> "Aplikasi desktop Windows Forms, data tersimpan di PostgreSQL. Login dulu untuk masuk."

**Jika ditanya soal keamanan password** → buka `Security/PasswordHasher.cs`:
- `Rfc2898DeriveBytes.Pbkdf2` — PBKDF2-HMAC-SHA256
- 100.000 iterasi, salt 16 byte, hash 32 byte
- Verifikasi memakai `CryptographicOperations.FixedTimeEquals` (anti timing attack)

### Menit 2 — Tunjukkan UI

Klik sidebar **Vehicle Entry**.

| Elemen | Nama kontrol |
|---|---|
| Input plat | `platenumberTextBox` |
| Jenis kendaraan | `vehicletypeComboBox` |
| Area parkir | `parkingareaComboBox` |
| Label tarif | `rateLabel` |
| Tombol simpan | `saveEntryButton` |
| Tabel 10 tiket | `grid22` |

**Aksi:** ganti jenis dari Motorcycle ke Car → label tarif berubah `Rp 2.000` → `Rp 5.000`.

> "Tarif diambil dari database, bukan hardcoded."

### Menit 3-5 — Tunjukkan kode

**A. `Views/VehicleEntry.cs` baris 68** — `SaveEntryButton_Click`

Tunjuk tiga hal:
```csharp
if (_editingTxId is int editId)      // 74 — mode edit atau tambah
if (string.IsNullOrWhiteSpace(...))  // 91 — validasi UI
InMemoryDb.CreateEntry(...)          // 96 — masuk ke logic bisnis
```

**B. `Database/InMemoryDb.cs` baris 61** — `CreateEntry`

| Baris | Aturan |
|---|---|
| 66 | Format plat `^[A-Z]{1,2}\s\d{1,4}\s[A-Z]{1,3}$` |
| 72 | Tolak plat yang masih aktif |
| 80 | Tolak jika area penuh |
| 86 | Hitung tarif per jenis kendaraan |
| 106 | `spot.Occupied++` |
| 107 | `Db.SaveChanges()` → PostgreSQL |

**C. `Database/ParkingDbContext.cs`**

- Baris 116: index `(PlateNumber, Status)` → mempercepat cek duplikat
- Baris 123-124: `ValueConverter` UTC ↔ WIB

> "Waktu disimpan UTC di database, ditampilkan WIB. Tanpa converter, Npgsql menolak menyimpan DateTimeOffset ber-offset."

### Menit 6-8 — Praktik

**Demo 1 — berhasil**
- Plat: `B 1234 XYZ`, jenis: Car → Save
- Muncul karcis: nomor tiket, jam masuk, tarif Rp 5.000/jam

**Demo 2 — format plat salah**
- Plat: `XXXX` → Save
- Muncul: *"Format nomor plat 'XXXX' tidak valid."*

**Demo 3 — plat ganda**
- Plat: `B 1234 XYZ` lagi → Save
- Muncul: *"Kendaraan dengan plat B 1234 XYZ masih tercatat aktif!"*

### Menit 9-11 — Buktikan di database

**Query 1 — tiket baru**
```sql
SELECT "Id","PlateNumber","VehicleType","Status","RateAtEntry","EntryTime"
FROM "Transactions" ORDER BY "Id" DESC LIMIT 3;
```

**Query 2 — counter area naik**
```sql
SELECT "Id","Name","Occupied","Capacity" FROM "Spots";
```
Occupied area 1 naik dari 2 → 3.

> "Angka ini tidak ada di kode. Ini hasil klik Save tadi."

### Menit 12 — Bonus: alur keluar

Klik sidebar **Vehicle Exit** → cari plat yang baru dibuat → **Find** → **Process Vehicle Exit**.

Lalu di Navicat:
```sql
SELECT "Id","PlateNumber","Status","Fare","ExitTime"
FROM "Transactions" ORDER BY "Id" DESC LIMIT 2;
```

Status berubah `ACTIVE` → `COMPLETED`, `Fare` dan `ExitTime` terisi, `Occupied` turun kembali.

> "Siklus lengkap: masuk tercatat, keluar dihitung otomatis per jam dibulatkan ke atas."

---

## Antisipasi Pertanyaan

| Pertanyaan | Jawaban | Bukti |
|---|---|---|
| Kenapa namanya `InMemoryDb` padahal PostgreSQL? | Nama warisan versi awal; sekarang membungkus EF Core ke PostgreSQL | `InMemoryDb.cs:16` |
| Kenapa `Occupied` di-update manual? | Kolom denormalisasi untuk performa, bukan hasil COUNT | Tabel `Spots` |
| ID tiket dihitung bagaimana? | `Max(Id) + 1` | `InMemoryDb.cs:89` |
| Bagaimana kalau dua jukir entry bersamaan? | **Belum aman** — belum ada transaksi DB/locking. Saya sadar ini keterbatasan | jujur saja |
| Kenapa pakai `DateTimeOffset` bukan `DateTime`? | Menyimpan offset waktu, penting untuk WIB | `ParkingDbContext.cs:123` |
| Halaman lain kenapa kosong? | Fokus demo di Vehicle Entry; desain dipertahankan | sidebar |
| Datanya hilang saat restart? | Tidak — PostgreSQL permanen | restart & query ulang |

**Dua jawaban jujur di atas (race condition & ID manual) justru menaikkan nilai** — menunjukkan kamu paham batas karyamu.

---

## Kalau Waktu Cuma 6 Menit

1. Login (30 detik)
2. Vehicle Entry — tunjuk 4 input + tabel (1 menit)
3. `VehicleEntry.cs:96` → `InMemoryDb.cs:61-107` (2 menit)
4. Demo entry + demo plat ganda (1,5 menit)
5. Navicat: query Transactions + Spots (1 menit)

---

## Troubleshooting Saat Demo

| Gejala | Penyebab | Perbaikan |
|---|---|---|
| Dialog "Koneksi Database Gagal" | PostgreSQL mati | Nyalakan Laragon PostgreSQL |
| "Skema database tidak lengkap" | Database berisi tabel proyek lain | Ubah `DB_NAME` di `.env` |
| Login gagal terus | Data seed terhapus | Hapus database, jalankan ulang aplikasi |
| Tabel Navicat kosong | Belum refresh | Klik kanan → Refresh (F5) |
| `relation "transactions" does not exist` | Nama tabel PascalCase | Pakai tanda kutip: `"Transactions"` |

---

## Checklist Final

- [ ] PostgreSQL Laragon nyala (port 5433)
- [ ] Navicat terkoneksi ke `parkiraja`
- [ ] Aplikasi jalan, login `jukir1` berhasil
- [ ] Plat `B 1234 XYZ` belum terpakai
- [ ] 4 file sudah terbuka di editor
- [ ] Tahu jawaban "kenapa `Occupied` manual"
- [ ] Tahu jawaban "bagaimana kalau konkuren"
