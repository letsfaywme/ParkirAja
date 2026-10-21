namespace ParkingSystem;

public partial class Login : Form
{
    private readonly ParkingService? service;
    private readonly string? setupError;
    private readonly CancellationTokenSource lifetime = new();
    private bool lifetimeDisposed;

    public Login()
    {
        InitializeComponent();
        signInButton.Enabled = false;
        Disposed += (_, _) =>
        {
            if (lifetimeDisposed) return;
            lifetimeDisposed = true;
            lifetime.Cancel();
            lifetime.Dispose();
        };
    }

    public Login(ParkingService? service, string? setupError) : this()
    {
        this.service = service;
        this.setupError = setupError;
        signInButton.Enabled = true;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (!e.Cancel) lifetime.Cancel();
    }

    private async void OpenWorkspace(object? sender, EventArgs e)
    {
        if (!signInButton.Enabled || lifetime.IsCancellationRequested) return;
        signInButton.Enabled = false;
        var cancellationToken = lifetime.Token;
        try
        {
            if (service is null)
            {
                MessageBox.Show(this, setupError ?? "Koneksi database belum dikonfigurasi. Atur PARKING_DB_CONNECTION.",
                    "Sign In", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var session = await service.AuthenticateAsync(usernameTextBox.Text, passwordTextBox.Text, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            passwordTextBox.Clear();
            if (session is null)
            {
                MessageBox.Show(this, "Username atau password tidak valid.", "Sign In",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var workspace = new MainForm(session, service);
            Hide();
            try { workspace.ShowDialog(this); }
            finally
            {
                if (!IsDisposed && !cancellationToken.IsCancellationRequested)
                {
                    Show();
                    usernameTextBox.Focus();
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
            if (!IsDisposed && !cancellationToken.IsCancellationRequested)
                MessageBox.Show(this, "Tidak dapat masuk. Periksa koneksi database dan konfigurasi aplikasi.",
                    "Sign In", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if (!IsDisposed)
            {
                passwordTextBox.Clear();
                signInButton.Enabled = !cancellationToken.IsCancellationRequested;
            }
        }
    }
}
