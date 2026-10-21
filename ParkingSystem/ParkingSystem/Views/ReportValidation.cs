namespace ParkingSystem;

public partial class ReportValidation : UserControl
{
    public ReportValidation()
    {
        InitializeComponent();
        ResponsiveLayout.Attach(this);

        searchButton.Click += SearchButton_Click;
        approveButton.Click += ApproveButton_Click;
        rejectButton.Click += RejectButton_Click;
    }

    private void SearchButton_Click(object? sender, EventArgs e)
    {
        // TODO: Implementasikan pencarian laporan setoran jukir berdasarkan status dan tanggal
    }

    private void ApproveButton_Click(object? sender, EventArgs e)
    {
        // TODO: Implementasikan persetujuan (approve) setoran terpilih
    }

    private void RejectButton_Click(object? sender, EventArgs e)
    {
        // TODO: Implementasikan penolakan (reject) setoran terpilih disertai catatan review
    }
}
