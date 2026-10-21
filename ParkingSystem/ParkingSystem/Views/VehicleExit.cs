namespace ParkingSystem;

public partial class VehicleExit : UserControl
{
    public VehicleExit()
    {
        InitializeComponent();
        ResponsiveLayout.Attach(this);

        findButton.Click += FindButton_Click;
        processVehicleExitButton.Click += ProcessVehicleExitButton_Click;
    }

    private void FindButton_Click(object? sender, EventArgs e)
    {
        // TODO: Implementasikan pencarian kendaraan berdasarkan plat nomor
        string plateNumber = plateTextBox.Text.Trim();
        MessageBox.Show($"TODO: Cari data kendaraan dengan plat '{plateNumber}'", "Vehicle Exit", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ProcessVehicleExitButton_Click(object? sender, EventArgs e)
    {
        // TODO: Implementasikan proses transaksi kendaraan keluar & pembayaran
        MessageBox.Show("TODO: Proses kendaraan keluar dan cetak struk pembayaran", "Vehicle Exit", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
