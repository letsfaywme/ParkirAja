namespace ParkingSystem;

public partial class TransactionHistory
{
    private UserSession? session;
    private ParkingService? service;
    internal CancellationToken ShellCancellation;
    private bool runtimeBound;
    private bool refreshing;
    private int page = 1;
    private int total;
    private DateOnly filterDate;
    private VehicleType? filterType;
    private string? filterPlate;

    public void Bind(UserSession session, ParkingService service)
    {
        this.session = session;
        this.service = service;
        if (runtimeBound) return;
        runtimeBound = true;
        filterDate = ParkingTime.Today(TimeProvider.System);
        dateDarkDateTimePicker.Value = filterDate.ToDateTime(TimeOnly.MinValue);
        vehicletypeComboBox.Items.Clear();
        vehicletypeComboBox.DisplayMember = "Key";
        vehicletypeComboBox.ValueMember = "Value";
        vehicletypeComboBox.DataSource = new KeyValuePair<string, VehicleType?>[]
        {
            new("All types", null),
            new("Motorcycle", VehicleType.Motorcycle),
            new("Car", VehicleType.Car)
        };
        grid21.AutoGenerateColumns = false;
        grid21.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid21.MultiSelect = false;
        column23.DataPropertyName = nameof(ParkingTransaction.EntryTime);
        column24.DataPropertyName = nameof(ParkingTransaction.ExitTime);
        column25.DataPropertyName = nameof(ParkingTransaction.PlateNumber);
        column26.DataPropertyName = nameof(ParkingTransaction.VehicleType);
        column27.DataPropertyName = nameof(ParkingTransaction.EntryTime);
        column28.DataPropertyName = nameof(ParkingTransaction.Fare);
        grid21.CellFormatting += FormatTransaction;
        grid21.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && grid21.Rows[e.RowIndex].DataBoundItem is ParkingTransaction transaction)
                UiOperations.ShowTransaction(this, transaction);
        };
        searchButton.Click += async (_, _) => await SearchAsync();
        previousButton.Click += async (_, _) => await ChangePageAsync(previousButton, page - 1);
        nextButton.Click += async (_, _) => await ChangePageAsync(nextButton, page + 1);
        exportButton.Click += async (_, _) => await ExportAsync();
        platenumberTextBox.KeyDown += async (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            await SearchAsync();
        };
        label20.Text = "Memuat transaksi…";
        UpdateNavigation();
    }
    private async Task ExportAsync()
    {
        if (service is null || session is null || refreshing) return;
        await UiOperations.RunAsync(this, exportButton, ShellCancellation, async () =>
        {
            var date = DateOnly.FromDateTime(dateDarkDateTimePicker.Value);
            var report = await service.GetExportReportAsync(session, date, date, session.Role == UserRole.Jukir ? session.Id : null, session.ParkingSpotId, ShellCancellation);
            using var dialog = new SaveFileDialog
            {
                Filter = "PDF (*.pdf)|*.pdf",
                DefaultExt = "pdf",
                AddExtension = true,
                FileName = $"transaksi-{date:yyyyMMdd}.pdf",
                OverwritePrompt = true
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            await using var stream = new FileStream(dialog.FileName, FileMode.Create, FileAccess.Write, FileShare.None);
            ReportPdf.Write(stream, report);
            MessageBox.Show(this, "Riwayat transaksi berhasil diekspor ke: " + dialog.FileName, "Export Selesai", MessageBoxButtons.OK, MessageBoxIcon.Information);
        });
    }

    private async Task SearchAsync()
    {
        if (refreshing) return;
        filterDate = DateOnly.FromDateTime(dateDarkDateTimePicker.Value);
        filterType = vehicletypeComboBox.SelectedItem is KeyValuePair<string, VehicleType?> choice ? choice.Value : null;
        filterPlate = platenumberTextBox.Text;
        await ChangePageAsync(searchButton, 1);
    }

    private async Task ChangePageAsync(Control action, int requestedPage)
    {
        if (refreshing || requestedPage < 1) return;
        await UiOperations.RunAsync(this, action, ShellCancellation,
            () => LoadHistoryAsync(requestedPage, ShellCancellation));
        UpdateNavigation();
    }

    public Task RefreshAsync(CancellationToken cancellationToken) => LoadHistoryAsync(page, cancellationToken);

    private async Task LoadHistoryAsync(int requestedPage, CancellationToken cancellationToken)
    {
        if (service is null || session is null || refreshing) return;
        refreshing = true;
        searchButton.Enabled = previousButton.Enabled = nextButton.Enabled = false;
        try
        {
            var result = await service.GetHistoryAsync(session, filterDate, filterType, filterPlate, requestedPage, cancellationToken);
            var lastPage = Math.Max(1, (int)Math.Ceiling(result.Total / 50d));
            if (result.Total > 0 && requestedPage > lastPage)
                result = await service.GetHistoryAsync(session, filterDate, filterType, filterPlate, lastPage, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (IsDisposed) return;
            page = result.Total == 0 ? 1 : result.Page;
            total = result.Total;
            grid21.DataSource = result.Rows.ToList();
            label20.Text = total == 0 ? "Tidak ada transaksi pada tanggal/filter ini." : $"Transaksi harian — {total} transaksi";
        }
        finally
        {
            refreshing = false;
            if (!IsDisposed)
            {
                searchButton.Enabled = true;
                UpdateNavigation();
            }
        }
    }

    private void UpdateNavigation()
    {
        var pages = Math.Max(1, (int)Math.Ceiling(total / 50d));
        label31.Text = $"{page} / {pages}";
        previousButton.Enabled = !refreshing && page > 1;
        nextButton.Enabled = !refreshing && page < pages;
    }

    private void FormatTransaction(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || grid21.Rows[e.RowIndex].DataBoundItem is not ParkingTransaction transaction) return;
        if (e.ColumnIndex == column23.Index) e.Value = ParkingTime.Wib(transaction.EntryTime).ToString("dd MMM yyyy HH:mm");
        else if (e.ColumnIndex == column24.Index) e.Value = transaction.ExitTime is { } exit ? ParkingTime.Wib(exit).ToString("dd MMM yyyy HH:mm") : "—";
        else if (e.ColumnIndex == column26.Index) e.Value = transaction.VehicleType == VehicleType.Motorcycle ? "Motorcycle" : "Car";
        else if (e.ColumnIndex == column27.Index)
        {
            var duration = (transaction.ExitTime ?? TimeProvider.System.GetUtcNow()) - transaction.EntryTime;
            if (duration < TimeSpan.Zero) duration = TimeSpan.Zero;
            e.Value = $"{(long)duration.TotalHours}j {duration.Minutes}m";
        }
        else if (e.ColumnIndex == column28.Index) e.Value = transaction.Fare is { } fare ? ParkingTime.Money(fare) : "Belum dibayar";
        else return;
        e.FormattingApplied = true;
    }
}
