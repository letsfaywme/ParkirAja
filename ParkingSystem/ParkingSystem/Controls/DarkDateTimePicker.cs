namespace ParkingSystem;

public class DarkDateTimePicker : DateTimePicker
{
    public DarkDateTimePicker()
    {
        BackColor = AppTheme.InputBackground;
        ForeColor = AppTheme.Text;
        CalendarMonthBackground = AppTheme.CardBackground;
        CalendarForeColor = AppTheme.Text;
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg is not (0x000F or 0x0318) || !IsHandleCreated) return;
        // Keep native calendar/keyboard behavior, including Designer bitmap rendering.
        using var graphics = m.Msg == 0x0318 && m.WParam != IntPtr.Zero
            ? Graphics.FromHdc(m.WParam)
            : Graphics.FromHwnd(Handle);
        using var fill = new SolidBrush(BackColor);
        graphics.FillRectangle(fill, ClientRectangle);
        var textBounds = new Rectangle(2, 0, Math.Max(0, Width - 26), Height);
        TextRenderer.DrawText(graphics, Text, Font, textBounds, ForeColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(graphics, "⌄", Font, new Rectangle(Width - 24, 0, 24, Height), ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        if (Focused)
            ControlPaint.DrawFocusRectangle(graphics, Rectangle.Inflate(ClientRectangle, -1, -1), ForeColor, BackColor);
    }
}
