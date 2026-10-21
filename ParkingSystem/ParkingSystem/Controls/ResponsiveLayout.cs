namespace ParkingSystem;

internal static class ResponsiveLayout
{
    internal static void Attach(UserControl page)
    {
        page.SizeChanged += Resize;
        page.Load += Resize;
        Resize(page, EventArgs.Empty);
    }

    private static void Resize(object? sender, EventArgs e)
    {
        if (sender is not UserControl page) return;
        foreach (Control control in page.Controls)
        {
            if (control is not TableLayoutPanel layout) continue;
            layout.AutoSize = false;
            layout.Dock = DockStyle.None;
            layout.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            layout.Width = Math.Max(layout.MinimumSize.Width, page.ClientSize.Width);
            layout.Height = layout.GetPreferredSize(new Size(layout.Width, 0)).Height;
        }
    }
}
