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

### Menjalankan Pengujian Otomatis (xUnit)
```powershell
cd ParkingSystem
dotnet test ParkingSystem.slnx
```

### Navigasi & Tampilan
- **Form Login**:
  - Akun Admin: Username `admin`, Password `adminpassword` (Membuka workspace Admin).
  - Akun Jukir 1: Username `jukir1`, Password `jukirpassword` (Area Parkir Utama).
  - Akun Jukir 2: Username `jukir2`, Password `jukirpassword` (Area Parkir Timur).
  - Kata sandi diamankan secara kriptografis menggunakan algoritma hashing PBKDF2 (HMAC-SHA256 bergaram 16-byte).
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

---

## Multi-Agent AI Development Ecosystem
Proyek ini dikembangkan dengan tata kelola multi-agent AI berbasis 9 peran terspesialisasi (Orchestrator, Product Analyst, System Architect, UI/UX Designer, Software Developer, QA Engineer, Security Engineer, DevOps Engineer, Technical Writer).

Panduan lengkap topologi, batasan wewenang (*boundary policies*), dan protokol jabat tangan (*cross-agent handshake*) dapat dilihat pada:
- **[Agent Skills Ecosystem & Governance Guide](docs/AGENT_SKILLS_ECOSYSTEM.md)**
- **[Laporan Audit Kualitas & Remediasi Multi-Agent](docs/PROJECT_VERIFICATION_AUDIT.md)**
- **Spesifikasi & Diagram Arsitektur**: `Parking_System.drawio` (Use Case, Activity Diagram, ERD, dan Wireframes)
