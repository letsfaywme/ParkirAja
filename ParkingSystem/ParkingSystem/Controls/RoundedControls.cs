using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace ParkingSystem;

internal static class RoundedShape
{
    internal static GraphicsPath Create(Rectangle bounds)
    {
        var path = new GraphicsPath();
        path.AddRoundedRectangle(bounds, new Size(12, 12));
        return path;
    }
}

public class RoundedPanel : Panel
{
    public RoundedPanel()
    {
        DoubleBuffered = true;
        Padding = new Padding(12, 10, 12, 10);
    }

    [DefaultValue(typeof(Color), "220, 227, 213")]
    public Color BorderColor { get; set; } = AppTheme.CardBorder;

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Width < 4 || Height < 4) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = RoundedShape.Create(new Rectangle(1, 1, Width - 3, Height - 3));
        using var fill = new SolidBrush(BackColor);
        using var border = new Pen(ContainsFocus ? AppTheme.FocusBorder : BorderColor);
        e.Graphics.Clear(Parent?.BackColor ?? BackColor);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);
    }

    protected override void OnEnter(EventArgs e) { base.OnEnter(e); Invalidate(); }
    protected override void OnLeave(EventArgs e) { base.OnLeave(e); Invalidate(); }
}

public class RoundedButton : Button
{
    public RoundedButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
        DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width < 4 || Height < 4) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Parent?.BackColor ?? BackColor);
        using var path = RoundedShape.Create(new Rectangle(1, 1, Width - 3, Height - 3));
        bool hover = ClientRectangle.Contains(PointToClient(MousePosition));
        var color = !Enabled ? SystemColors.ControlDark
            : Capture && hover ? ControlPaint.Dark(BackColor, .1f)
            : hover ? ControlPaint.Light(BackColor, .1f) : BackColor;
        using var fill = new SolidBrush(color);
        using var border = new Pen(Focused ? AppTheme.FocusBorder : BackColor);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);
        TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle,
            Enabled ? ForeColor : SystemColors.GrayText,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        if (Focused && ShowFocusCues)
            ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(ClientRectangle, -6, -6), ForeColor, color);
    }
}
