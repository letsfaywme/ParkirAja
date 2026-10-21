using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Globalization;

namespace ParkingSystem;

public static class ReportPdf
{
    public static void Write(Stream destination, ReportDocument report)
    {
        ArgumentNullException.ThrowIfNull(destination);
        var document = new PdfDocument();
        document.Info.Title = "Laporan parkir";
        const double margin = 32, headerHeight = 76, rowHeight = 18;
        var font = new XFont("Segoe UI", 8);
        var bold = new XFont("Segoe UI", 8, XFontStyleEx.Bold);
        var titleFont = new XFont("Segoe UI", 15, XFontStyleEx.Bold);
        var columns = new[] { ("ID", 38d), ("Plat", 70d), ("Jenis", 65d), ("Lokasi", 120d), ("Jukir", 105d), ("Masuk", 105d), ("Keluar", 105d), ("Tarif", 72d) };
        int rowIndex = 0, pageNumber = 0;
        while (rowIndex < report.Rows.Count || pageNumber == 0)
        {
            var page = document.AddPage();
            page.Orientation = PdfSharp.PageOrientation.Landscape;
            page.Size = PdfSharp.PageSize.A4;
            pageNumber++;
            using var graphics = XGraphics.FromPdfPage(page);
            double width = page.Width.Point - margin * 2;
            graphics.DrawString("Laporan parkir", titleFont, XBrushes.Black, new XPoint(margin, 30));
            graphics.DrawString($"Periode: {report.From:dd MMM yyyy} – {report.To:dd MMM yyyy} (WIB)", font, XBrushes.Black, new XPoint(margin, 48));
            graphics.DrawString($"Jukir: {report.JukirFilter ?? "Semua"}   Lokasi: {report.ParkingSpotFilter ?? "Semua"}", font, XBrushes.Black, new XPoint(margin, 62));
            double y = margin + headerHeight;
            double x = margin;
            foreach (var column in columns) { graphics.DrawRectangle(XBrushes.LightGray, x, y, column.Item2, rowHeight); graphics.DrawString(column.Item1, bold, XBrushes.Black, new XRect(x + 3, y + 4, column.Item2 - 6, rowHeight), XStringFormats.TopLeft); x += column.Item2; }
            y += rowHeight;
            int available = Math.Max(1, (int)((page.Height.Point - margin - 28 - y) / rowHeight));
            int count = Math.Min(available, report.Rows.Count - rowIndex);
            for (int i = 0; i < count; i++)
            {
                var row = report.Rows[rowIndex++]; x = margin;
                string[] values = [row.TransactionId.ToString(CultureInfo.InvariantCulture), row.PlateNumber, row.VehicleType == VehicleType.Motorcycle ? "Motor" : "Mobil", row.LocationName, row.ExitJukirName ?? row.EntryJukirName, ParkingTime.Wib(row.EntryTime).ToString("dd/MM HH:mm", CultureInfo.InvariantCulture), row.ExitTime is null ? "-" : ParkingTime.Wib(row.ExitTime.Value).ToString("dd/MM HH:mm", CultureInfo.InvariantCulture), ParkingTime.Money(row.Fare)];
                for (int c = 0; c < columns.Length; c++) { if (i % 2 == 1) graphics.DrawRectangle(new XSolidBrush(XColor.FromArgb(245,245,245)), x, y, columns[c].Item2, rowHeight); graphics.DrawString(values[c], font, XBrushes.Black, new XRect(x + 3, y + 4, columns[c].Item2 - 6, rowHeight), XStringFormats.TopLeft); x += columns[c].Item2; }
                y += rowHeight;
            }
            if (report.Rows.Count == 0) graphics.DrawString("Tidak ada transaksi pada filter ini.", font, XBrushes.Black, new XPoint(margin, y + 24));
            if (rowIndex >= report.Rows.Count) graphics.DrawString($"Total: {ParkingTime.Money(report.Total)}", bold, XBrushes.Black, new XPoint(margin, page.Height.Point - margin));
            graphics.DrawString($"Halaman {pageNumber}", font, XBrushes.Black, new XRect(page.Width.Point - 110, page.Height.Point - margin, 78, 12), XStringFormats.TopRight);
        }
        document.Save(destination, false);
    }
}
