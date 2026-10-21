namespace ParkingSystem;

public partial class ParkingAreas
{
    private UserSession? session;
    private ParkingService? service;
    private int? editingId;
    private TextBox addressTextBox = null!;
    private IReadOnlyList<ParkingSpot> allSpots = Array.Empty<ParkingSpot>();
    private bool runtimeControlsInitialized;
    internal CancellationToken ShellCancellation;
    public void Bind(UserSession session, ParkingService service)
    {
        this.session = session; this.service = service; EnsureRuntimeControls(); _ = RefreshAsync(ShellCancellation);
    }
    private void EnsureRuntimeControls()
    {
        if (runtimeControlsInitialized) return;
        runtimeControlsInitialized = true;
        addressTextBox = new TextBox { PlaceholderText = "Alamat", Visible = false };
        areadetailsPanel.Controls.Add(addressTextBox);
        grid5.AutoGenerateColumns = false;
        grid5.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid5.MultiSelect = false;
        column7.DataPropertyName = nameof(ParkingSpot.Name);
        column8.DataPropertyName = nameof(ParkingSpot.Capacity);
        column9.DataPropertyName = nameof(ParkingSpot.Occupied);
        column10.DataPropertyName = nameof(ParkingSpot.Capacity);
        column11.DataPropertyName = nameof(ParkingSpot.IsActive);
        grid5.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || grid5.Rows[e.RowIndex].DataBoundItem is not ParkingSpot spot) return;
            if (e.ColumnIndex == column10.Index) e.Value = Math.Max(0, spot.Capacity - spot.Occupied).ToString("N0");
            else if (e.ColumnIndex == column11.Index) e.Value = spot.IsActive ? (spot.Occupied >= spot.Capacity ? "Penuh" : "Tersedia") : "Nonaktif";
            else return;
            e.FormattingApplied = true;
        };
        grid5.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0) LoadSelected();
        };
        addButton.Click += (_, _) => ClearEditor();
        editButton.Click += (_, _) => LoadSelected();
        saveButton.Click += async (_, _) => await SaveAsync();
        deleteButton.Click += async (_, _) => await DeactivateAsync();
        searchTextBox.TextChanged += (_, _) => ApplyFilter();
    }
    public async Task RefreshAsync(CancellationToken ct)
    {
        if (service is null || session is null) return;
        EnsureRuntimeControls();
        await UiOperations.RunAsync(this, null, ct, async () =>
        {
            allSpots = await service.GetParkingSpotsAsync(session, ct);
            ApplyFilter();
        });
    }
    private void ApplyFilter()
    {
        var q = searchTextBox.Text.Trim();
        grid5.DataSource = string.IsNullOrEmpty(q)
            ? allSpots.ToList()
            : allSpots.Where(x => x.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || x.Address.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
    }
    private void ClearEditor()
    {
        editingId = null;
        areanameTextBox.Clear();
        addressTextBox.Clear();
        capacityNumericUpDown.Value = 100;
        motorcycleRateNumericUpDown.Value = 2000;
        carRateNumericUpDown.Value = 5000;
        label13.Text = "Area details — Tambah Area Baru";
        saveButton.Text = "Simpan Baru";
    }
    private void LoadSelected()
    {
        if (grid5.CurrentRow?.DataBoundItem is not ParkingSpot p) return;
        editingId = p.Id;
        areanameTextBox.Text = p.Name;
        addressTextBox.Text = p.Address;
        capacityNumericUpDown.Value = p.Capacity;
        motorcycleRateNumericUpDown.Value = p.MotorcycleRate;
        carRateNumericUpDown.Value = p.CarRate;
        label13.Text = $"Area details — Edit: {p.Name}";
        saveButton.Text = "Simpan";
    }
    private async Task SaveAsync()
    {
        if (service is null || session is null) return;
        if (string.IsNullOrWhiteSpace(areanameTextBox.Text))
        {
            MessageBox.Show(this, "Nama area wajib diisi.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var actionText = editingId is null ? "Tambah lokasi baru?" : "Simpan perubahan lokasi?";
        if (MessageBox.Show(this, actionText, "Konfirmasi", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        var address = string.IsNullOrWhiteSpace(addressTextBox.Text) ? areanameTextBox.Text.Trim() : addressTextBox.Text.Trim();
        await UiOperations.RunAsync(this, saveButton, ShellCancellation, async () =>
        {
            await service.SaveParkingSpotAsync(session, editingId, areanameTextBox.Text, address, motorcycleRateNumericUpDown.Value, carRateNumericUpDown.Value, (int)capacityNumericUpDown.Value, ShellCancellation);
            await RefreshAsync(ShellCancellation);
            ClearEditor();
        });
    }
    private async Task DeactivateAsync()
    {
        if (service is null || session is null || grid5.CurrentRow?.DataBoundItem is not ParkingSpot p) return;
        if (MessageBox.Show(this, $"Nonaktifkan lokasi {p.Name}?", "Konfirmasi", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        await UiOperations.RunAsync(this, deleteButton, ShellCancellation, async () =>
        {
            await service.DeactivateParkingSpotAsync(session, p.Id, ShellCancellation);
            await RefreshAsync(ShellCancellation);
            ClearEditor();
        });
    }
}
