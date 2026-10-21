# ParkirAja — Desktop UI

Open `ParkingSystem.slnx`, build, then open `Views/Login.cs` or `Views/MainForm.cs` with **View Designer**. `MainForm` owns a minimal sidebar and persistent workspace. The ten workspace pages remain editable `UserControl` designs in `Views/`.

Startup membuka Login dengan autentikasi PostgreSQL dan PBKDF2-SHA256. Role berasal dari akun, bukan pilihan pengguna. Jukir memiliki Dashboard, Vehicle Entry, Vehicle Exit, Transaction History, dan Daily Report; Admin memiliki Dashboard, Validate Reports, Parking Areas, Jukir Accounts, dan Export Reports. Navigasi mempertahankan instance halaman dan input. Sign Out membatalkan request shell, kembali ke Login, dan membersihkan password. Constructor parameterless khusus Designer, tanpa akses DB.

Vehicle details memakai enam `TextBox` readonly; input menggunakan kontrol native, caption dan summary tetap `Label`. Transaksi memakai tarif tetap yang disalin saat masuk, bukan tarif per jam. Plat dinormalisasi uppercase tanpa whitespace (1–20 ASCII alfanumerik), unik selama active lintas lokasi. Kapasitas dihitung dari transaksi active. Pembayaran shift lain pada lokasi sama dicatat atas petugas penerima, bukan petugas entry.

Login disables maximize and centers its input group horizontally and vertically using native table layout. Workspace pages use percentage-based table layouts and expand with the available width. Below the 850-logical-pixel content minimum, native scrolling preserves access to fields and actions rather than shrinking text or overlapping controls. The shell supports resizing down to 920 × 600 logical pixels. Checks cover workspace widths 920, 1260, and 1600 and Login widths 480, 620, and 900 at the current Windows DPI.

Run checks from this directory:

```powershell
dotnet build ParkingSystem.slnx
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Check-Business.ps1
```

Wrapper menyediakan fixture runtime untuk `Check-Forms.ps1`; checker form standalone memerlukan fixture dan `PARKING_CHECKS_PASSWORD`. Checks meluncurkan seluruh Designer, login nyata dua role, navigasi SPA, state input, masked password, readonly details, dan resize; screenshot berada di `%TEMP%/ParkingSystem-Forms`.

## PostgreSQL setup

Schema awal berada di `ParkingSystem/Database/schema.sql`. Bootstrap eksplisit; aplikasi tidak membuat atau memigrasikan schema saat startup. Siapkan database owner dan role runtime terpisah (runtime bukan superuser, bukan pemilik schema), lalu jalankan:

```powershell
$env:PGPASSWORD = '<password owner>'
.\Initialize-Database.ps1 -ConnectionString 'host=<server LAN> dbname=<database> user=<owner>' -RuntimeRole '<runtime role>'
Remove-Item Env:PGPASSWORD
$env:PARKING_DB_CONNECTION = 'Host=<server LAN>;Database=<database>;Username=<runtime role>;Password=<password runtime>'
.\ParkingSystem\bin\Debug\net10.0-windows\ParkingSystem.exe --create-admin
```

`--create-admin` meminta username, nama, dan password tanpa echo; menolak bila admin sudah ada. Tidak ada kredensial bawaan. Bootstrap kedua memvalidasi versi 1 dan struktur tanpa mereset data. Kredensial koneksi langsung memberi akses DML database; batas kepercayaan adalah LAN dan operator pemegang kredensial, bukan role UI.

`Check-Business.ps1` membuat cluster PostgreSQL 16 sementara pada loopback; tidak memakai database operator. Binary wajib tersedia di `C:\Program Files\PostgreSQL\16\bin` atau path eksplisit `-PostgresBin`.

## Operasi parkir

Tanggal bisnis menggunakan WIB (UTC+7); penyimpanan `timestamptz` dan filter rentang UTC half-open. Riwayat menampilkan entry/pembayaran petugas pada lokasi penugasan, 50 baris per halaman. Tarif lokasi yang berubah tidak mengubah tarif transaksi active. Konfirmasi pembayaran dibatalkan berarti tidak ada completion.

Mutasi mengunci lokasi sebelum akun/transaksi dan memeriksa ulang penugasan aktif, sehingga sesi lama tidak dapat menulis setelah deaktivasi/reassignment. Entry memakai waktu database dan snapshot tarif; completion hanya sekali, dengan penerima pembayaran tersimpan terpisah. Validasi setoran hanya dilakukan Admin dan mencatat Admin tersebut sebagai validator. `TransactionChecks.RunAsync` menggunakan fixture `AuthChecks` untuk concurrency kapasitas/plat/exit, batas WIB, pagination, pergantian shift, dashboard, dan otorisasi validasi setoran; fixture transaksi dibersihkan sebelum smoke UI.

Bukti dicetak setelah commit melalui preview dan dialog printer Windows. Printer gagal atau pembatalan tidak membatalkan transaksi; detail riwayat menyediakan cetak ulang. Tidak ada payment gateway.
