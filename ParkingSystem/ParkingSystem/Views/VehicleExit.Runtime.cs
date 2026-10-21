namespace ParkingSystem;

public partial class VehicleExit
{
    private UserSession? session;
    private ParkingService? service;
    private ParkingTransaction? activeTransaction;
    private bool bound;
    private bool processing;
    private int lookupVersion;
    internal CancellationToken ShellCancellation;

    public void Bind(UserSession session, ParkingService service)
    {
        this.session = session;
        this.service = service;
        if (bound) return;
        bound = true;
        label6.Text = "Plate number";
        plateTextBox.PlaceholderText = "Masukkan plat kendaraan";
        ClearDetails();
        plateTextBox.TextChanged += (_, _) =>
        {
            lookupVersion++;
            ClearDetails();
        };
        plateTextBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                FindVehicle(findButton, EventArgs.Empty);
            }
        };
        findButton.Click += FindVehicle;
        processVehicleExitButton.Click += ProcessExit;
    }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        if (service is null || session is null) return;
        lookupVersion++;
        ClearDetails();
        await service.ValidateSessionAsync(session, cancellationToken);
    }

    private void ClearDetails()
    {
        activeTransaction = null;
        foreach (var field in new[] { platenumberTextBox, vehicletypeTextBox, parkingareaTextBox,
                     entrytimeTextBox, exittimeTextBox, durationTextBox })
        {
            field.ReadOnly = true;
            field.Clear();
        }
        label41.Text = "—";
        label43.Text = "Tarif tetap sekali parkir";
        label46.Text = "Cari kendaraan aktif untuk memproses pembayaran.";
        processVehicleExitButton.Enabled = false;
    }

    private void ShowDetails(ParkingTransaction transaction)
    {
        platenumberTextBox.Text = transaction.PlateNumber;
        vehicletypeTextBox.Text = transaction.VehicleType.ToString();
        parkingareaTextBox.Text = transaction.LocationName;
        entrytimeTextBox.Text = ParkingTime.Wib(transaction.EntryTime).ToString("dd MMM yyyy HH:mm:ss");
        var end = transaction.ExitTime ?? DateTimeOffset.UtcNow;
        exittimeTextBox.Text = transaction.ExitTime is null ? "Belum keluar" : ParkingTime.Wib(end).ToString("dd MMM yyyy HH:mm:ss");
        var duration = end - transaction.EntryTime;
        if (duration < TimeSpan.Zero) duration = TimeSpan.Zero;
        durationTextBox.Text = $"{(long)duration.TotalHours} jam {duration.Minutes} menit";
        label41.Text = ParkingTime.Money(transaction.Fare ?? transaction.RateAtEntry);
        label43.Text = $"Tarif tetap · {transaction.VehicleType}";
    }

    private async void FindVehicle(object? sender, EventArgs e)
    {
        if (processing || service is null || session is null) return;
        var version = ++lookupVersion;
        var plate = plateTextBox.Text.Trim();
        ClearDetails();
        if (string.IsNullOrWhiteSpace(plate))
        {
            MessageBox.Show(this, "Masukkan nomor plat kendaraan yang ingin dicari.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        await UiOperations.RunAsync(this, findButton, ShellCancellation, async () =>
        {
            var active = await service.FindActiveVehicleAsync(session, plate, ShellCancellation);
            ShellCancellation.ThrowIfCancellationRequested();
            if (version != lookupVersion || IsDisposed) return;
            if (active is not null)
            {
                activeTransaction = active;
                ShowDetails(active);
                label46.Text = "Kendaraan aktif ditemukan. Konfirmasi pembayaran sebelum keluar.";
                processVehicleExitButton.Enabled = true;
                return;
            }

            // Not active: check if this vehicle has already completed exit
            var latest = await service.FindLatestVehicleAsync(session, plate, ShellCancellation);
            ShellCancellation.ThrowIfCancellationRequested();
            if (version != lookupVersion || IsDisposed) return;

            if (latest is not null && latest.Status == "completed")
            {
                ShowDetails(latest);
                label41.Text = ParkingTime.Money(latest.Fare ?? latest.RateAtEntry);
                label43.Text = $"Sudah keluar · {latest.VehicleType}";
                label46.Text = $"Kendaraan sudah keluar pada {ParkingTime.Wib(latest.ExitTime!.Value):dd MMM yyyy HH:mm:ss} WIB.";
                processVehicleExitButton.Enabled = false;
                MessageBox.Show(this,
                    $"Kendaraan dengan plat {latest.PlateNumber} SUDAH KELUAR.\n\n" +
                    $"Waktu masuk : {ParkingTime.Wib(latest.EntryTime):dd MMM yyyy HH:mm:ss} WIB\n" +
                    $"Waktu keluar: {ParkingTime.Wib(latest.ExitTime!.Value):dd MMM yyyy HH:mm:ss} WIB\n" +
                    $"Total bayar : {ParkingTime.Money(latest.Fare ?? latest.RateAtEntry)}\n" +
                    $"Petugas     : {latest.ExitJukirName ?? latest.JukirName}",
                    "Kendaraan Sudah Keluar", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            label46.Text = "Kendaraan tidak ditemukan di lokasi penugasan ini.";
            MessageBox.Show(this, $"Kendaraan dengan plat {plate.ToUpperInvariant()} tidak ditemukan di lokasi ini.",
                "Tidak Ditemukan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        });
    }

    private async void ProcessExit(object? sender, EventArgs e)
    {
        if (processing || service is null || session is null || activeTransaction is not { } transaction) return;
        if (MessageBox.Show(this, $"Pembayaran sebesar {ParkingTime.Money(transaction.RateAtEntry)} sudah diterima?",
            "Konfirmasi pembayaran", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        if (activeTransaction?.Id != transaction.Id || ShellCancellation.IsCancellationRequested) return;
        processing = true;
        lookupVersion++;
        plateTextBox.Enabled = false;
        findButton.Enabled = false;
        try
        {
            await UiOperations.RunAsync(this, processVehicleExitButton, ShellCancellation, async () =>
            {
                var completed = await service.CompleteExitAsync(session, transaction.Id, ShellCancellation);
                activeTransaction = null;
                if (IsDisposed || ShellCancellation.IsCancellationRequested) return;
                ShowDetails(completed);
                label46.Text = $"Transaksi #{completed.Id} selesai. Pembayaran tersimpan.";
                MessageBox.Show(this, "Pembayaran tersimpan; kendaraan keluar.", "Vehicle Exit",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
        }
        finally
        {
            processing = false;
            if (!IsDisposed)
            {
                plateTextBox.Enabled = true;
                findButton.Enabled = true;
                // Failed/stale completion requires fresh lookup, never reuse stale payment details.
                if (activeTransaction is not null) ClearDetails();
                processVehicleExitButton.Enabled = false;
            }
        }
    }
}
