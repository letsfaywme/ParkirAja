namespace ParkingSystem;

public partial class VehicleEntry
{
    private UserSession? session;
    private ParkingService? service;
    private bool bound;
    private bool saving;
    private ParkingSpot? currentSpot;
    internal CancellationToken ShellCancellation;

    public void Bind(UserSession session, ParkingService service)
    {
        this.session = session;
        this.service = service;
        if (bound) return;
        bound = true;
        platenumberTextBox.PlaceholderText = "Masukkan plat kendaraan";
        parkingareaComboBox.Items.Clear();
        parkingareaComboBox.DisplayMember = nameof(ParkingSpot.Name);
        parkingareaComboBox.ValueMember = nameof(ParkingSpot.Id);
        parkingareaComboBox.Enabled = false;
        vehicletypeComboBox.Items.Clear();
        vehicletypeComboBox.DataSource = Enum.GetValues<VehicleType>();
        vehicletypeComboBox.SelectedIndexChanged += (_, _) => UpdateRateDisplay();
        grid22.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid22.MultiSelect = false;
        label21.Text = "Transaksi hari ini";
        saveEntryButton.Click += SaveEntry;
        clearButton.Click += (_, _) =>
        {
            if (saving) return;
            platenumberTextBox.Clear();
            vehicletypeComboBox.SelectedItem = VehicleType.Motorcycle;
            platenumberTextBox.Focus();
        };
        grid22.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && grid22.Rows[e.RowIndex].Tag is ParkingTransaction transaction)
                UiOperations.ShowTransaction(this, transaction);
        };
    }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        if (service is null || session is null) return;
        var spot = await service.GetAssignedSpotAsync(session, cancellationToken);
        var history = await service.GetHistoryAsync(session, ParkingTime.Today(TimeProvider.System), null, null, 1, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (parkingareaComboBox.Items.Count == 0)
        {
            parkingareaComboBox.Items.Add(spot);
            parkingareaComboBox.SelectedIndex = 0;
        }
        else parkingareaComboBox.Items[0] = spot;
        currentSpot = spot;
        UpdateRateDisplay();
        grid22.Rows.Clear();
        foreach (var transaction in history.Rows)
        {
            var index = grid22.Rows.Add(ParkingTime.Wib(transaction.EntryTime).ToString("dd MMM yyyy HH:mm"),
                transaction.PlateNumber, transaction.VehicleType, transaction.LocationName, transaction.Status);
            grid22.Rows[index].Tag = transaction;
        }
        label21.Text = history.Total == 0 ? "Tidak ada transaksi hari ini." : $"Transaksi hari ini ({history.Total}; terbaru 50)";
    }
    private void UpdateRateDisplay()
    {
        if (currentSpot is null) return;
        var isMotorcycle = (vehicletypeComboBox.SelectedItem as VehicleType? ?? VehicleType.Motorcycle) == VehicleType.Motorcycle;
        var rate = isMotorcycle ? currentSpot.MotorcycleRate : currentSpot.CarRate;
        rateLabel.Text = ParkingTime.Money(rate);
    }
    private async void SaveEntry(object? sender, EventArgs e)
    {
        if (saving || service is null || session is null) return;
        var saved = false;
        saving = true;
        registervehiclePanel.Enabled = false;
        try
        {
            await UiOperations.RunAsync(this, saveEntryButton, ShellCancellation, async () =>
            {
                if (vehicletypeComboBox.SelectedItem is not VehicleType type)
                    throw new ArgumentException("Pilih jenis kendaraan.");
                var transaction = await service.EnterVehicleAsync(session, platenumberTextBox.Text, type, ShellCancellation);
                saved = true;
                platenumberTextBox.Clear();
                // Committed entry survives print cancellation or printer failure.
                if (!ShellCancellation.IsCancellationRequested && !IsDisposed)
                {
                    var printed = EntryReceipt.Show(this, transaction);
                    MessageBox.Show(this, printed ? "Transaksi tersimpan; bukti dicetak." : "Transaksi tersimpan; bukti belum dicetak.",
                        "Vehicle Entry", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            });
        }
        finally
        {
            saving = false;
            if (!IsDisposed) registervehiclePanel.Enabled = true;
        }
        if (saved && !ShellCancellation.IsCancellationRequested && !IsDisposed)
            await UiOperations.RunAsync(this, saveEntryButton, ShellCancellation, () => RefreshAsync(ShellCancellation));
    }
}
