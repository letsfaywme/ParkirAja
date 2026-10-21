namespace ParkingSystem;

public partial class ParkingAreas : UserControl
{
    public ParkingAreas()
    {
        InitializeComponent();
        ResponsiveLayout.Attach(this);

        addButton.Click += AddButton_Click;
        editButton.Click += EditButton_Click;
        deleteButton.Click += DeleteButton_Click;
        saveButton.Click += SaveButton_Click;
    }

    private void AddButton_Click(object? sender, EventArgs e)
    {
        // TODO: Siapkan form input untuk menambahkan area parkir baru
    }

    private void EditButton_Click(object? sender, EventArgs e)
    {
        // TODO: Muat data area parkir yang dipilih ke form input untuk diedit
    }

    private void DeleteButton_Click(object? sender, EventArgs e)
    {
        // TODO: Nonaktifkan atau hapus area parkir yang dipilih
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        // TODO: Simpan perubahan atau data baru area parkir
    }
}
