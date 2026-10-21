namespace ParkingSystem;

public partial class JukirAccounts : UserControl
{
    public JukirAccounts()
    {
        InitializeComponent();
        ResponsiveLayout.Attach(this);

        addButton.Click += AddButton_Click;
        editButton.Click += EditButton_Click;
        deleteButton.Click += DeleteButton_Click;
    }

    private void AddButton_Click(object? sender, EventArgs e)
    {
        // TODO: Tambahkan akun petugas parkir (Jukir) baru
    }

    private void EditButton_Click(object? sender, EventArgs e)
    {
        // TODO: Muat data akun jukir terpilih ke form untuk diedit
    }

    private void DeleteButton_Click(object? sender, EventArgs e)
    {
        // TODO: Nonaktifkan atau hapus akun jukir terpilih
    }
}
