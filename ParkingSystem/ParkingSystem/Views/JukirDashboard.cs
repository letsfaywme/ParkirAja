namespace ParkingSystem;

public partial class JukirDashboard : UserControl
{
    public JukirDashboard()
    {
        InitializeComponent();
        ResponsiveLayout.Attach(this);

        refreshButton.Click += RefreshButton_Click;
    }

    private void RefreshButton_Click(object? sender, EventArgs e)
    {
        // TODO: Muat ulang data ringkasan harian jukir (jumlah masuk, keluar, pendapatan, transaksi terkini)
    }
}
