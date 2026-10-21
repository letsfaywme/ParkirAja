using System.Security.Cryptography;

namespace ParkingSystem;

public static class PasswordHash
{
    public static string Hash(string password)
    {
        if (password.Length is < 8 or > 128)
            throw new ArgumentException("Password harus 8–128 karakter.");
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 600000, HashAlgorithmName.SHA256, 32);
        return $"pbkdf2-sha256$600000${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string encoded)
    {
        if (password.Length is < 8 or > 128) return false;
        var parts = encoded.Split('$');
        if (parts.Length != 4 || parts[0] != "pbkdf2-sha256" || parts[1] != "600000") return false;
        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            return salt.Length == 16 && expected.Length == 32 && CryptographicOperations.FixedTimeEquals(
                Rfc2898DeriveBytes.Pbkdf2(password, salt, 600000, HashAlgorithmName.SHA256, 32), expected);
        }
        catch (FormatException) { return false; }
    }
}
