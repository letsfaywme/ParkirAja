namespace ParkingSystem;

public partial class VehicleEntry : UserControl
{
    public VehicleEntry()
    {
        InitializeComponent();
        ResponsiveLayout.Attach(this);

        // Inisialisasi dropdown jenis kendaraan
        vehicletypeComboBox.Items.Clear();
        vehicletypeComboBox.DataSource = Enum.GetValues<VehicleType>();

        // Event handler tombol aksi
        saveEntryButton.Click += SaveEntryButton_Click;
        clearButton.Click += ClearButton_Click;
    }

    private void SaveEntryButton_Click(object? sender, EventArgs e)
    {
        // TODO: Implementasikan logika penyimpanan kendaraan masuk secara manual
        string plateNumber = platenumberTextBox.Text.Trim();
        var vehicleType = vehicletypeComboBox.SelectedItem;

        MessageBox.Show($"TODO: Simpan data kendaraan masuk ({plateNumber} - {vehicleType})", "Vehicle Entry", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ClearButton_Click(object? sender, EventArgs e)
    {
        // TODO: Reset input form
        platenumberTextBox.Clear();
        platenumberTextBox.Focus();
    }
}
