# ParkirAja — Desktop UI (Windows Forms)

Sistem aplikasi desktop manajemen parkir berbasis Windows Forms (.NET 10).

## Struktur Branch

Repository ini memiliki dua branch utama:

1. **`master`** (Branch saat ini):
   - **Template UI & Struktur Kode Bersih**: Berisi seluruh desain antarmuka (UI/Forms), layout responsif, komponen custom (`RoundedControls`, `DarkDateTimePicker`, `AppTheme`), dan navigasi halaman.
   - **Tanpa Logic & Tanpa Database**: Seluruh dependency database dan query SQL telah dilepas agar Anda dapat langsung fokus menuliskan business logic, state management, dan integrasi database secara manual.
   - Setiap view (`Views/`) telah dilengkapi *event handler stub* dengan komentar `// TODO:` yang terstruktur rapi.

2. **`vibe`**:
   - **Full Source Code**: Berisi implementasi utuh dari sistem parkir ParkirAja, termasuk koneksi database PostgreSQL, hashing password PBKDF2, service layer (`ParkingService`), cetak struk GDI, ekspor PDF, serta suite validasi otomatis (`ParkingSystem.Checks`).

---

## Memulai di Branch `master`

### Prasyarat
- [.NET 10.0 SDK](https://dotnet.microsoft.com/)
- Visual Studio 2022 / 2026 atau VS Code dengan C# Dev Kit

### Menjalankan Project
```powershell
# Masuk ke folder ParkingSystem
cd ParkingSystem

# Build solution
dotnet build ParkingSystem.slnx

# Jalankan aplikasi
dotnet run --project ParkingSystem/ParkingSystem.csproj
```

### Navigasi & Tampilan
- **Form Login**:
  - Ketik username apa saja lalu klik **Sign In** untuk membuka workspace **Jukir** (Petugas Parkir).
  - Ketik username yang mengandung kata `admin` (misal: `admin`) lalu klik **Sign In** untuk membuka workspace **Admin**.
- **Admin Pages**:
  - `AdminDashboard` (Ringkasan statistik)
  - `ReportValidation` (Validasi setoran jukir)
  - `ParkingAreas` (Manajemen area & kapasitas parkir)
  - `JukirAccounts` (Manajemen akun petugas jukir)
  - `ExportReports` (Ekspor laporan berkala)
- **Jukir Pages**:
  - `JukirDashboard` (Statistik harian jukir)
  - `VehicleEntry` (Input kendaraan masuk)
  - `VehicleExit` (Pencarian & pembayaran kendaraan keluar)
  - `TransactionHistory` (Riwayat transaksi)
  - `DailyReport` (Laporan & pengajuan setoran harian)
