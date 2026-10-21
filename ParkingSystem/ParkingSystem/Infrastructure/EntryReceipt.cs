using System.Drawing.Printing;

namespace ParkingSystem;

public static class EntryReceipt
{
    public static bool Show(IWin32Window owner, ParkingTransaction transaction)
    {
            var defaultPrinter = new PrinterSettings().PrinterName;
            if (!string.Equals(Environment.GetEnvironmentVariable("PARKING_ENABLE_PRINT_PREVIEW"), "1", StringComparison.Ordinal) || PrinterSettings.InstalledPrinters.Count == 0 || string.IsNullOrWhiteSpace(defaultPrinter)) return false;
         try
        {
            using var document = new PrintDocument { DocumentName = $"Bukti parkir #{transaction.Id}" };
            using var titleFont = new Font("Segoe UI", 16, FontStyle.Bold);
            using var bodyFont = new Font("Segoe UI", 11);
            var printed = false;
            document.PrintPage += (_, e) =>
            {
                if (e.Graphics is null) throw new InvalidOperationException("Printer tidak tersedia.");
                var x = e.MarginBounds.Left;
                var y = (float)e.MarginBounds.Top;
                var width = e.MarginBounds.Width;
                const string title = "BUKTI MASUK PARKIR";
                var heading = e.Graphics.MeasureString(title, titleFont, width);
                e.Graphics.DrawString(title, titleFont, Brushes.Black, new RectangleF(x, y, width, heading.Height));
                y += heading.Height + 24;
                string[] lines =
                [
                    $"Transaksi: #{transaction.Id}",
                    $"Plat: {transaction.PlateNumber}",
                    $"Jenis: {(transaction.VehicleType == VehicleType.Motorcycle ? "Motor" : "Mobil")}",
                    $"Lokasi: {transaction.LocationName}",
                    $"Masuk: {ParkingTime.Wib(transaction.EntryTime):dd MMM yyyy HH:mm:ss} WIB",
                    $"Tarif tetap: {ParkingTime.Money(transaction.RateAtEntry)}",
                    "Simpan bukti ini sampai kendaraan keluar."
                ];
                foreach (var line in lines)
                {
                    var size = e.Graphics.MeasureString(line, bodyFont, width);
                    e.Graphics.DrawString(line, bodyFont, Brushes.Black, new RectangleF(x, y, width, size.Height));
                    y += size.Height + 12;
                }
                e.HasMorePages = false;
            };
            document.EndPrint += (_, e) =>
            {
                if (!document.PrintController.IsPreview && !e.Cancel) printed = true;
            };
            using (var preview = new PrintPreviewDialog
            {
                Document = document,
                Text = $"Bukti parkir #{transaction.Id}",
                Width = 900,
                Height = 700,
                UseAntiAlias = true,
                StartPosition = FormStartPosition.CenterParent
            })
            {
                preview.ShowDialog(owner);
            }
            if (printed) return true;
            if (MessageBox.Show(owner, "Cetak bukti parkir sekarang?", "Cetak bukti",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return false;
            using var dialog = new PrintDialog { Document = document, UseEXDialog = true, AllowSomePages = false, AllowSelection = false };
            if (dialog.ShowDialog(owner) != DialogResult.OK) return false;
            document.Print();
            return printed;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException and not AccessViolationException)
        {
            // Caller reports persisted entry and offers reprint; printer errors never undo DB commit.
            return false;
        }
    }
}
