namespace ParkingSystem;

public partial class AdminDashboard
{
    private UserSession? session;
    private ParkingService? service;
    internal CancellationToken ShellCancellation;
    private bool runtimeBound;
    private bool refreshing;
    private bool openingDetail;

    public void Bind(UserSession session, ParkingService service)
    {
        this.session = session;
        this.service = service;
        if (runtimeBound) return;
        runtimeBound = true;
        label6.Text = label13.Text = label20.Text = label27.Text = "—";
        label8.Text = label15.Text = "Memuat data…";
        label22.Text = "Ruang tersedia";
        label29.Text = "Pendapatan hari ini (WIB)";
        label32.Text = "Memuat lokasi…";
        grid33.AutoGenerateColumns = false;
        grid33.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid33.MultiSelect = false;
        column35.DataPropertyName = nameof(ParkingSpot.Name);
        column36.DataPropertyName = nameof(ParkingSpot.Capacity);
        column37.DataPropertyName = nameof(ParkingSpot.Occupied);
        column38.DataPropertyName = nameof(ParkingSpot.Capacity);
        column39.DataPropertyName = nameof(ParkingSpot.IsActive);
        grid33.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || grid33.Rows[e.RowIndex].DataBoundItem is not ParkingSpot spot) return;
            if (e.ColumnIndex == column38.Index) e.Value = Math.Max(0, spot.Capacity - spot.Occupied).ToString("N0");
            else if (e.ColumnIndex == column39.Index) e.Value = spot.Occupied >= spot.Capacity ? "Penuh" : "Tersedia";
            else return;
            e.FormattingApplied = true;
        };
        grid33.CellDoubleClick += async (_, e) =>
        {
            if (e.RowIndex < 0 || openingDetail || grid33.Rows[e.RowIndex].DataBoundItem is not ParkingSpot spot) return;
            openingDetail = true;
            try
            {
                await UiOperations.RunAsync(this, grid33, ShellCancellation, () => ShowLocationAsync(spot, ShellCancellation));
            }
            finally { openingDetail = false; }
        };
        refreshButton.Click += async (_, _) =>
            await UiOperations.RunAsync(this, refreshButton, ShellCancellation, () => RefreshAsync(ShellCancellation));
        components ??= new System.ComponentModel.Container();
        var refreshTimer = new System.Windows.Forms.Timer(components) { Interval = 30_000 };
        refreshTimer.Tick += async (_, _) =>
        {
            if (!Visible || refreshing || openingDetail || ShellCancellation.IsCancellationRequested) return;
            await UiOperations.RunAsync(this, refreshButton, ShellCancellation, () => RefreshAsync(ShellCancellation));
        };
        VisibleChanged += (_, _) =>
        {
            if (IsDisposed || Disposing) return;
            refreshTimer.Enabled = Visible && !ShellCancellation.IsCancellationRequested;
        };
        refreshTimer.Enabled = Visible && !ShellCancellation.IsCancellationRequested;
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
            var spots = summary.Spots.Where(spot => spot.IsActive).ToList();
            var capacity = spots.Sum(spot => (long)spot.Capacity);
            var occupied = spots.Sum(spot => (long)spot.Occupied);
            label6.Text = capacity.ToString("N0");
            label13.Text = occupied.ToString("N0");
            label20.Text = Math.Max(0, capacity - occupied).ToString("N0");
            label27.Text = ParkingTime.Money(summary.Revenue);
            label8.Text = $"{spots.Count} lokasi aktif";
            label15.Text = capacity == 0 ? "0% terisi" : $"{occupied * 100m / capacity:0.#}% terisi";
            grid33.DataSource = spots;
            label32.Text = spots.Count == 0 ? "Belum ada lokasi aktif." : "Status lokasi parkir — klik dua kali untuk transaksi";
        }
        finally
        {
            refreshing = false;
            if (!IsDisposed) refreshButton.Enabled = true;
        }
    }

    private async Task ShowLocationAsync(ParkingSpot spot, CancellationToken cancellationToken)
    {
        if (service is null || session is null) return;
        var transactions = await service.GetLocationTransactionsAsync(session, spot.Id, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (IsDisposed) return;
        using var dialog = new Form
        {
            Text = $"Transaksi — {spot.Name}", StartPosition = FormStartPosition.CenterParent,
            Size = new Size(1000, 520), MinimumSize = new Size(650, 350),
            BackColor = BackColor, ForeColor = ForeColor, Font = Font,
            MinimizeBox = false, ShowInTaskbar = false
        };
        var table = new DataGridView
        {
            Dock = DockStyle.Fill, AutoGenerateColumns = false, ReadOnly = true,
            AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = grid33.BackgroundColor, GridColor = grid33.GridColor,
            EnableHeadersVisualStyles = false,
            ColumnHeadersDefaultCellStyle = grid33.ColumnHeadersDefaultCellStyle.Clone(),
            DefaultCellStyle = grid33.DefaultCellStyle.Clone(),
            DataSource = transactions.ToList()
        };
        table.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Plat", DataPropertyName = nameof(ParkingTransaction.PlateNumber) });
        table.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Jenis", DataPropertyName = nameof(ParkingTransaction.VehicleType) });
        table.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Masuk (WIB)", DataPropertyName = nameof(ParkingTransaction.EntryTime) });
        table.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Keluar (WIB)", DataPropertyName = nameof(ParkingTransaction.ExitTime) });
        table.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tarif dibayar", DataPropertyName = nameof(ParkingTransaction.Fare) });
        table.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status", DataPropertyName = nameof(ParkingTransaction.Status) });
        table.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0) return;
            if (e.Value is DateTimeOffset time) e.Value = ParkingTime.Wib(time).ToString("dd MMM yyyy HH:mm");
            else if (e.ColumnIndex == 4) e.Value = e.Value is decimal fare ? ParkingTime.Money(fare) : "Belum dibayar";
            else return;
            e.FormattingApplied = true;
        };
        table.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && table.Rows[e.RowIndex].DataBoundItem is ParkingTransaction transaction)
                UiOperations.ShowTransaction(dialog, transaction);
        };
        var caption = new Label
        {
            Dock = DockStyle.Top, Height = 40, Padding = new Padding(12), AutoEllipsis = true,
            Text = transactions.Count == 0 ? "Tidak ada transaksi di lokasi ini." : $"{transactions.Count} transaksi — klik dua kali untuk detail"
        };
        dialog.Controls.Add(table);
        dialog.Controls.Add(caption);
        dialog.ShowDialog(this);
    }
}
