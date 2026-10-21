namespace ParkingSystem;

public partial class JukirAccounts
{
    private UserSession? session; private ParkingService? service; private int? editingId;
    private bool runtimeControlsInitialized; internal CancellationToken ShellCancellation;
    public void Bind(UserSession session, ParkingService service){this.session=session;this.service=service;EnsureRuntimeControls();_=RefreshAsync(ShellCancellation);}
    private void EnsureRuntimeControls()
    {
        if (runtimeControlsInitialized) return;
        runtimeControlsInitialized = true;
        grid5.AutoGenerateColumns = false;
        grid5.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid5.MultiSelect = false;
        column7.DataPropertyName = nameof(JukirAccount.Name);
        column8.DataPropertyName = nameof(JukirAccount.Username);
        column9.DataPropertyName = nameof(JukirAccount.LocationName);
        column10.DataPropertyName = nameof(JukirAccount.IsActive);
        grid5.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || grid5.Rows[e.RowIndex].DataBoundItem is not JukirAccount a) return;
            if (e.ColumnIndex == column10.Index)
            {
                e.Value = a.IsActive ? "Aktif" : "Nonaktif";
                e.FormattingApplied = true;
            }
        };
        grid5.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0) LoadSelected();
        };
        addButton.Click += async (_, _) => await SaveAsync();
        editButton.Click += (_, _) => LoadSelected();
        deleteButton.Click += async (_, _) => await DeactivateAsync();
        assignedareaComboBox.Items.Clear();
        assignedareaComboBox.DisplayMember = nameof(ParkingSpot.Name);
        assignedareaComboBox.ValueMember = nameof(ParkingSpot.Id);
    }
    public async Task RefreshAsync(CancellationToken ct)
    {
        if (service is null || session is null) return;
        EnsureRuntimeControls();
        await UiOperations.RunAsync(this, null, ct, async () =>
        {
            var spots = await service.GetParkingSpotsAsync(session, ct);
            assignedareaComboBox.DataSource = spots.Where(x => x.IsActive).ToList();
            var rows = await service.GetJukirAsync(session, ct);
            grid5.DataSource = rows.ToList();
        });
    }
    private void ClearEditor()
    {
        editingId = null;
        jukirnameTextBox.Clear();
        usernameTextBox.Clear();
        passwordTextBox.Clear();
        if (assignedareaComboBox.Items.Count > 0) assignedareaComboBox.SelectedIndex = 0;
        label12.Text = "Account details — Tambah Akun Baru";
        addButton.Text = "Tambah";
    }
    private void LoadSelected()
    {
        if (grid5.CurrentRow?.DataBoundItem is not JukirAccount a) return;
        editingId = a.Id;
        jukirnameTextBox.Text = a.Name;
        usernameTextBox.Text = a.Username;
        passwordTextBox.Clear();
        assignedareaComboBox.SelectedValue = a.ParkingSpotId;
        label12.Text = $"Account details — Edit: {a.Name}";
        addButton.Text = "Simpan";
    }
    private async Task SaveAsync()
    {
        if (service is null || session is null || assignedareaComboBox.SelectedValue is not int spot) return;
        if (string.IsNullOrWhiteSpace(jukirnameTextBox.Text) || string.IsNullOrWhiteSpace(usernameTextBox.Text))
        {
            MessageBox.Show(this, "Nama jukir dan username wajib diisi.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (editingId is null && string.IsNullOrWhiteSpace(passwordTextBox.Text))
        {
            MessageBox.Show(this, "Password wajib diisi untuk akun baru.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var actionText = editingId is null ? "Tambah akun Jukir baru?" : "Simpan perubahan akun?";
        if (MessageBox.Show(this, actionText, "Konfirmasi", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        await UiOperations.RunAsync(this, addButton, ShellCancellation, async () =>
        {
            await service.SaveJukirAsync(session, editingId, jukirnameTextBox.Text, usernameTextBox.Text, string.IsNullOrEmpty(passwordTextBox.Text) ? null : passwordTextBox.Text, spot, ShellCancellation);
            await RefreshAsync(ShellCancellation);
            ClearEditor();
        });
    }
    private async Task DeactivateAsync()
    {
        if (service is null || session is null || grid5.CurrentRow?.DataBoundItem is not JukirAccount a) return;
        if (MessageBox.Show(this, $"Nonaktifkan akun {a.Name}?", "Konfirmasi", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        await UiOperations.RunAsync(this, deleteButton, ShellCancellation, async () =>
        {
            await service.DeactivateJukirAsync(session, a.Id, ShellCancellation);
            await RefreshAsync(ShellCancellation);
            ClearEditor();
        });
    }
}
