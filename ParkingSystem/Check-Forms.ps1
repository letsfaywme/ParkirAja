$ErrorActionPreference = 'Stop'
$temporary = Join-Path ([IO.Path]::GetTempPath()) ('ParkingSystem-Forms-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $temporary | Out-Null
$project = [System.Security.SecurityElement]::Escape((Join-Path $PSScriptRoot 'ParkingSystem/ParkingSystem.csproj'))
try {
    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0-windows</TargetFramework><UseWindowsForms>true</UseWindowsForms><ImplicitUsings>enable</ImplicitUsings></PropertyGroup>
  <ItemGroup><ProjectReference Include="$project" /></ItemGroup>
</Project>
"@ | Set-Content (Join-Path $temporary 'Check.csproj')
    @'
using ParkingSystem;
using Npgsql;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text;

internal static class Check
{
    static string stage = "startup";
    static bool drivingUi;
    static void Stage(string value) { Volatile.Write(ref stage, value); Console.WriteLine("CHECK: " + value); Console.Out.Flush(); }
    [STAThread]
    static void Main()
    {
        using var globalWatchdog = new System.Threading.Timer(_ =>
        {
            Console.Error.WriteLine("FAIL: form checker exceeded 180 seconds at " + Volatile.Read(ref stage));
            Console.Error.Flush();
            Environment.Exit(1);
        }, null, 180000, Timeout.Infinite);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        Stage("initializing WinForms");
        ApplicationConfiguration.Initialize();
        Application.SetColorMode(SystemColorMode.Dark);
        using var source = NpgsqlDataSource.Create(DatabaseSettings.GetConnectionString());
        var service = new ParkingService(source, TimeProvider.System);
        var fixturePassword = Environment.GetEnvironmentVariable("PARKING_CHECKS_PASSWORD") ?? throw new Exception("Fixture password belum tersedia.");
        Stage("authenticating fixture Jukir");
        var jukirSession = Task.Run(() => service.AuthenticateAsync("check-jukir-a", fixturePassword, default)).GetAwaiter().GetResult() ?? throw new Exception("Fixture Jukir login gagal.");
        Stage("authenticating fixture admin");
        var adminSession = Task.Run(() => service.AuthenticateAsync("check-admin", fixturePassword, default)).GetAwaiter().GetResult() ?? throw new Exception("Fixture admin login gagal.");
        using var unexpectedDialogTimer = new System.Windows.Forms.Timer { Interval = 100 };
        unexpectedDialogTimer.Tick += (_, _) =>
        {
            if (drivingUi) return;
            EnumWindows((window, _) =>
            {
                GetWindowThreadProcessId(window, out uint pid);
                if (pid != Environment.ProcessId || !IsWindowVisible(window) || WindowClass(window) != "#32770") return true;
                var message = new StringBuilder();
                EnumChildWindows(window, (child, _) => { if (WindowClass(child) == "Static") message.Append(WindowText(child)).Append(' '); return true; }, IntPtr.Zero);
                Console.Error.WriteLine("FAIL unexpected dialog at " + stage + ": " + message);
                Console.Error.Flush();
                Environment.Exit(1);
                return false;
            }, IntPtr.Zero);
        };
        unexpectedDialogTimer.Start();
        string[] names = ["Login", "JukirDashboard", "AdminDashboard", "VehicleEntry", "VehicleExit", "TransactionHistory", "DailyReport", "ReportValidation", "ParkingAreas", "JukirAccounts", "ExportReports"];
        string images = Path.Combine(Path.GetTempPath(), "ParkingSystem-Forms");
        Directory.CreateDirectory(images);
        foreach (string name in names)
        {
            Stage("Designer " + name);
            using var view = (Control)Activator.CreateInstance(typeof(Login).Assembly.GetType("ParkingSystem." + name)!)!;
            using var form = view as Form ?? new Form { AutoScaleDimensions = new SizeF(96F, 96F), AutoScaleMode = AutoScaleMode.Dpi, ClientSize = new Size(1050, 618) };
            if (view is not Form) { view.Dock = DockStyle.Fill; form.Controls.Add(view); }
            form.Show();
            if (view is not Form) form.ClientSize = new Size(1050 * form.DeviceDpi / 96, 618 * form.DeviceDpi / 96);
            Application.DoEvents();
            var controls = Descendants(form).ToArray();
            foreach (var control in controls)
            {
                if (control.Parent is null || control is DataGridView) continue;
                var available = control.Parent is ScrollableControl scroll && scroll.AutoScroll ? scroll.DisplayRectangle.Size : control.Parent.ClientSize;
                if (control.Left < 0 || control.Top < 0 || control.Right > available.Width + 2 || control.Bottom > available.Height + 2)
                    throw new Exception(name + ": clipped control " + control.Name + " bounds=" + control.Bounds + " parent=" + control.Parent.ClientSize);
            }
            foreach (var grid in controls.OfType<DataGridView>())
            {
                if (grid.Columns.Count == 0 || !grid.ReadOnly || grid.AllowUserToAddRows)
                    throw new Exception(name + ": invalid presentation grid");
            }
            if (name == "Login")
            {
                var username = controls.OfType<TextBox>().Single(c => c.Name == "usernameTextBox");
                var password = controls.OfType<TextBox>().Single(c => c.Name == "passwordTextBox");
                username.Text = "check-user";
                if (username.Text != "check-user" || !password.UseSystemPasswordChar)
                    throw new Exception("Login input or password masking failed");
                username.Clear();
                username.Focus();
            }
            form.Refresh();
            Application.DoEvents();
            using var bitmap = new Bitmap(form.Width, form.Height);
            form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
            bitmap.Save(Path.Combine(images, name + ".png"));
            Console.WriteLine("PASS: " + name + " / " + controls.Length + " controls");
            form.Close();
        }
        Stage("creating runtime Jukir shell");
        using var shell = new MainForm(jukirSession, service);
        shell.Show();
        Application.DoEvents();
        var handle = shell.Handle;
        var buttons = Descendants(shell).OfType<RoundedButton>().Where(b => b.Tag is UserControl).ToArray();
        if (buttons.Length != 5 || buttons.Any(b => b.Tag is AdminDashboard or ReportValidation or ParkingAreas or JukirAccounts or ExportReports))
            throw new Exception("Jukir workspace exposes admin pages.");
        var entryButton = buttons.Single(b => b.Tag is VehicleEntry);
        var exitButton = buttons.Single(b => b.Tag is VehicleExit);
        entryButton.PerformClick();
        var entry = (VehicleEntry)entryButton.Tag!;
        var plate = Descendants(entry).OfType<TextBox>().Single();
        plate.Text = "B 1234 SPA";
        exitButton.PerformClick();
        entryButton.PerformClick();
        if (shell.Handle != handle || plate.Text != "B 1234 SPA" || !ReferenceEquals(entry, entryButton.Tag))
            throw new Exception("SPA navigation recreated shell/page or discarded field state.");
        foreach (var button in buttons)
        {
            Stage("Jukir navigation " + button.Text);
            button.PerformClick();
            Application.DoEvents();
            var page = (Control)button.Tag!;
            if (page.Parent!.Controls.GetChildIndex(page) != 0) throw new Exception("Navigation failed: " + button.Text);
            using var image = new Bitmap(shell.Width, shell.Height);
            shell.DrawToBitmap(image, new Rectangle(Point.Empty, shell.Size));
            image.Save(Path.Combine(images, "Shell-" + page.GetType().Name + ".png"));
        }
        if (Descendants(shell).OfType<Label>().Any(l => l.Text.Contains("System online", StringComparison.OrdinalIgnoreCase)))
            throw new Exception("Unnecessary System online badge remains.");
        var exit = (VehicleExit)exitButton.Tag!;
        if (Descendants(exit).OfType<TextBox>().Count(t => t.ReadOnly) != 6)
            throw new Exception("Vehicle details must use six read-only native text fields.");
        Stage("creating runtime admin shell");
        using var adminShell = new MainForm(adminSession, service);
        adminShell.Show();
        Application.DoEvents();
        var adminButtons = Descendants(adminShell).OfType<RoundedButton>().Where(b => b.Tag is UserControl).ToArray();
        if (adminButtons.Length != 5 || adminButtons.Any(b => b.Tag is JukirDashboard or VehicleEntry or VehicleExit or TransactionHistory or DailyReport))
            throw new Exception("Admin workspace exposes jukir pages.");
        foreach (var button in adminButtons)
        {
            Stage("Admin navigation " + button.Text);
            button.PerformClick();
            Application.DoEvents();
            var page = (Control)button.Tag!;
            if (!page.Visible || page.Parent!.Controls.GetChildIndex(page) != 0) throw new Exception("Admin navigation failed.");
            using var image = new Bitmap(adminShell.Width, adminShell.Height);
            adminShell.DrawToBitmap(image, new Rectangle(Point.Empty, adminShell.Size));
            image.Save(Path.Combine(images, "Shell-" + page.GetType().Name + ".png"));
        }
        foreach (var workspace in new[] { shell, adminShell })
        {
            var navigation = Descendants(workspace).OfType<RoundedButton>().Where(b => b.Tag is UserControl).ToArray();
            foreach (int width in new[] { 920, 1260, 1600 })
            {
                workspace.Size = new Size(width * workspace.DeviceDpi / 96, 760 * workspace.DeviceDpi / 96);
                foreach (var button in navigation)
                {
                    Stage("resize " + width + " / " + button.Text);
                    button.PerformClick();
                    Application.DoEvents();
                    var page = (UserControl)button.Tag!;
                    var layout = page.Controls.OfType<TableLayoutPanel>().Single();
                    if (layout.Width < layout.MinimumSize.Width || (page.ClientSize.Width >= layout.MinimumSize.Width && Math.Abs(layout.Width - page.ClientSize.Width) > 24))
                        throw new Exception("Responsive width failed: " + page.GetType().Name);
                    if (page.ClientSize.Width < layout.MinimumSize.Width && !page.HorizontalScroll.Visible)
                        throw new Exception("Narrow page content is unreachable: " + page.GetType().Name);
                    if (page.HorizontalScroll.Visible)
                    {
                        page.AutoScrollPosition = new Point(page.DisplayRectangle.Width, 0);
                        Application.DoEvents();
                        if (page.AutoScrollPosition.X >= 0) throw new Exception("Horizontal scrolling failed.");
                        page.AutoScrollPosition = Point.Empty;
                    }
                    foreach (Control child in layout.Controls)
                        if (child.Right > layout.Width + 2 || child.Bottom > layout.Height + 2)
                            throw new Exception("Responsive content overflow: " + child.Name);
                    using var capture = new Bitmap(workspace.Width, workspace.Height);
                    workspace.DrawToBitmap(capture, new Rectangle(Point.Empty, workspace.Size));
                    capture.Save(Path.Combine(images, "Resize-" + width + "-" + page.GetType().Name + ".png"));
                }
            }
        }
        Console.WriteLine("PASS: all ten pages resized at 920, 1260, 1600 logical pixels.");
        Console.WriteLine("PASS: separate Jukir/Admin sidebars, five pages each, stable shell/page instances, preserved plate input.");
        Stage("transaction UI smoke");
        SmokeTransactions(source, shell, buttons, images);
        Stage("missing connection login");
        using (var missing = new Login(null, "Koneksi database belum dikonfigurasi. Atur PARKING_DB_CONNECTION."))
        {
            missing.Show();
            var missingUsername = Named<TextBox>(missing, "usernameTextBox");
            var missingPassword = Named<TextBox>(missing, "passwordTextBox");
            missingUsername.Text = "keep-user";
            missingPassword.Text = "do-not-keep";
            var missingButton = Named<RoundedButton>(missing, "signInButton");
            DriveUi(missingButton.PerformClick, () => missingButton.Enabled, images,
                [("PARKING_DB_CONNECTION", 1)]);
            if (missingUsername.Text != "keep-user" || missingPassword.Text.Length != 0 || Application.OpenForms.OfType<MainForm>().Any(f => f.Owner == missing))
                throw new Exception("Missing connection login opened workspace or lost/retained wrong input.");
            missing.Close();
        }
        Console.WriteLine("PASS: missing connection blocks login, preserves username, clears password.");
        Stage("authenticated login smoke");
        using var login = new Login(service, null);
        login.Show();
        if (login.MaximizeBox) throw new Exception("Login must not allow maximize.");
        foreach (int width in new[] { 480, 620, 900 })
        {
            login.ClientSize = new Size(width * login.DeviceDpi / 96, 640 * login.DeviceDpi / 96);
            Application.DoEvents();
            var fields = Descendants(login).OfType<TableLayoutPanel>().Single(p => p.Controls.OfType<TextBox>().Any() || p.Controls.OfType<RoundedPanel>().Any());
            var center = fields.Parent!.PointToClient(login.PointToScreen(new Point(login.ClientSize.Width / 2, login.ClientSize.Height / 2)));
            if (Math.Abs(fields.Left + fields.Width / 2 - center.X) > 8 || Math.Abs(fields.Top + fields.Height / 2 - center.Y) > 8)
                throw new Exception("Login fields are not centered.");
        }
        var loginControls = Descendants(login).ToArray();
        if (loginControls.OfType<ComboBox>().Any()) throw new Exception("Preview role selector remains.");
        var usernameInput = loginControls.OfType<TextBox>().Single(t => t.Name == "usernameTextBox");
        var passwordInput = loginControls.OfType<TextBox>().Single(t => t.UseSystemPasswordChar);
        var signIn = loginControls.OfType<RoundedButton>().Single();
        foreach (int role in new[] { 0, 1 })
        {
            usernameInput.Text = role == 0 ? "check-jukir-a" : "check-admin";
            passwordInput.Text = fixturePassword;
            bool opened = false;
            using var timer = new System.Windows.Forms.Timer { Interval = 100 };
            timer.Tick += (_, _) =>
            {
                var workspace = Application.OpenForms.OfType<MainForm>().LastOrDefault(f => f.Owner == login);
                if (workspace is null) return;
                opened = Descendants(workspace).Any(c => role == 0 ? c is JukirDashboard : c is AdminDashboard);
                timer.Stop();
                Descendants(workspace).OfType<RoundedButton>().Single(b => b.Name == "signOutButton").PerformClick();
            };
            timer.Start();
            DriveUi(signIn.PerformClick, () => opened && login.Visible && signIn.Enabled, images, []);
            timer.Stop();
            if (!opened || !login.Visible || passwordInput.Text.Length != 0)
                throw new Exception($"Authenticated login or Sign Out return failed: role={role}, opened={opened}, visible={login.Visible}, passwordCleared={passwordInput.Text.Length == 0}, enabled={signIn.Enabled}, forms={string.Join(',', Application.OpenForms.Cast<Form>().Select(f => f.GetType().Name))}");
        }
        Console.WriteLine("PASS: authenticated Login routes both roles; Sign Out returns and clears password.");
        Console.WriteLine("Screenshots: " + images);
    }

    static T Named<T>(Control owner, string name) where T : Control => Descendants(owner).OfType<T>().Single(c => c.Name == name);

    static void SmokeTransactions(NpgsqlDataSource source, MainForm shell, RoundedButton[] buttons, string images)
    {
        Stage("entry location binding");
        var entryButton = buttons.Single(b => b.Tag is VehicleEntry);
        var exitButton = buttons.Single(b => b.Tag is VehicleExit);
        var historyButton = buttons.Single(b => b.Tag is TransactionHistory);
        var entry = (VehicleEntry)entryButton.Tag!;
        var exit = (VehicleExit)exitButton.Tag!;
        string plate = "SM" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
        entryButton.PerformClick();
        PumpUntil(() => Named<ComboBox>(entry, "parkingareaComboBox").Items.Count == 1, "entry location binding");
        var entryPlate = Named<TextBox>(entry, "platenumberTextBox");
        entryPlate.Text = plate;
        Named<ComboBox>(entry, "vehicletypeComboBox").SelectedIndex = 0;
        var save = Named<RoundedButton>(entry, "saveEntryButton");
        Stage("entry save and printer failure semantics");
        DriveUi(save.PerformClick, () => Named<RoundedPanel>(entry, "registervehiclePanel").Enabled, images,
            [("Transaksi tersimpan; bukti belum dicetak.", 1)], preview: false);
        Console.WriteLine("INFO: printer default tidak tersedia; transaksi tersimpan tanpa rollback. Preview fisik belum diverifikasi.");
        var saved = ReadSmokeTransaction(source, plate);
        if (saved.Status != "active" || saved.ExitTime is not null || saved.Fare is not null || entryPlate.Text.Length != 0)
            throw new Exception("Entry UI did not commit active transaction before canceled printing.");
        entryPlate.Text = plate;
        Stage("duplicate entry popup");
        DriveUi(save.PerformClick, () => Named<RoundedPanel>(entry, "registervehiclePanel").Enabled, images,
            [("Kendaraan masih terparkir.", 1)]);
        if (entryPlate.Text != plate || ReadSmokeTransaction(source, plate).Id != saved.Id)
            throw new Exception("Duplicate entry changed input or transaction.");

        exitButton.PerformClick();
        Named<TextBox>(exit, "plateTextBox").Text = plate;
        var process = Named<RoundedButton>(exit, "processVehicleExitButton");
        Stage("exit find");
        DriveUi(Named<RoundedButton>(exit, "findButton").PerformClick, () => process.Enabled, images, []);
        if (Named<TextBox>(exit, "platenumberTextBox").Text != plate)
            throw new Exception("Find did not populate stored plate.");
        Stage("exit cancel payment");
        DriveUi(process.PerformClick, () => process.Enabled, images, [("sudah diterima?", 7)]);
        var canceled = ReadSmokeTransaction(source, plate);
        if (canceled.Status != "active" || canceled.ExitTime is not null || canceled.Fare is not null)
            throw new Exception("Canceled payment changed transaction.");
        Stage("exit confirm payment");
        DriveUi(process.PerformClick, () => !process.Enabled, images,
            [("sudah diterima?", 6), ("Pembayaran tersimpan; kendaraan keluar.", 1)]);
        var completed = ReadSmokeTransaction(source, plate);
        if (completed.Status != "completed" || completed.ExitTime is null || completed.Fare != completed.Rate || completed.ExitJukirId is null)
            throw new Exception("Confirmed payment missing atomic DB completion.");

        historyButton.PerformClick();
        var history = (TransactionHistory)historyButton.Tag!;
        Named<DarkDateTimePicker>(history, "dateDarkDateTimePicker").Value = DateTime.UtcNow.AddHours(7).Date;
        Named<ComboBox>(history, "vehicletypeComboBox").SelectedIndex = 0;
        Named<TextBox>(history, "platenumberTextBox").Text = plate;
        var grid = Named<DataGridView>(history, "grid21");
        Stage("history search");
        DriveUi(Named<RoundedButton>(history, "searchButton").PerformClick,
            () => grid.Rows.Count == 1 && grid.Rows.Cast<DataGridViewRow>().Any(r => r.Cells.Cast<DataGridViewCell>().Any(c => Equals(c.Value, plate))), images, []);
        grid.CurrentCell = grid.Rows[0].Cells[0];
        var cell = grid.GetCellDisplayRectangle(0, 0, false);
        int point = ((cell.Top + cell.Height / 2) << 16) | ((cell.Left + cell.Width / 2) & 0xffff);
        Stage("history detail double click");
        DriveUi(() => SendMessage(grid.Handle, 0x0203, new IntPtr(1), new IntPtr(point)), () => true, images, [], detail: "Detail transaksi #" + saved.Id);
        Console.WriteLine("PASS: actual entry/preview cancellation, duplicate input preservation, find/payment cancellation and confirmation, history detail; DB values verified.");
    }

    static (int Id, string Status, DateTime? ExitTime, decimal? Fare, decimal Rate, int? ExitJukirId) ReadSmokeTransaction(NpgsqlDataSource source, string plate)
    {
        using var command = source.CreateCommand("SELECT id,status,exit_time,fare,rate_at_entry,exit_jukir_id FROM parking.parking_transactions WHERE plate_number=@plate");
        command.Parameters.AddWithValue("plate", plate);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw new Exception("Smoke transaction not stored.");
        var result = (reader.GetInt32(0), reader.GetString(1), reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2),
            reader.IsDBNull(3) ? (decimal?)null : reader.GetDecimal(3), reader.GetDecimal(4), reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5));
        if (reader.Read()) throw new Exception("Duplicate smoke transaction stored.");
        return result;
    }

    static void PumpUntil(Func<bool> ready, string operation)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);
        do
        {
            Application.DoEvents();
            if (ready()) return;
            Thread.Sleep(10);
        } while (DateTime.UtcNow < deadline);
        throw new Exception("Timed out: " + operation);
    }

    static void DriveUi(Action action, Func<bool> finished, string images, (string Text, int Response)[] expected, bool preview = false, string detail = null)
    {
        drivingUi = true;
        int index = 0;
        bool previewSeen = false, detailSeen = false;
        string failure = null;
        using var watchdog = new System.Threading.Timer(_ =>
        {
            Console.Error.WriteLine("FAIL: native UI workflow exceeded 30 seconds.");
            Environment.Exit(1);
        }, null, 30000, Timeout.Infinite);
        using var timer = new System.Windows.Forms.Timer { Interval = 50 };
        timer.Tick += (_, _) =>
        {
            foreach (var form in Application.OpenForms.Cast<Form>().ToArray())
            {
                if (form is PrintPreviewDialog && form.Text.StartsWith("Bukti parkir #"))
                {
                    if (!preview) failure = "Unexpected print preview.";
                    previewSeen = true;
                    using var bitmap = new Bitmap(form.Width, form.Height);
                    form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
                    bitmap.Save(Path.Combine(images, "Smoke-EntryReceipt.png"));
                    form.Close();
                }
                else if (detail is not null && form.Text == detail)
                {
                    detailSeen = true;
                    if (Descendants(form).OfType<TextBox>().Count(t => t.ReadOnly) != 11)
                        failure = "Transaction detail missing readonly fields.";
                    using var bitmap = new Bitmap(form.Width, form.Height);
                    form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
                    bitmap.Save(Path.Combine(images, "Smoke-TransactionDetail.png"));
                    form.Close();
                }
            }
            if (Application.OpenForms.OfType<PrintPreviewDialog>().Any()) return;
            EnumWindows((window, ignoredParent) =>
            {
                GetWindowThreadProcessId(window, out uint processId);
                if (processId != Environment.ProcessId || !IsWindowVisible(window) || WindowClass(window) != "#32770") return true;
                var text = new StringBuilder();
                EnumChildWindows(window, (child, ignoredChild) => { if (WindowClass(child) == "Static") text.Append(WindowText(child)).Append(' '); return true; }, IntPtr.Zero);
                if (text.Length == 0) return true;
                int response = 2;
                if (index < expected.Length && text.ToString().Contains(expected[index].Text, StringComparison.Ordinal)) response = expected[index++].Response;
                else failure = "Unexpected native dialog: " + text;
                SendMessage(window, 0x0111, new IntPtr(response), IntPtr.Zero);
                return true;
            }, IntPtr.Zero);
        };
        timer.Start();
        action();
        PumpUntil(() => failure is not null || (finished() && index == expected.Length && (!preview || previewSeen) && (detail is null || detailSeen)), "UI workflow");
        timer.Stop();
        if (failure is not null) throw new Exception(failure);
        drivingUi = false;
    }

    delegate bool WindowVisitor(IntPtr window, IntPtr parameter);
    [DllImport("user32.dll")] static extern bool EnumWindows(WindowVisitor visitor, IntPtr parameter);
    [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr parent, WindowVisitor visitor, IntPtr parameter);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr window, StringBuilder text, int maximum);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr window, StringBuilder text, int maximum);
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
    static string WindowText(IntPtr window) { var text = new StringBuilder(2048); GetWindowText(window, text, text.Capacity); return text.ToString(); }
    static string WindowClass(IntPtr window) { var text = new StringBuilder(256); GetClassName(window, text, text.Capacity); return text.ToString(); }

    static IEnumerable<Control> Descendants(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            yield return child;
            foreach (Control descendant in Descendants(child)) yield return descendant;
        }
    }
}
'@ | Set-Content (Join-Path $temporary 'Program.cs')
    dotnet run --project (Join-Path $temporary 'Check.csproj')
    if ($LASTEXITCODE -ne 0) { throw 'Form checks failed.' }
} finally {
    Remove-Item $temporary -Recurse -Force
}
