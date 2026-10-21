using System.Drawing;

namespace ParkingSystem;

/// <summary>
/// Design tokens and semantic color definitions for the application palette.
/// Primitive tokens:
///   --text:       #0f172a (RGB 15, 23, 42 - Slate 900)
///   --background: #f8fafc (RGB 248, 250, 252 - Slate 50)
///   --primary:    #059669 (RGB 5, 150, 105 - Emerald 600)
///   --secondary:  #e6f4ea (RGB 230, 244, 234 - Soft Mint)
///   --accent:     #10b981 (RGB 16, 185, 129 - Emerald 500)
/// </summary>
public static class AppTheme
{
    // Primitive Tokens
    public static readonly Color Text = Color.FromArgb(15, 23, 42);             // #0F172A Slate 900
    public static readonly Color Background = Color.FromArgb(248, 250, 252);    // #F8FAFC Slate 50
    public static readonly Color Primary = Color.FromArgb(5, 150, 105);         // #059669 Emerald 600
    public static readonly Color Secondary = Color.FromArgb(230, 244, 234);     // #E6F4EA Soft Mint
    public static readonly Color Accent = Color.FromArgb(16, 185, 129);         // #10B981 Emerald 500

    // Semantic Surfaces
    public static readonly Color CardBackground = Color.FromArgb(255, 255, 255); // Crisp clean white cards
    public static readonly Color CardBorder = Color.FromArgb(226, 232, 240);     // #E2E8F0 Clean Slate Border
    public static readonly Color InputBackground = Color.FromArgb(255, 255, 255);
    public static readonly Color InputBorder = Color.FromArgb(203, 213, 225);    // #CBD5E1 Slate 300
    public static readonly Color SidebarBackground = Color.FromArgb(240, 253, 244);// #F0FDF4 Emerald Tint
    public static readonly Color SidebarActive = Primary;

    // Metric Card Surfaces
    public static readonly Color MetricCard1 = Color.FromArgb(236, 253, 245);    // Light Emerald
    public static readonly Color MetricCard2 = Color.FromArgb(240, 249, 255);    // Soft Sky
    public static readonly Color MetricCard3 = Color.FromArgb(254, 243, 199);    // Warm Amber
    public static readonly Color MetricCard4 = Color.FromArgb(241, 245, 249);    // Crisp Slate

    // Semantic Text
    public static readonly Color TextPrimary = Text;
    public static readonly Color TextMuted = Color.FromArgb(100, 116, 139);      // #64748B Slate 500
    public static readonly Color TextAccent = Color.FromArgb(4, 120, 87);         // #047857 Emerald 700
    public static readonly Color BrandText = Color.FromArgb(4, 120, 87);

    // Semantic Actions
    public static readonly Color ButtonPrimaryBackground = Primary;
    public static readonly Color ButtonPrimaryForeground = Color.White;
    public static readonly Color ButtonSecondaryBackground = Color.FromArgb(241, 245, 249);
    public static readonly Color ButtonSecondaryForeground = Text;

    // Focus & Highlights
    public static readonly Color FocusBorder = Accent;
    public static readonly Color StatHighlight = Color.FromArgb(5, 150, 105);

    // Data Grids
    public static readonly Color GridLine = Color.FromArgb(226, 232, 240);
    public static readonly Color TableHeaderBackground = Color.FromArgb(241, 245, 249);
    public static readonly Color TableHeaderForeground = Color.FromArgb(51, 65, 85);
    public static readonly Color TableRowBackground = Color.White;
    public static readonly Color TableRowForeground = Text;
    public static readonly Color TableSelectionBackground = Color.FromArgb(209, 250, 229); // #D1FAE5 Soft Highlight
    public static readonly Color TableSelectionForeground = Color.FromArgb(6, 78, 59);     // #064E3B Deep Emerald
}
