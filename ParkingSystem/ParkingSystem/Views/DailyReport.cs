namespace ParkingSystem;

public partial class DailyReport : UserControl
{
    public DailyReport()
    {
        InitializeComponent();
        ResponsiveLayout.Attach(this);

        showReportButton.Click += ShowReportButton_Click;
        submitforValidationButton.Click += SubmitforValidationButton_Click;
        exportReportButton.Click += ExportReportButton_Click;
    }

    private void ShowReportButton_Click(object? sender, EventArgs e)
    {
        // TODO: Implementasikan pengambilan dan penampilan laporan harian jukir berdasarkan tanggal
    }

    private void SubmitforValidationButton_Click(object? sender, EventArgs e)
    {
        // TODO: Implementasikan pengajuan setoran harian untuk divalidasi oleh admin
    }

    private void ExportReportButton_Click(object? sender, EventArgs e)
    {
        // TODO: Implementasikan ekspor data laporan harian
    }
}
