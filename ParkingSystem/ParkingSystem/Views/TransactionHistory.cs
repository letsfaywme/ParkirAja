namespace ParkingSystem;

public partial class TransactionHistory : UserControl
{
    public TransactionHistory()
    {
        InitializeComponent();
        ResponsiveLayout.Attach(this);

        // Inisialisasi filter jenis kendaraan
        vehicletypeComboBox.Items.Clear();
        vehicletypeComboBox.Items.Add("All Types");
        foreach (var type in Enum.GetValues<VehicleType>())
            vehicletypeComboBox.Items.Add(type);
        vehicletypeComboBox.SelectedIndex = 0;

        // Event handler tombol aksi
        searchButton.Click += SearchButton_Click;
        previousButton.Click += PreviousButton_Click;
        nextButton.Click += NextButton_Click;
        exportButton.Click += ExportButton_Click;
    }

    private void SearchButton_Click(object? sender, EventArgs e)
    {
        // TODO: Implementasikan pencarian riwayat transaksi berdasarkan filter tanggal, jenis kendaraan, dan plat
    }

    private void PreviousButton_Click(object? sender, EventArgs e)
    {
        // TODO: Implementasikan navigasi halaman riwayat sebelumnya (pagination)
    }

    private void NextButton_Click(object? sender, EventArgs e)
    {
        // TODO: Implementasikan navigasi halaman riwayat berikutnya (pagination)
    }

    private void ExportButton_Click(object? sender, EventArgs e)
    {
        // TODO: Implementasikan ekspor data riwayat transaksi
    }
}
