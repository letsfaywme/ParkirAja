namespace ParkingSystem;

/// <summary>
/// Identitas visual aplikasi: ikon jendela dan logo merek.
/// Berkas sumber berada di <c>Assets/</c> (logo.svg sebagai master, app.ico untuk jendela).
/// </summary>
public static class AppBrand
{
    private static Icon? _icon;
    private static Image? _logo;

    /// <summary>
    /// Ikon jendela aplikasi. Dimuat sekali lalu dipakai bersama oleh semua Form.
    /// </summary>
    public static Icon Icon => _icon ??= LoadIcon();

    /// <summary>
    /// Logo merek untuk ditampilkan pada antarmuka (mis. sidebar).
    /// Diambil dari resource tertanam agar tetap tersedia setelah dipublikasikan.
    /// </summary>
    public static Image Logo => _logo ??= LoadLogo();

    private static Icon LoadIcon()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");

        if (File.Exists(path))
            return new Icon(path);

        return Icon.ExtractAssociatedIcon(Application.ExecutablePath)
               ?? SystemIcons.Application;
    }

    private static Image LoadLogo()
    {
        var stream = typeof(AppBrand).Assembly
            .GetManifestResourceStream("ParkingSystem.Assets.logo-256.png");

        if (stream is not null)
        {
            using (stream)
            {
                // Salin ke memori agar stream resource bisa dilepas.
                using var buffer = new MemoryStream();
                stream.CopyTo(buffer);
                buffer.Position = 0;
                return Image.FromStream(buffer);
            }
        }

        return SystemIcons.Application.ToBitmap();
    }
}
