namespace ParkingSystem;

/// <summary>
/// Kontrak antarmuka untuk komponen halaman (UserControl) yang membutuhkan konteks sesi pengguna aktif.
/// Memastikan atribusi identitas transaksi dan pembatasan hak akses berbasis sesi.
/// </summary>
public interface ISessionAware
{
    /// <summary>
    /// Mengaitkan data sesi pengguna login aktif ke halaman.
    /// </summary>
    /// <param name="session">Objek UserSession yang memuat ID, Nama, Role, dan ParkingSpotId</param>
    void BindSession(UserSession session);
}

/// <summary>
/// Penanda bahwa sebuah halaman diizinkan beroperasi penuh (halaman yang didemokan).
/// </summary>
public interface IDemoPage
{
    /// <summary>
    /// Dipanggil setiap kali pengguna berpindah ke halaman lain / menutup workspace,
    /// sehingga halaman demo dapat membersihkan state yang belum disimpan.
    /// </summary>
    void OnDemoPageLeft();
}
