using ParkingSystem;
using PdfSharp.Pdf.IO;

#pragma warning disable CS0618
public static class PdfChecks
{
    public static Task RunAsync()
    {
        var rows = Enumerable.Range(1, 120).Select(i => new ReportRow(i, "B1234XYZ", VehicleType.Motorcycle,
            "Lokasi Uji – Jakarta", "Jukir Ä", "Jukir É", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(5), 2000m, "completed")).ToArray();
        var report = new ReportDocument(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), "Jukir Ä", "Lokasi Uji – Jakarta", rows);
        using var stream = new MemoryStream();
        ReportPdf.Write(stream, report);
        stream.Position = 0;
        using var document = PdfReader.Open(stream, PdfDocumentOpenMode.ReadOnly);
#pragma warning restore CS0618
        if (document.PageCount < 2) throw new Exception("PDF report did not paginate.");
        Console.WriteLine($"PASS PDF report ({document.PageCount} pages, {rows.Length} rows)");
        return Task.CompletedTask;
    }
}
