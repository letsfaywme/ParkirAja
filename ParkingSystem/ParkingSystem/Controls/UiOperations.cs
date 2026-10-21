namespace ParkingSystem;

internal static class UiOperations
{
    internal static async Task RunAsync(Control owner, Control? action, CancellationToken ct, Func<Task> work)
    {
        if (ct.IsCancellationRequested || owner.IsDisposed) return;
        if (action is not null) action.Enabled = false;
        try { await work(); }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            if (!owner.IsDisposed && !ct.IsCancellationRequested)
                MessageBox.Show(owner, ex is ArgumentException or InvalidOperationException or UnauthorizedAccessException
                    ? ex.Message : "Operasi gagal. Periksa koneksi database dan coba kembali.",
                    "ParkirAja", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { if (action is not null && !action.IsDisposed && !ct.IsCancellationRequested) action.Enabled = true; }
    }

    internal static void ShowTransaction(IWin32Window owner, ParkingTransaction item)
    {
        using var dialog = new Form { Text = $"Detail transaksi #{item.Id}", StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(560, 550), MinimumSize = new Size(500, 500), BackColor = AppTheme.Background, ForeColor = AppTheme.Text };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, ColumnCount = 2, Padding = new Padding(16) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var values = new (string Caption, string Value)[] {
            ("ID", item.Id.ToString()), ("Plat", item.PlateNumber), ("Jenis", item.VehicleType == VehicleType.Motorcycle ? "Motor" : "Mobil"),
            ("Lokasi", item.LocationName), ("Petugas masuk", item.JukirName), ("Masuk (WIB)", ParkingTime.Wib(item.EntryTime).ToString("dd/MM/yyyy HH:mm:ss")),
            ("Keluar (WIB)", item.ExitTime is { } exit ? ParkingTime.Wib(exit).ToString("dd/MM/yyyy HH:mm:ss") : "—"),
            ("Petugas pembayaran", item.ExitJukirName ?? "—"), ("Tarif tetap", ParkingTime.Money(item.RateAtEntry)),
            ("Pembayaran", item.Fare is { } fare ? ParkingTime.Money(fare) : "—"), ("Status", item.Status) };
        foreach (var (caption, value) in values)
        {
            var row = layout.RowCount++;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.Controls.Add(new Label { Text = caption, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 5, 12, 3) }, 0, row);
            layout.Controls.Add(new TextBox { Text = value, ReadOnly = true, Dock = DockStyle.Fill, BackColor = AppTheme.InputBackground, ForeColor = AppTheme.Text }, 1, row);
        }
        var print = new Button { Text = "Cetak ulang bukti", AutoSize = true, BackColor = AppTheme.Primary, ForeColor = AppTheme.Text };
        print.Click += (_, _) => { if (!EntryReceipt.Show(dialog, item)) MessageBox.Show(dialog, "Transaksi tersimpan; bukti belum dicetak."); };
        layout.Controls.Add(print, 1, layout.RowCount++);
        dialog.Controls.Add(layout);
        dialog.ShowDialog(owner);
    }
}
