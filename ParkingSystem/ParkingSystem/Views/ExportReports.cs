namespace ParkingSystem;

public partial class ExportReports : UserControl
{
    public ExportReports()
    {
        InitializeComponent();
        ResponsiveLayout.Attach(this);

        generatePreviewButton.Click += GeneratePreviewButton_Click;
        exportFileButton.Click += ExportFileButton_Click;
    }

    private void GeneratePreviewButton_Click(object? sender, EventArgs e)
    {
        // TODO: Buat preview tabel laporan berdasarkan tipe, periode, dan format
    }

    private void ExportFileButton_Click(object? sender, EventArgs e)
    {
        // TODO: Simpan dan ekspor laporan ke file
    }
}
