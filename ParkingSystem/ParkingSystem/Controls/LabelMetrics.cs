namespace ParkingSystem;

/// <summary>
/// Menyesuaikan tinggi kotak label agar teksnya tidak terpotong.
/// Designer menyimpan tinggi label dalam satuan 96 DPI, sedangkan pada DPI lebih tinggi
/// tinggi baris font (mis. 8pt Segoe UI = 19px) melebihi tinggi kotak tersebut.
/// </summary>
internal static class LabelMetrics
{
    /// <summary>
    /// Memastikan setiap label statis di dalam <paramref name="root"/> cukup tinggi untuk
    /// menampilkan teksnya utuh, tanpa mengubah font, warna, maupun posisi horizontalnya.
    /// </summary>
    internal static void EnsureReadable(Control root)
    {
        foreach (Control child in root.Controls)
        {
            if (child is Label label && label.Dock != DockStyle.Fill && !string.IsNullOrEmpty(label.Text))
            {
                int needed = TextRenderer.MeasureText(label.Text, label.Font).Height;
                if (needed > label.Height)
                {
                    label.Height = needed;
                }
            }

            EnsureReadable(child);
        }
    }
}
