namespace ParkingSystem;

public partial class JukirDashboard
{
    private UserSession? session;
    private ParkingService? service;
    internal CancellationToken ShellCancellation;
    private bool runtimeBound;
    private bool refreshing;

    public void Bind(UserSession session, ParkingService service)
    {
        this.session = session;
        this.service = service;
        if (runtimeBound) return;
        runtimeBound = true;
        label6.Text = label13.Text = label20.Text = "—";
        label8.Text = "Kendaraan masuk hari ini";
        label15.Text = "Pembayaran diterima hari ini";
        label22.Text = "Pendapatan diterima hari ini";
        label25.Text = "Memuat transaksi…";
        grid26.AutoGenerateColumns = false;
        grid26.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid26.MultiSelect = false;
        column28.DataPropertyName = nameof(ParkingTransaction.EntryTime);
        column29.DataPropertyName = nameof(ParkingTransaction.PlateNumber);
        column30.DataPropertyName = nameof(ParkingTransaction.VehicleType);
        column31.DataPropertyName = nameof(ParkingTransaction.Fare);
        grid26.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || grid26.Rows[e.RowIndex].DataBoundItem is not ParkingTransaction transaction) return;
            if (e.ColumnIndex == column28.Index)
                e.Value = ParkingTime.Wib(transaction.ExitTime ?? transaction.EntryTime).ToString("dd MMM yyyy HH:mm");
            else if (e.ColumnIndex == column30.Index)
                e.Value = transaction.VehicleType == VehicleType.Motorcycle ? "Motorcycle" : "Car";
            else if (e.ColumnIndex == column31.Index)
                e.Value = transaction.Fare is { } fare ? ParkingTime.Money(fare) : "Belum dibayar";
            else return;
            e.FormattingApplied = true;
        };
        grid26.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && grid26.Rows[e.RowIndex].DataBoundItem is ParkingTransaction transaction)
                UiOperations.ShowTransaction(this, transaction);
        };
        refreshButton.Click += async (_, _) =>
            await UiOperations.RunAsync(this, refreshButton, ShellCancellation, () => RefreshAsync(ShellCancellation));
    }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        if (service is null || session is null || refreshing) return;
        refreshing = true;
        refreshButton.Enabled = false;
        try
        {
            var summary = await service.GetDashboardAsync(session, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (IsDisposed) return;
            label6.Text = summary.Entries.ToString("N0");
            label13.Text = summary.Exits.ToString("N0");
            label20.Text = ParkingTime.Money(summary.Revenue);
            grid26.DataSource = summary.Latest.ToList();
            label25.Text = summary.Latest.Count == 0 ? "Belum ada transaksi." : "Transaksi terbaru";
        }
        finally
        {
            refreshing = false;
            if (!IsDisposed) refreshButton.Enabled = true;
        }
    }
}
