#nullable enable annotations
namespace ParkingSystem;

partial class VehicleEntry
{
    private System.ComponentModel.IContainer? components = null;
    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }
    #region Windows Form Designer generated code
    private void InitializeComponent()
    {
        DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
        label1 = new Label();
        registervehiclePanel = new Panel();
        proofPanel = new Panel();
        proofLabel = new Label();
        labelProof = new Label();
        ratePanel = new Panel();
        rateLabel = new Label();
        rateTitleLabel = new Label();
        clearButton = new Button();
        saveEntryButton = new Button();
        parkingareaPanel = new Panel();
        parkingareaComboBox = new ComboBox();
        label14 = new Label();
        vehicletypePanel = new Panel();
        vehicletypeComboBox = new ComboBox();
        label10 = new Label();
        platenumberPanel = new Panel();
        platenumberTextBox = new TextBox();
        label6 = new Label();
        label4 = new Label();
        editEntryButton = new Button();
        deleteEntryButton = new Button();
        latestentriesPanel = new Panel();
        grid22 = new DataGridView();
        column24 = new DataGridViewTextBoxColumn();
        column25 = new DataGridViewTextBoxColumn();
        column26 = new DataGridViewTextBoxColumn();
        column27 = new DataGridViewTextBoxColumn();
        column28 = new DataGridViewTextBoxColumn();
        label21 = new Label();
        responsiveLayout = new Panel();
        registervehiclePanel.SuspendLayout();
        proofPanel.SuspendLayout();
        ratePanel.SuspendLayout();
        parkingareaPanel.SuspendLayout();
        vehicletypePanel.SuspendLayout();
        platenumberPanel.SuspendLayout();
        latestentriesPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)grid22).BeginInit();
        SuspendLayout();
        // 
        // label1
        // 
        label1.BackColor = Color.Transparent;
        label1.Dock = DockStyle.Top;
        label1.Font = new Font("Segoe UI", 12.3F, FontStyle.Bold);
        label1.ForeColor = Color.FromArgb(15, 23, 42);
        label1.Location = new Point(0, 35);
        label1.Margin = new Padding(0, 0, 0, 36);
        label1.Name = "label1";
        label1.Size = new Size(1312, 25);
        label1.TabIndex = 1;
        label1.Text = "Vehicle Entry";
        // 
        // registervehiclePanel
        // 
        registervehiclePanel.BackColor = Color.FromArgb(255, 255, 255);
        registervehiclePanel.BorderStyle = BorderStyle.FixedSingle;
        registervehiclePanel.Controls.Add(proofPanel);
        registervehiclePanel.Controls.Add(labelProof);
        registervehiclePanel.Controls.Add(ratePanel);
        registervehiclePanel.Controls.Add(rateTitleLabel);
        registervehiclePanel.Controls.Add(clearButton);
        registervehiclePanel.Controls.Add(saveEntryButton);
        registervehiclePanel.Controls.Add(parkingareaPanel);
        registervehiclePanel.Controls.Add(label14);
        registervehiclePanel.Controls.Add(vehicletypePanel);
        registervehiclePanel.Controls.Add(label10);
        registervehiclePanel.Controls.Add(platenumberPanel);
        registervehiclePanel.Controls.Add(label6);
        registervehiclePanel.Controls.Add(label4);
        registervehiclePanel.Dock = DockStyle.Top;
        registervehiclePanel.ForeColor = Color.FromArgb(15, 23, 42);
        registervehiclePanel.Location = new Point(0, 60);
        registervehiclePanel.Margin = new Padding(4, 4, 4, 4);
        registervehiclePanel.Name = "registervehiclePanel";
        registervehiclePanel.Size = new Size(1312, 281);
        registervehiclePanel.TabIndex = 2;
        // 
        // proofPanel
        // 
        proofPanel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        proofPanel.BackColor = Color.FromArgb(255, 255, 255);
        proofPanel.BorderStyle = BorderStyle.FixedSingle;
        proofPanel.Controls.Add(proofLabel);
        proofPanel.ForeColor = Color.FromArgb(15, 23, 42);
        proofPanel.Location = new Point(812, 189);
        proofPanel.Margin = new Padding(4, 4, 4, 4);
        proofPanel.Name = "proofPanel";
        proofPanel.Size = new Size(174, 44);
        proofPanel.TabIndex = 0;
        // 
        // proofLabel
        // 
        proofLabel.Cursor = Cursors.Hand;
        proofLabel.Dock = DockStyle.Fill;
        proofLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        proofLabel.ForeColor = Color.FromArgb(100, 116, 139);
        proofLabel.Location = new Point(0, 0);
        proofLabel.Margin = new Padding(4, 0, 4, 0);
        proofLabel.Name = "proofLabel";
        proofLabel.Size = new Size(172, 42);
        proofLabel.TabIndex = 0;
        proofLabel.Text = "Belum ada";
        proofLabel.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // labelProof
        // 
        labelProof.BackColor = Color.Transparent;
        labelProof.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        labelProof.ForeColor = Color.FromArgb(100, 116, 139);
        labelProof.Location = new Point(748, 162);
        labelProof.Margin = new Padding(4, 0, 4, 0);
        labelProof.Name = "labelProof";
        labelProof.Size = new Size(175, 15);
        labelProof.TabIndex = 1;
        labelProof.Text = "Bukti tiket";
        // 
        // ratePanel
        // 
        ratePanel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        ratePanel.BackColor = Color.FromArgb(255, 255, 255);
        ratePanel.BorderStyle = BorderStyle.FixedSingle;
        ratePanel.Controls.Add(rateLabel);
        ratePanel.ForeColor = Color.FromArgb(15, 23, 42);
        ratePanel.Location = new Point(671, 189);
        ratePanel.Margin = new Padding(4, 4, 4, 4);
        ratePanel.Name = "ratePanel";
        ratePanel.Size = new Size(124, 44);
        ratePanel.TabIndex = 2;
        // 
        // rateLabel
        // 
        rateLabel.Dock = DockStyle.Fill;
        rateLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        rateLabel.ForeColor = Color.FromArgb(5, 150, 105);
        rateLabel.Location = new Point(0, 0);
        rateLabel.Margin = new Padding(4, 0, 4, 0);
        rateLabel.Name = "rateLabel";
        rateLabel.Size = new Size(122, 42);
        rateLabel.TabIndex = 0;
        rateLabel.Text = "Rp 0";
        rateLabel.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // rateTitleLabel
        // 
        rateTitleLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        rateTitleLabel.BackColor = Color.Transparent;
        rateTitleLabel.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        rateTitleLabel.ForeColor = Color.FromArgb(100, 116, 139);
        rateTitleLabel.Location = new Point(671, 162);
        rateTitleLabel.Margin = new Padding(4, 0, 4, 0);
        rateTitleLabel.Name = "rateTitleLabel";
        rateTitleLabel.Size = new Size(119, 15);
        rateTitleLabel.TabIndex = 3;
        rateTitleLabel.Text = "Tarif berlaku";
        // 
        // clearButton
        // 
        clearButton.AccessibleName = "Clear";
        clearButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        clearButton.BackColor = Color.FromArgb(241, 245, 249);
        clearButton.Cursor = Cursors.Hand;
        clearButton.FlatAppearance.BorderSize = 0;
        clearButton.FlatStyle = FlatStyle.Flat;
        clearButton.ForeColor = Color.FromArgb(15, 23, 42);
        clearButton.Location = new Point(1146, 189);
        clearButton.Margin = new Padding(4, 4, 4, 4);
        clearButton.Name = "clearButton";
        clearButton.Size = new Size(125, 45);
        clearButton.TabIndex = 18;
        clearButton.Text = "Clear";
        clearButton.UseVisualStyleBackColor = false;
        // 
        // saveEntryButton
        // 
        saveEntryButton.AccessibleName = "Save Entry";
        saveEntryButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        saveEntryButton.BackColor = Color.FromArgb(5, 150, 105);
        saveEntryButton.Cursor = Cursors.Hand;
        saveEntryButton.FlatAppearance.BorderSize = 0;
        saveEntryButton.FlatStyle = FlatStyle.Flat;
        saveEntryButton.ForeColor = Color.White;
        saveEntryButton.Location = new Point(1002, 189);
        saveEntryButton.Margin = new Padding(4, 4, 4, 4);
        saveEntryButton.Name = "saveEntryButton";
        saveEntryButton.Size = new Size(131, 45);
        saveEntryButton.TabIndex = 17;
        saveEntryButton.Text = "Save Entry";
        saveEntryButton.UseVisualStyleBackColor = false;
        // 
        // parkingareaPanel
        // 
        parkingareaPanel.BackColor = Color.FromArgb(255, 255, 255);
        parkingareaPanel.BorderStyle = BorderStyle.FixedSingle;
        parkingareaPanel.Controls.Add(parkingareaComboBox);
        parkingareaPanel.ForeColor = Color.FromArgb(15, 23, 42);
        parkingareaPanel.Location = new Point(62, 189);
        parkingareaPanel.Margin = new Padding(4, 4, 4, 4);
        parkingareaPanel.Name = "parkingareaPanel";
        parkingareaPanel.Size = new Size(461, 47);
        parkingareaPanel.TabIndex = 15;
        // 
        // parkingareaComboBox
        // 
        parkingareaComboBox.AccessibleName = "Parking area";
        parkingareaComboBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        parkingareaComboBox.BackColor = Color.FromArgb(255, 255, 255);
        parkingareaComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        parkingareaComboBox.FlatStyle = FlatStyle.Flat;
        parkingareaComboBox.ForeColor = Color.FromArgb(15, 23, 42);
        parkingareaComboBox.Items.AddRange(new object[] { "Area A", "Area B", "Area C" });
        parkingareaComboBox.Location = new Point(10, 9);
        parkingareaComboBox.Margin = new Padding(4, 4, 4, 4);
        parkingareaComboBox.Name = "parkingareaComboBox";
        parkingareaComboBox.Size = new Size(440, 28);
        parkingareaComboBox.TabIndex = 16;
        // 
        // label14
        // 
        label14.BackColor = Color.Transparent;
        label14.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        label14.ForeColor = Color.FromArgb(100, 116, 139);
        label14.Location = new Point(25, 162);
        label14.Margin = new Padding(4, 0, 4, 0);
        label14.Name = "label14";
        label14.Size = new Size(461, 15);
        label14.TabIndex = 14;
        label14.Text = "Parking area";
        // 
        // vehicletypePanel
        // 
        vehicletypePanel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        vehicletypePanel.BackColor = Color.FromArgb(255, 255, 255);
        vehicletypePanel.BorderStyle = BorderStyle.FixedSingle;
        vehicletypePanel.Controls.Add(vehicletypeComboBox);
        vehicletypePanel.ForeColor = Color.FromArgb(15, 23, 42);
        vehicletypePanel.Location = new Point(671, 94);
        vehicletypePanel.Margin = new Padding(4, 4, 4, 4);
        vehicletypePanel.Name = "vehicletypePanel";
        vehicletypePanel.Size = new Size(477, 47);
        vehicletypePanel.TabIndex = 11;
        // 
        // vehicletypeComboBox
        // 
        vehicletypeComboBox.AccessibleName = "Vehicle type";
        vehicletypeComboBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        vehicletypeComboBox.BackColor = Color.FromArgb(255, 255, 255);
        vehicletypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        vehicletypeComboBox.FlatStyle = FlatStyle.Flat;
        vehicletypeComboBox.ForeColor = Color.FromArgb(15, 23, 42);
        vehicletypeComboBox.Items.AddRange(new object[] { "Motorcycle", "Car" });
        vehicletypeComboBox.Location = new Point(10, 9);
        vehicletypeComboBox.Margin = new Padding(4, 4, 4, 4);
        vehicletypeComboBox.Name = "vehicletypeComboBox";
        vehicletypeComboBox.Size = new Size(454, 28);
        vehicletypeComboBox.TabIndex = 12;
        // 
        // label10
        // 
        label10.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        label10.BackColor = Color.Transparent;
        label10.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        label10.ForeColor = Color.FromArgb(100, 116, 139);
        label10.Location = new Point(671, 68);
        label10.Margin = new Padding(4, 0, 4, 0);
        label10.Name = "label10";
        label10.Size = new Size(450, 15);
        label10.TabIndex = 10;
        label10.Text = "Vehicle type";
        // 
        // platenumberPanel
        // 
        platenumberPanel.BackColor = Color.FromArgb(255, 255, 255);
        platenumberPanel.BorderStyle = BorderStyle.FixedSingle;
        platenumberPanel.Controls.Add(platenumberTextBox);
        platenumberPanel.ForeColor = Color.FromArgb(15, 23, 42);
        platenumberPanel.Location = new Point(62, 94);
        platenumberPanel.Margin = new Padding(4, 4, 4, 4);
        platenumberPanel.Name = "platenumberPanel";
        platenumberPanel.Size = new Size(461, 47);
        platenumberPanel.TabIndex = 7;
        // 
        // platenumberTextBox
        // 
        platenumberTextBox.AccessibleName = "Plate number";
        platenumberTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        platenumberTextBox.BackColor = Color.FromArgb(255, 255, 255);
        platenumberTextBox.BorderStyle = BorderStyle.None;
        platenumberTextBox.ForeColor = Color.FromArgb(15, 23, 42);
        platenumberTextBox.Location = new Point(10, 9);
        platenumberTextBox.Margin = new Padding(4, 4, 4, 4);
        platenumberTextBox.Name = "platenumberTextBox";
        platenumberTextBox.PlaceholderText = "B 2841 SJK";
        platenumberTextBox.Size = new Size(441, 20);
        platenumberTextBox.TabIndex = 8;
        // 
        // label6
        // 
        label6.BackColor = Color.Transparent;
        label6.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        label6.ForeColor = Color.FromArgb(100, 116, 139);
        label6.Location = new Point(25, 68);
        label6.Margin = new Padding(4, 0, 4, 0);
        label6.Name = "label6";
        label6.Size = new Size(461, 15);
        label6.TabIndex = 6;
        label6.Text = "Plate number";
        // 
        // label4
        // 
        label4.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        label4.BackColor = Color.Transparent;
        label4.Font = new Font("Segoe UI", 9.22F, FontStyle.Bold);
        label4.ForeColor = Color.FromArgb(15, 23, 42);
        label4.Location = new Point(22, 20);
        label4.Margin = new Padding(4, 0, 4, 0);
        label4.Name = "label4";
        label4.Size = new Size(1066, 19);
        label4.TabIndex = 4;
        label4.Text = "Register vehicle";
        // 
        // editEntryButton
        // 
        editEntryButton.AccessibleName = "Edit";
        editEntryButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        editEntryButton.BackColor = Color.FromArgb(241, 245, 249);
        editEntryButton.Cursor = Cursors.Hand;
        editEntryButton.FlatAppearance.BorderSize = 0;
        editEntryButton.FlatStyle = FlatStyle.Flat;
        editEntryButton.ForeColor = Color.FromArgb(15, 23, 42);
        editEntryButton.Location = new Point(1075, 12);
        editEntryButton.Margin = new Padding(4, 4, 4, 4);
        editEntryButton.Name = "editEntryButton";
        editEntryButton.Size = new Size(112, 38);
        editEntryButton.TabIndex = 0;
        editEntryButton.Text = "Edit";
        editEntryButton.UseVisualStyleBackColor = false;
        // 
        // deleteEntryButton
        // 
        deleteEntryButton.AccessibleName = "Batalkan";
        deleteEntryButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        deleteEntryButton.BackColor = Color.FromArgb(241, 245, 249);
        deleteEntryButton.Cursor = Cursors.Hand;
        deleteEntryButton.FlatAppearance.BorderSize = 0;
        deleteEntryButton.FlatStyle = FlatStyle.Flat;
        deleteEntryButton.ForeColor = Color.FromArgb(15, 23, 42);
        deleteEntryButton.Location = new Point(1193, 12);
        deleteEntryButton.Margin = new Padding(4, 4, 4, 4);
        deleteEntryButton.Name = "deleteEntryButton";
        deleteEntryButton.Size = new Size(112, 38);
        deleteEntryButton.TabIndex = 1;
        deleteEntryButton.Text = "Batalkan";
        deleteEntryButton.UseVisualStyleBackColor = false;
        // 
        // latestentriesPanel
        // 
        latestentriesPanel.BackColor = Color.FromArgb(255, 255, 255);
        latestentriesPanel.BorderStyle = BorderStyle.FixedSingle;
        latestentriesPanel.Controls.Add(editEntryButton);
        latestentriesPanel.Controls.Add(deleteEntryButton);
        latestentriesPanel.Controls.Add(grid22);
        latestentriesPanel.Controls.Add(label21);
        latestentriesPanel.Dock = DockStyle.Top;
        latestentriesPanel.ForeColor = Color.FromArgb(15, 23, 42);
        latestentriesPanel.Location = new Point(0, 341);
        latestentriesPanel.Margin = new Padding(4, 4, 4, 4);
        latestentriesPanel.Name = "latestentriesPanel";
        latestentriesPanel.Size = new Size(1312, 348);
        latestentriesPanel.TabIndex = 19;
        // 
        // grid22
        // 
        grid22.AllowUserToAddRows = false;
        grid22.AllowUserToDeleteRows = false;
        grid22.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        grid22.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid22.BackgroundColor = Color.White;
        grid22.BorderStyle = BorderStyle.None;
        grid22.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
        grid22.ColumnHeadersHeight = 29;
        grid22.Columns.AddRange(new DataGridViewColumn[] { column24, column25, column26, column27, column28 });
        grid22.EnableHeadersVisualStyles = false;
        grid22.GridColor = Color.FromArgb(226, 232, 240);
        grid22.Location = new Point(14, 61);
        grid22.Margin = new Padding(4, 4, 4, 4);
        grid22.Name = "grid22";
        grid22.ReadOnly = true;
        grid22.RowHeadersVisible = false;
        grid22.RowHeadersWidth = 51;
        grid22.Size = new Size(1281, 269);
        grid22.TabIndex = 22;
        // 
        // column24
        // 
        column24.HeaderText = "Entry time";
        column24.MinimumWidth = 6;
        column24.Name = "column24";
        column24.ReadOnly = true;
        // 
        // column25
        // 
        column25.HeaderText = "Plate number";
        column25.MinimumWidth = 6;
        column25.Name = "column25";
        column25.ReadOnly = true;
        // 
        // column26
        // 
        column26.HeaderText = "Vehicle type";
        column26.MinimumWidth = 6;
        column26.Name = "column26";
        column26.ReadOnly = true;
        // 
        // column27
        // 
        column27.HeaderText = "Area";
        column27.MinimumWidth = 6;
        column27.Name = "column27";
        column27.ReadOnly = true;
        // 
        // column28
        // 
        column28.HeaderText = "Status";
        column28.MinimumWidth = 6;
        column28.Name = "column28";
        column28.ReadOnly = true;
        // 
        // label21
        // 
        label21.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        label21.BackColor = Color.Transparent;
        label21.Font = new Font("Segoe UI", 9.22F, FontStyle.Bold);
        label21.ForeColor = Color.FromArgb(15, 23, 42);
        label21.Location = new Point(22, 20);
        label21.Margin = new Padding(4, 0, 4, 0);
        label21.Name = "label21";
        label21.Size = new Size(1015, 19);
        label21.TabIndex = 21;
        label21.Text = "Latest entries";
        // 
        // responsiveLayout
        // 
        responsiveLayout.AutoSize = true;
        responsiveLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        responsiveLayout.Dock = DockStyle.Top;
        responsiveLayout.Location = new Point(0, 0);
        responsiveLayout.Margin = new Padding(4, 4, 4, 4);
        responsiveLayout.MinimumSize = new Size(1261, 0);
        responsiveLayout.Name = "responsiveLayout";
        responsiveLayout.Padding = new Padding(25, 10, 25, 25);
        responsiveLayout.Size = new Size(1312, 35);
        responsiveLayout.TabIndex = 20;
        // 
        // VehicleEntry
        // 
        AutoScaleDimensions = new SizeF(120F, 120F);
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScroll = true;
        AutoScrollMinSize = new Size(1009, 618);
        BackColor = Color.FromArgb(248, 250, 252);
        Controls.Add(latestentriesPanel);
        Controls.Add(registervehiclePanel);
        Controls.Add(label1);
        Controls.Add(responsiveLayout);
        Font = new Font("Segoe UI", 9F);
        Margin = new Padding(4, 4, 4, 4);
        Name = "VehicleEntry";
        Size = new Size(1312, 772);
        registervehiclePanel.ResumeLayout(false);
        proofPanel.ResumeLayout(false);
        ratePanel.ResumeLayout(false);
        parkingareaPanel.ResumeLayout(false);
        vehicletypePanel.ResumeLayout(false);
        platenumberPanel.ResumeLayout(false);
        platenumberPanel.PerformLayout();
        latestentriesPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)grid22).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }
    #endregion
    private Label label1 = null!;
    private Panel registervehiclePanel = null!;
    private Label label4 = null!;
    private Label label6 = null!;
    private Panel platenumberPanel = null!;
    private TextBox platenumberTextBox = null!;
    private Label label10 = null!;
    private Panel vehicletypePanel = null!;
    private ComboBox vehicletypeComboBox = null!;
    private Label label14 = null!;
    private Panel parkingareaPanel = null!;
    private ComboBox parkingareaComboBox = null!;
    private Label rateTitleLabel = null!;
    private Panel ratePanel = null!;
    private Label rateLabel = null!;
    private Button saveEntryButton = null!;
    private Button clearButton = null!;
    private Panel latestentriesPanel = null!;
    private Label label21 = null!;
    private DataGridView grid22 = null!;
    private DataGridViewTextBoxColumn column24 = null!;
    private DataGridViewTextBoxColumn column25 = null!;
    private DataGridViewTextBoxColumn column26 = null!;
    private DataGridViewTextBoxColumn column27 = null!;
    private DataGridViewTextBoxColumn column28 = null!;
    private Panel responsiveLayout = null!;
    private Label labelProof = null!;
    private Panel proofPanel = null!;
    private Label proofLabel = null!;
    private Button editEntryButton = null!;
    private Button deleteEntryButton = null!;
}
