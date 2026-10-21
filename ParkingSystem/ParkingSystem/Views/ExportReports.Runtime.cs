namespace ParkingSystem;

public partial class ExportReports
{
    private UserSession? session;
    private ParkingService? service;
    private ReportDocument? report;
    internal CancellationToken ShellCancellation;

    public void Bind(UserSession session, ParkingService service)
    {
        this.session = session;
        this.service = service;
        if (session.Role != UserRole.Admin)
        {
            Enabled = false;
            return;
        }
        ConfigureRuntime();
        _ = LoadFiltersAsync(ShellCancellation);
    }

    private void ConfigureRuntime()
    {
        var today = ParkingTime.Today(TimeProvider.System);
        fromDarkDateTimePicker.Value = today.AddDays(-7).ToDateTime(TimeOnly.MinValue);
        toDarkDateTimePicker.Value = today.ToDateTime(TimeOnly.MinValue);
        grid24.AutoGenerateColumns = false;
        grid24.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid24.MultiSelect = false;
        column26.DataPropertyName = "Date";
        column27.DataPropertyName = "Jukir";
        column28.DataPropertyName = "Plate";
        column29.DataPropertyName = "Vehicle";
        column30.DataPropertyName = "Fee";
        jukirComboBox.Items.Clear();
        parkingareaComboBox.Items.Clear();
        jukirComboBox.Items.Add("All jukir");
        parkingareaComboBox.Items.Add("All areas");
        jukirComboBox.SelectedIndex = 0;
        parkingareaComboBox.SelectedIndex = 0;
        generatePreviewButton.Click -= PreviewClick;
        exportFileButton.Click -= ExportClick;
        generatePreviewButton.Click += PreviewClick;
        exportFileButton.Click += ExportClick;
    }

    private async Task LoadFiltersAsync(CancellationToken ct)
    {
        if (service is null || session is null || session.Role != UserRole.Admin) return;
        await UiOperations.RunAsync(this, null, ct, async () =>
        {
            var jukir = await service.GetJukirAsync(session, ct);
            var spots = await service.GetParkingSpotsAsync(session, ct);
            jukirComboBox.Items.Clear(); jukirComboBox.Items.Add("All jukir");
            foreach (var item in jukir.Where(x => x.IsActive)) jukirComboBox.Items.Add(item);
            parkingareaComboBox.Items.Clear(); parkingareaComboBox.Items.Add("All areas");
            foreach (var item in spots.Where(x => x.IsActive)) parkingareaComboBox.Items.Add(item);
            jukirComboBox.SelectedIndex = 0; parkingareaComboBox.SelectedIndex = 0;
        });
    }

    private async void PreviewClick(object? sender, EventArgs e) => await LoadReportAsync(generatePreviewButton, ShellCancellation);

    private async Task LoadReportAsync(Control action, CancellationToken ct)
    {
        if (service is null || session is null || session.Role != UserRole.Admin) return;
        var from = DateOnly.FromDateTime(fromDarkDateTimePicker.Value);
        var to = DateOnly.FromDateTime(toDarkDateTimePicker.Value);
        if (from > to)
        {
            MessageBox.Show(this, "Tanggal awal tidak boleh melebihi tanggal akhir.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        await UiOperations.RunAsync(this, action, ct, async () =>
        {
            var jukirId = (jukirComboBox.SelectedItem as JukirAccount)?.Id;
            var spotId = (parkingareaComboBox.SelectedItem as ParkingSpot)?.Id;
            report = await service.GetExportReportAsync(session, from, to, jukirId, spotId, ct);
            grid24.DataSource = report.Rows.Select(x => new
            {
                Date = ParkingTime.Wib(x.ExitTime ?? x.EntryTime).ToString("dd MMM yyyy HH:mm"),
                Jukir = x.ExitJukirName ?? x.EntryJukirName,
                Plate = x.PlateNumber,
                Vehicle = x.VehicleType == VehicleType.Motorcycle ? "Motor" : "Mobil",
                Fee = ParkingTime.Money(x.Fare)
            }).ToList();
            label23.Text = report.Rows.Count == 0
                ? "Tidak ada transaksi pada periode ini."
                : $"Preview Laporan ({report.Rows.Count} transaksi, Total: {ParkingTime.Money(report.Total)})";
        });
    }

    private async void ExportClick(object? sender, EventArgs e)
    {
        if (service is null || session is null || session.Role != UserRole.Admin) return;
        await UiOperations.RunAsync(this, exportFileButton, ShellCancellation, async () =>
        {
            await LoadReportAsync(exportFileButton, ShellCancellation);
            if (report is null) return;
            using var dialog = new SaveFileDialog
            {
                Filter = "PDF (*.pdf)|*.pdf",
                DefaultExt = "pdf",
                AddExtension = true,
                FileName = $"laporan-parkir-{report.From:yyyyMMdd}-{report.To:yyyyMMdd}.pdf",
                OverwritePrompt = true
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            await using var stream = new FileStream(dialog.FileName, FileMode.Create, FileAccess.Write, FileShare.None);
            ReportPdf.Write(stream, report);
            MessageBox.Show(this, "Laporan berhasil diekspor ke: " + dialog.FileName, "Export Selesai", MessageBoxButtons.OK, MessageBoxIcon.Information);
        });
    }

    public Task RefreshAsync(CancellationToken cancellationToken) =>
        service is null || session is null ? Task.CompletedTask : LoadFiltersAsync(cancellationToken);
}
