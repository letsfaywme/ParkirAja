namespace ParkingSystem;

public partial class AdminDashboard : UserControl
{
    public AdminDashboard()
    {
        InitializeComponent();
        ResponsiveLayout.Attach(this);

        refreshButton.Click += RefreshButton_Click;
    }

    private void RefreshButton_Click(object? sender, EventArgs e)
    {
        // TODO: Muat ulang data statistik & ringkasan kapasitas area parkir untuk admin
    }
}
