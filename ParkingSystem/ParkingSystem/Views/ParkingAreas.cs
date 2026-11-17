using ParkingSystem.Database;

namespace ParkingSystem;

/// <summary>
/// Tampilan modul manajemen master data area parkir (Manage Parking Spot Data).
/// Sesuai diagram Draw.io:
/// - 1 - Use Case: Manage Parking Spot Data
/// - 4 - Wireframe: Form input (nama area, kapasitas, tarif motor, tarif mobil),
///   tabel daftar area, tombol Add, Edit, Delete (toggle status), dan Save.
/// </summary>
public partial class ParkingAreas : UserControl
{
    private ParkingSpot? _selectedSpot;

    /// <summary>
    /// Inisialisasi antarmuka kelola area parkir dan mengaitkan event handler tombol.
    /// </summary>
    public ParkingAreas()
    {
        InitializeComponent();
        ResponsiveLayout.Attach(this);

        addButton.Click += AddButton_Click;
        editButton.Click += EditButton_Click;
        deleteButton.Click += DeleteButton_Click;
        saveButton.Click += SaveButton_Click;
        searchTextBox.TextChanged += (s, e) => LoadAreas();

        LoadAreas();
    }

    /// <summary>
    /// Memuat daftar titik area parkir dari InMemoryDb/EF Core ke DataGridView
    /// beserta pencarian filter kata kunci nama atau alamat.
    /// </summary>
    private void LoadAreas()
    {
        grid5.Rows.Clear();
        string kw = searchTextBox.Text.Trim();
        var list = InMemoryDb.Spots.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(kw))
            list = list.Where(s => s.Name.Contains(kw, StringComparison.OrdinalIgnoreCase) || s.Address.Contains(kw, StringComparison.OrdinalIgnoreCase));

        foreach (var s in list)
        {
            int idx = grid5.Rows.Add(
                s.Name,
                s.Capacity.ToString(),
                $"Rp {s.MotorcycleRate:N0}",
                $"Rp {s.CarRate:N0}",
                s.IsActive ? "ACTIVE" : "INACTIVE"
            );
            grid5.Rows[idx].Tag = s;
        }
    }

    /// <summary>
    /// Event handler tombol "Add" / Form Baru untuk mereset form ke mode tambah data baru.
    /// </summary>
    private void AddButton_Click(object? sender, EventArgs e)
    {
        _selectedSpot = null;
        areanameTextBox.Clear();
        capacityNumericUpDown.Value = 50;
        motorcycleRateNumericUpDown.Value = 2000;
        carRateNumericUpDown.Value = 5000;
        areanameTextBox.Focus();
    }

    /// <summary>
    /// Event handler tombol "Edit" untuk memuat data area yang dipilih di tabel ke field input form.
    /// </summary>
    private void EditButton_Click(object? sender, EventArgs e)
    {
        if (grid5.CurrentRow?.Tag is not ParkingSpot spot)
        {
            MessageBox.Show("Pilih area parkir dari tabel!", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _selectedSpot = spot;
        areanameTextBox.Text = spot.Name;
        capacityNumericUpDown.Value = spot.Capacity;
        motorcycleRateNumericUpDown.Value = spot.MotorcycleRate;
        carRateNumericUpDown.Value = spot.CarRate;
    }

    /// <summary>
    /// Event handler tombol "Delete" untuk mengubah status operasional area parkir (ACTIVE &lt;-&gt; INACTIVE).
    /// </summary>
    private void DeleteButton_Click(object? sender, EventArgs e)
    {
        if (grid5.CurrentRow?.Tag is not ParkingSpot spot)
        {
            MessageBox.Show("Pilih area parkir dari tabel!", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        spot.IsActive = !spot.IsActive;
        InMemoryDb.SaveChanges();
        MessageBox.Show($"Status area parkir '{spot.Name}' diubah menjadi: {(spot.IsActive ? "ACTIVE" : "INACTIVE")}",
            "Status Berubah", MessageBoxButtons.OK, MessageBoxIcon.Information);
        LoadAreas();
    }

    /// <summary>
    /// Event handler tombol "Save" untuk menyimpan data area parkir baru atau memperbarui data yang diedit.
    /// </summary>
    private void SaveButton_Click(object? sender, EventArgs e)
    {
        string name = areanameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Nama area parkir tidak boleh kosong!", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        int cap = (int)capacityNumericUpDown.Value;
        decimal mRate = motorcycleRateNumericUpDown.Value;
        decimal cRate = carRateNumericUpDown.Value;

        if (_selectedSpot == null)
        {
            // Mode Tambah Baru
            var newSpot = new ParkingSpot
            {
                Id = InMemoryDb.NextSpotId(),
                Name = name,
                Address = "Lokasi default",
                Capacity = cap,
                MotorcycleRate = mRate,
                CarRate = cRate,
                IsActive = true
            };
            InMemoryDb.AddSpot(newSpot);
            MessageBox.Show($"Area parkir baru '{newSpot.Name}' berhasil ditambahkan!", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            // Mode Edit Perubahan
            _selectedSpot.Name = name;
            _selectedSpot.Capacity = cap;
            _selectedSpot.MotorcycleRate = mRate;
            _selectedSpot.CarRate = cRate;
            InMemoryDb.SaveChanges();
            MessageBox.Show($"Data area parkir '{_selectedSpot.Name}' berhasil diperbarui!", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        AddButton_Click(null, EventArgs.Empty);
        LoadAreas();
    }
}
