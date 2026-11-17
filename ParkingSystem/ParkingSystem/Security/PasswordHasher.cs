using System.Security.Cryptography;

namespace ParkingSystem.Security;

/// <summary>
/// Layanan hashing dan verifikasi kata sandi berbasis algoritma standar industri PBKDF2 (HMAC-SHA256).
/// Memenuhi rekomendasi keamanan OWASP dan standar email-and-password-best-practices.
/// </summary>
public static class PasswordHasher
{
    private const int SaltSize = 16;       // 128 bit
    private const int KeySize = 32;        // 256 bit
    private const int Iterations = 100_000; // 100k iterasi SHA-256
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;
    private const char SegmentDelimiter = ':';

    /// <summary>
    /// Menghasilkan hash bergaram (salted hash) dari kata sandi teks biasa menggunakan PBKDF2.
    /// Format keluaran: {iterasi}:{saltBase64}:{hashBase64}
    /// </summary>
    /// <param name="password">Kata sandi teks biasa</param>
    /// <returns>String hash PBKDF2 terenkapsulasi</returns>
    public static string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty.", nameof(password));

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);

        return string.Join(
            SegmentDelimiter,
            Iterations,
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash)
        );
    }

    /// <summary>
    /// Memverifikasi kata sandi teks biasa terhadap string hash PBKDF2 tersimpan.
    /// Menggunakan perbandingan waktu-konstan (FixedTimeEquals) untuk mencegah serangan timing.
    /// Mendukung fallback aman jika string tersimpan adalah teks biasa (prototipe warisan).
    /// </summary>
    /// <param name="password">Kata sandi yang dimasukkan pengguna</param>
    /// <param name="storedHash">Hash yang tersimpan di database</param>
    /// <returns>True jika kata sandi cocok, false jika tidak</returns>
    public static bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
            return false;

        string[] segments = storedHash.Split(SegmentDelimiter);
        if (segments.Length != 3)
        {
            // Fallback backward-compatibility untuk seed/migrasi plaintext sementara
            return CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(password),
                System.Text.Encoding.UTF8.GetBytes(storedHash)
            );
        }

        if (!int.TryParse(segments[0], out int iterations))
            return false;

        byte[] salt = Convert.FromBase64String(segments[1]);
        byte[] expectedHash = Convert.FromBase64String(segments[2]);

        byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
