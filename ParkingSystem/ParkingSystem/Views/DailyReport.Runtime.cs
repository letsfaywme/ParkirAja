namespace ParkingSystem;
public partial class DailyReport
{
    private UserSession? session;
    private ParkingService? service;
    private bool bound;
    internal CancellationToken ShellCancellation;

    public void Bind(UserSession s, ParkingService p)
    {
        session = s;
        service = p;
        if (!bound)
        {
            bound = true;
            reportdateDarkDateTimePicker.Value = ParkingTime.Wib(DateTimeOffset.UtcNow).Date;
            grid38.AutoGenerateColumns = false;
            grid38.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid38.MultiSelect = false;
            column40.DataPropertyName = "VehicleType";
            column41.DataPropertyName = "Transactions";
            column42.DataPropertyName = "Revenue";
            showReportButton.Text = "Refresh";
            showReportButton.Click += (_, _) => _ = RefreshAsync(ShellCancellation);
            submitforValidationButton.Text = "Submit Setoran";
            submitforValidationButton.Click += SubmitClick;
            exportReportButton.Text = "Export PDF";
            exportReportButton.Click += ExportClick;
        }
        _ = RefreshAsync(ShellCancellation);
    }

    public Task RefreshAsync(CancellationToken ct) => LoadAsync(ct);

    private async Task LoadAsync(CancellationToken ct)
    {
        if (service is null || session is null) return;
        await UiOperations.RunAsync(this, showReportButton, ct, async () =>
        {
            var date = DateOnly.FromDateTime(reportdateDarkDateTimePicker.Value);
            var summary = await service.GetDailyReportAsync(session, date, ct);
            label11.Text = summary.TransactionCount.ToString("N0");
            label18.Text = summary.TransactionCount.ToString("N0");
            label25.Text = summary.TransactionCount.ToString("N0");
            label32.Text = ParkingTime.Money(summary.AmountExpected);
            label34.Text = summary.AmountDeposited is { } deposited
                ? (summary.Status == DepositStatus.Pending ? $"Pending: {ParkingTime.Money(deposited)}" : $"Status: {summary.Status} ({ParkingTime.Money(deposited)})")
                : "Belum disetor";
            amountDepositedNumericUpDown.Value = summary.AmountDeposited ?? summary.AmountExpected;
            submitforValidationButton.Enabled = summary.Status != DepositStatus.Matched;
            grid38.DataSource = new[]
            {
                new { VehicleType = "Motorcycle", Transactions = summary.MotorcycleCount.ToString("N0"), Revenue = ParkingTime.Money(summary.MotorcycleRevenue) },
                new { VehicleType = "Car", Transactions = summary.CarCount.ToString("N0"), Revenue = ParkingTime.Money(summary.CarRevenue) },
                new { VehicleType = "Total", Transactions = summary.TransactionCount.ToString("N0"), Revenue = ParkingTime.Money(summary.AmountExpected) }
            };
        });
    }

    private async void SubmitClick(object? s, EventArgs e)
    {
        if (session is null || service is null) return;
        var date = DateOnly.FromDateTime(reportdateDarkDateTimePicker.Value);
        var today = ParkingTime.Today(TimeProvider.System);
        if (date > today)
        {
            MessageBox.Show(this, "Tanggal setoran tidak boleh melebihi hari ini.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (MessageBox.Show(this, $"Kirim setoran sebesar {ParkingTime.Money(amountDepositedNumericUpDown.Value)} untuk tanggal {date:dd MMM yyyy}?", "Konfirmasi Setoran", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        await UiOperations.RunAsync(this, submitforValidationButton, ShellCancellation, async () =>
        {
            await service.SubmitDepositAsync(session, date, amountDepositedNumericUpDown.Value, true, ShellCancellation);
            MessageBox.Show(this, "Setoran berhasil dikirim untuk divalidasi Admin.", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await RefreshAsync(ShellCancellation);
        });
    }

    private async void ExportClick(object? s, EventArgs e)
    {
        if (service is null || session is null) return;
        var date = DateOnly.FromDateTime(reportdateDarkDateTimePicker.Value);
        await UiOperations.RunAsync(this, exportReportButton, ShellCancellation, async () =>
        {
            var report = await service.GetExportReportAsync(session, date, date, session.Id, session.ParkingSpotId, ShellCancellation);
            using var dialog = new SaveFileDialog
            {
                Filter = "PDF (*.pdf)|*.pdf",
                DefaultExt = "pdf",
                AddExtension = true,
                FileName = $"laporan-harian-{date:yyyyMMdd}.pdf",
                OverwritePrompt = true
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            await using var stream = new FileStream(dialog.FileName, FileMode.Create, FileAccess.Write, FileShare.None);
            ReportPdf.Write(stream, report);
            MessageBox.Show(this, "Laporan harian berhasil diekspor ke: " + dialog.FileName, "Export Selesai", MessageBoxButtons.OK, MessageBoxIcon.Information);
        });
    }
}