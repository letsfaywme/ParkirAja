# Panduan Database PostgreSQL + Navicat

Konfigurasi database mengikuti pola proyek **UangKu**: berkas `.env` + `dotenv.net`,
dengan variabel `DB_HOST` / `DB_PORT` / `DB_NAME` / `DB_USER` / `DB_PASSWORD`
atau satu `DB_CONNECTION_STRING` utuh.

---

## 1. Konfigurasi

Buat berkas `.env` di **folder project** (sebelah `ParkingSystem.csproj`):

```
ParkingSystem/
├─ ParkingSystem/
│  ├─ ParkingSystem.csproj
│  ├─ .env              ← di sini (tidak di-commit)
│  └─ ...
├─ .env.example         ← template yang di-commit
└─ ParkingSystem.slnx
```

Isi `.env`:

```ini
DB_HOST=localhost
DB_PORT=5432
DB_NAME=parkiraja_app
DB_USER=postgres
DB_PASSWORD=postgres
```

Atau satu baris:

```ini
DB_CONNECTION_STRING=Host=localhost;Port=5432;Database=parkiraja_app;Username=postgres;Password=postgres
```

Bila `DB_CONNECTION_STRING` diisi, seluruh komponen `DB_*` lainnya diabaikan.
`.env` sudah masuk `.gitignore`; `.env.example` tetap di-commit sebagai template.

### Urutan pencarian `.env`

1. Folder output aplikasi (`bin/Debug/net10.0-windows/`)
2. Direktori kerja saat aplikasi dijalankan
3. Setiap folder leluhur dari folder output

Variabel lingkungan yang sudah terisi **tidak ditimpa** oleh `.env`, sehingga kamu bisa
menimpa nilai tertentu dari terminal tanpa mengubah berkas.

### Bila password tidak ada

Aplikasi berhenti dengan pesan jelas:

> Password database tidak ditemukan. Buat berkas .env di folder proyek berisi DB_PASSWORD=...
> atau isi DB_CONNECTION_STRING secara utuh. Contoh: lihat .env.example.

Ini disengaja: lebih baik gagal di awal daripada menghasilkan koneksi anonim yang membingungkan.

---

## 2. Menjalankan aplikasi

```powershell
cd "D:\Dokumen\Produktif Fayakun\Personal Project\Parking System\ParkingSystem"
dotnet run --project ParkingSystem/ParkingSystem.csproj
```

Database dan seluruh tabel dibuat otomatis (`EnsureCreated`) beserta data seed bila belum ada.

Login demo:

| Peran | Username | Password |
|---|---|---|
| Admin | `admin` | `adminpassword` |
| Jukir 1 | `jukir1` | `jukirpassword` |
| Jukir 2 | `jukir2` | `jukirpassword` |

---

## 3. PostgreSQL yang dipakai

Mesin ini punya **dua** instance. Pilih satu lewat `DB_PORT` di `.env`.

| | Service Windows | Laragon |
|---|---|---|
| Versi | 16.15 | 18.2 |
| Port | `5432` | `5433` |
| Data | `C:\Program Files\PostgreSQL\16\data` | `C:\laragon\data\postgresql` |
| Start | otomatis (Windows service) | manual |
| Status | RUNNING | perlu `pg_ctl start` |

### Memakai service Windows (disarankan)

```powershell
Get-Service postgresql-x64-16      # harus Running
```

`.env`:

```ini
DB_PORT=5432
DB_NAME=parkiraja_app
```

### Memakai Laragon

```powershell
C:\laragon\bin\postgresql\postgresql\bin\pg_ctl.exe -D C:\laragon\data\postgresql -l C:\laragon\data\postgresql\server.log start
```

`.env`:

```ini
DB_PORT=5433
DB_NAME=parkiraja_laragon
```

Catatan: tombol **Start All** Laragon **tidak** menyalakan PostgreSQL karena
`[postgresql] Use=-1` di `C:\laragon\usr\laragon.ini`. Jalankan `pg_ctl` seperti di atas.

---

## 4. Menghubungkan Navicat

1. Buka **Navicat Premium Lite 17**.
2. **Connection → PostgreSQL**.
3. Isi:

   | Field | Nilai |
   |---|---|
   | Connection Name | `ParkirAja Local` |
   | Host | `localhost` |
   | Port | `5432` (service Windows) atau `5433` (Laragon) |
   | Initial Database | `parkiraja_app` |
   | Authentication | Password |
   | User Name | `postgres` |
   | Password | `postgres` |

4. **Test Connection** → **OK**.

Tabel yang akan terlihat: `Users`, `Spots`, `Transactions`, `Deposits`.

### Alur demo yang meyakinkan

1. Buka **Vehicle Entry**, masukkan plat `B 1234 XYZ`, pilih **Car**, simpan.
2. Di Navicat, klik kanan `Transactions` → **Refresh**.
3. Baris baru muncul: `Status = ACTIVE`, `RateAtEntry = 5000.00`, `EntryTime` ber-offset `+07`.
4. Buka `Spots`, lihat `Occupied` area tersebut bertambah 1.
5. Di aplikasi, buka **Vehicle Exit**, cari plat yang sama, proses pembayaran.
6. Refresh Navicat: `Status` menjadi `COMPLETED`, `Fare` dan `ExitTime` terisi, `Occupied` berkurang.

---

## 5. Nama tabel dan tanda kutip

Nama tabel mengikuti nama `DbSet` (PascalCase), jadi di Navicat maupun psql **wajib** tanda kutip ganda:

```sql
SELECT * FROM "Transactions" ORDER BY "Id" DESC;   -- benar
SELECT * FROM transactions;                        -- error: relation does not exist
```

---

## 6. Zona waktu

Seluruh kolom waktu bertipe `timestamptz` dan disimpan sebagai UTC.
`ParkingDbContext` mengonversi bolak-balik lewat `ValueConverter`:

- **Tulis:** `DateTimeOffset` apa pun → `ToUniversalTime()`
- **Baca:** nilai UTC → `ToOffset(+07:00)`

Tanpa konverter ini, Npgsql menolak `DateTimeOffset.Now` (offset `+07`) dengan
`ArgumentException: only offset 0 (UTC) is supported`, dan aplikasi gagal menyimpan tiket.

---

## 7. Troubleshooting

### "Skema database 'X' tidak lengkap. Tabel tidak ditemukan: ..."

Database tujuan sudah berisi tabel/schema lain, sehingga `EnsureCreated()` menganggap skema
sudah ada dan melewati pembuatan tabel.

Perbaikan — ubah `DB_NAME` di `.env` ke database baru, atau hapus database yang bentrok:

```sql
DROP DATABASE namadb WITH (FORCE);
```

### "Koneksi Database Gagal" saat aplikasi dibuka

Dialog muncul sebelum jendela login. Periksa:

1. PostgreSQL hidup: `Get-Service postgresql-x64-16`
2. `DB_PASSWORD` di `.env` benar
3. Port di `.env` cocok dengan instance yang hidup (5432 vs 5433)

### Variabel `PARKING_DB_CONNECTION` lama

Mesin ini masih menyimpan variabel lingkungan lama dari branch `vibe`:

```
PARKING_DB_CONNECTION=Host=127.0.0.1;Port=5432;Database=parkiraja;Username=parking_runtime;
```

Variabel itu **tidak lagi dibaca** oleh kode saat ini (konfigurasi kini memakai `DB_*`),
jadi aman dibiarkan. Untuk membersihkannya:

```powershell
[Environment]::SetEnvironmentVariable("PARKING_DB_CONNECTION", $null, "User")
```

### Data hilang setiap restart

Pastikan `.env` menunjuk PostgreSQL yang benar, bukan database lain.
Cek cepat: tambah tiket di aplikasi, lalu `SELECT count(*) FROM "Transactions";` di Navicat.

---

## 8. Ringkasan perubahan kode

| File | Perubahan |
|---|---|
| `Database/DatabaseSettings.cs` | Konfigurasi `.env` + `DB_*`, cache connection string, gagal keras bila password kosong |
| `Database/DatabaseInitializer.cs` | Verifikasi tabel wajib, gagal keras bila skema tidak lengkap |
| `Database/ParkingDbContext.cs` | `UseNpgsql` + `ValueConverter` UTC↔WIB |
| `Program.cs` | DI factory ke Npgsql + dialog error saat startup gagal |
| `Directory.Packages.props` | `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3, `dotenv.net` 4.2.0 |
| `ParkingSystem.csproj` | Referensi paket + `InternalsVisibleTo` untuk test |
| `.env.example` | Template konfigurasi (di-commit) |
| `.env` | Konfigurasi lokal (di-gitignore) |
