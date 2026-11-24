# ParkirAja — Parking System (.NET 10 / Windows Forms)

Sistem aplikasi desktop manajemen parkir berbasis Windows Forms (.NET 10) dengan
Entity Framework Core dan database **PostgreSQL**.

Fokus implementasi: **modul Vehicle Entry** (pencatatan kendaraan masuk).
Halaman lain dipertahankan sebagai tampilan (desain utuh) tanpa logic data.

---

## Struktur Repository

```
Parking System/
├─ README.md
├─ docs/
│  ├─ Parking_System.drawio       # Use Case, Activity Diagram, ERD, Wireframe
│  ├─ POSTGRES_NAVICAT_RUNBOOK.md # Panduan database + Navicat
│  └─ AGENT_SKILLS_ECOSYSTEM.md   # Tata kelola pengembangan multi-agent
└─ ParkingSystem/
   ├─ ParkingSystem.slnx
   ├─ .env.example                # Template konfigurasi database
   ├─ Directory.Build.props
   ├─ Directory.Packages.props
   ├─ ParkingSystem/              # Aplikasi utama
   │  ├─ Program.cs               # Entry point + DI
   │  ├─ ISessionAware.cs
   │  ├─ Controls/                # Komponen UI kustom
   │  ├─ Database/                # DbContext, settings, repositori
   │  ├─ Models/                  # Entitas bisnis
   │  ├─ Security/                # PBKDF2 password hasher
   │  ├─ Theme/                   # Palet warna aplikasi
   │  └─ Views/                   # Halaman UI (UserControl/Form)
   └─ ParkingSystem.Tests/        # xUnit test suite
```

---

## Prasyarat

- [.NET 10.0 SDK](https://dotnet.microsoft.com/)
- Windows (Windows Forms Desktop Runtime)
- PostgreSQL 16+ yang berjalan
- Visual Studio 2022/2026 atau VS Code dengan C# Dev Kit

---

## Konfigurasi Database

Buat berkas `.env` di folder `ParkingSystem/ParkingSystem/` (sebelah `ParkingSystem.csproj`):

```ini
DB_HOST=localhost
DB_PORT=5432
DB_NAME=parkiraja
DB_USER=postgres
DB_PASSWORD=postgres
```

Atau satu connection string utuh:

```ini
DB_CONNECTION_STRING=Host=localhost;Port=5432;Database=parkiraja;Username=postgres;Password=postgres
```

Berkas `.env` **tidak di-commit** (sudah masuk `.gitignore`).
Template tersedia di `ParkingSystem/.env.example`.

Tabel dan data awal dibuat otomatis saat aplikasi pertama kali dijalankan.

Panduan lengkap (termasuk Navicat dan troubleshooting): **[docs/POSTGRES_NAVICAT_RUNBOOK.md](docs/POSTGRES_NAVICAT_RUNBOOK.md)**

---

## Menjalankan Aplikasi

```powershell
cd ParkingSystem

dotnet build ParkingSystem.slnx
dotnet run --project ParkingSystem/ParkingSystem.csproj
```

## Menjalankan Pengujian

```powershell
cd ParkingSystem
dotnet test ParkingSystem.slnx
```

---

## Akun Demo

| Peran | Username | Password | Area |
|---|---|---|---|
| Admin | `admin` | `adminpassword` | Pusat |
| Jukir 1 | `jukir1` | `jukirpassword` | Area Parkir Utama |
| Jukir 2 | `jukir2` | `jukirpassword` | Area Parkir Timur |

Kata sandi disimpan sebagai hash **PBKDF2** (HMAC-SHA256, 100.000 iterasi, salt 16 byte),
bukan teks biasa. Lihat `Security/PasswordHasher.cs`.

---

## Modul

| Halaman | Status |
|---|---|
| **Vehicle Entry** | **Berfungsi penuh** — validasi plat, cek duplikat, cek kapasitas, hitung tarif, simpan ke PostgreSQL |
| Vehicle Exit | Tampilan saja |
| Transaction History | Tampilan saja |
| Daily Report | Tampilan saja |
| Jukir Dashboard | Tampilan saja |
| Admin Dashboard | Tampilan saja |
| Parking Areas | Tampilan saja |
| Jukir Accounts | Tampilan saja |
| Report Validation | Tampilan saja |
| Export Reports | Tampilan saja |

---

## Alur Logic Vehicle Entry

```
Views/VehicleEntry.cs          SaveEntryButton_Click  (baris 68)
        ↓
Database/InMemoryDb.cs         CreateEntry            (baris 61)
        ↓                      ├─ validasi format plat
                               ├─ tolak plat aktif ganda
                               ├─ tolak area penuh
                               ├─ hitung tarif per jenis
                               └─ Occupied++ & SaveChanges
        ↓
Database/ParkingDbContext.cs   EF Core → PostgreSQL
```

---

## Catatan Teknis

- **Zona waktu**: kolom waktu disimpan UTC, ditampilkan WIB (UTC+7) lewat `ValueConverter`
  di `ParkingDbContext`. Npgsql menolak `DateTimeOffset` dengan offset selain 0.
- **Verifikasi skema**: `DatabaseInitializer` memastikan seluruh tabel wajib ada.
  `EnsureCreated()` saja akan gagal senyap bila database tujuan sudah berisi tabel lain.
- **Nama tabel** mengikuti `DbSet` (PascalCase), sehingga query manual memerlukan
  tanda kutip ganda: `SELECT * FROM "Transactions";`

---

## Spesifikasi & Diagram

`docs/Parking_System.drawio` berisi Use Case, Activity Diagram, ERD, dan Wireframe.

Dokumentasi tata kelola pengembangan:
- [Agent Skills Ecosystem](docs/AGENT_SKILLS_ECOSYSTEM.md)
