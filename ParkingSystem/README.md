# ParkirAja — Parking System Desktop Application (.NET 10 / Windows Forms)

Sistem aplikasi desktop manajemen parkir terpadu berbasis Windows Forms (.NET 10) dengan dukungan arsitektur Entity Framework Core (In-Memory untuk pengembangan terisolasi dan siap migrasi ke PostgreSQL).

---

## 1. Panduan Menjalankan Proyek

### Prasyarat
- [.NET 10.0 SDK](https://dotnet.microsoft.com/)
- Windows OS (Windows Forms Desktop Runtime)
- Visual Studio 2022/2026 atau VS Code dengan C# Dev Kit

### Build & Menjalankan Aplikasi
```powershell
# Masuk ke direktori solution
cd ParkingSystem

# Build solution
dotnet build ParkingSystem.slnx

# Jalankan aplikasi
dotnet run --project ParkingSystem/ParkingSystem.csproj
```

### Menjalankan Pengujian Otomatis (xUnit & FluentAssertions)
```powershell
cd ParkingSystem
dotnet test ParkingSystem.slnx
```

---

## 2. Kredensial Autentikasi Demo

Seluruh kata sandi diamankan secara kriptografis menggunakan algoritma hashing **PBKDF2 (HMAC-SHA256, 100.000 iterasi, salt 16-byte)**:

| Peran | Username | Password | Penugasan Area | Hak Akses Modul |
|---|---|---|---|---|
| **Admin** | `admin` | `adminpassword` | Seluruh Area (Pusat) | Dashboard Global, Validasi Setoran, Kelola Area, Kelola Jukir, Ekspor Laporan |
| **Jukir** | `jukir1` | `jukirpassword` | Area Parkir Utama (ID: 1) | Entri Kendaraan, Checkout/Exit, Riwayat Harian, Pengajuan Setoran |
| **Jukir** | `jukir2` | `jukirpassword` | Area Parkir Timur (ID: 2) | Entri Kendaraan, Checkout/Exit, Riwayat Harian, Pengajuan Setoran |

---

## 3. Fitur Utama & Kepatuhan Arsitektur

1. **Keamanan & Otorisasi**:
   - `PasswordHasher`: Hashing dan verifikasi kata sandi waktu-konstan (`FixedTimeEquals`).
   - `ISessionAware`: Seluruh form operasional (Vehicle Entry, Exit, Daily Report, Validation) mengikat transaksi ke sesi pengguna yang sedang login aktif.
2. **Arsitektur Thread-Safe & Async**:
   - `IDbContextFactory<ParkingDbContext>`: Mencegah konflik multi-thread dan memory leak.
   - `IParkingService`: Layer service asynchronous untuk semua operasi bisnis parkir.
   - Timestamps standar UTC (`DateTimeOffset.UtcNow`).
3. **Penyelarasan Skema & Diagram Draw.io**:
   - Berkas spesifikasi `../Parking_System.drawio` telah disinkronkan secara menyeluruh (Use Case, Activity Diagram, ERD, dan Wireframes).
   - Foreign Keys (`ParkingSpot`, `Jukir`, `ExitJukir`, `Validator`) dan relasi navigasi EF Core terkonfigurasi lengkap.
   - Validasi format plat nomor standar Indonesia (`^[A-Z]{1,2}\s\d{1,4}\s[A-Z]{1,3}$`).
4. **DevOps & Build Governance**:
   - Central Package Management (CPM) via `Directory.Packages.props`.
   - CI/CD workflow GitHub Actions di `.github/workflows/ci.yml`.
