using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace Vidi;

// Context menus in the player's dark palette: dark surface, light text, orange check marks, rounded corners on Windows 11.
public sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
{
    private static readonly Color Surface = Color.FromArgb(32, 32, 32);
    private static readonly Color Hover = Color.FromArgb(52, 52, 52);
    private static readonly Color Border = Color.FromArgb(58, 58, 58);
    private static readonly Color Text = Color.FromArgb(230, 230, 230);
    private static readonly Color DisabledText = Color.FromArgb(110, 110, 110);
    private static readonly Color Accent = Color.FromArgb(255, 154, 60);

    public DarkMenuRenderer() : base(new DarkColors())
    {
        RoundedEdges = false;
    }

    // Applies the renderer to the menu and every submenu, including ones added later.
    public static void Apply(ToolStripDropDown menu)
    {
        menu.Renderer = new DarkMenuRenderer();
        menu.BackColor = Surface;
        menu.Font = new Font("Segoe UI", 9.75f);
        menu.Opened += (_, _) => RoundCorners(menu);
        ApplyToItems(menu.Items);
        menu.ItemAdded += (_, e) => ApplyToItem(e.Item);
    }

    private static void ApplyToItems(ToolStripItemCollection items)
    {
        foreach (ToolStripItem item in items)
        {
            ApplyToItem(item);
        }
    }

    private static void ApplyToItem(ToolStripItem? item)
    {
        if (item is null)
        {
            return;
        }

        item.ForeColor = Text;
        item.Padding = new Padding(2, 3, 2, 3);
        if (item is ToolStripMenuItem { DropDown: { } dropDown } && dropDown.Renderer is not DarkMenuRenderer)
        {
            Apply(dropDown);
        }
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? Text : DisabledText;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        e.ArrowColor = e.Item?.Enabled == false ? DisabledText : Text;
        base.OnRenderArrow(e);
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var r = e.ImageRectangle;
        using var pen = new Pen(Accent, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        g.DrawLines(pen, new[]
        {
            new PointF(r.Left + r.Width * 0.2f, r.Top + r.Height * 0.52f),
            new PointF(r.Left + r.Width * 0.42f, r.Top + r.Height * 0.74f),
            new PointF(r.Left + r.Width * 0.8f, r.Top + r.Height * 0.3f)
        });
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        var y = e.Item.Height / 2;
        using var pen = new Pen(Border);
        e.Graphics.DrawLine(pen, 8, y, e.Item.Width - 8, y);
    }

    private static void RoundCorners(Control menu)
    {
        var round = 2; // DWMWCP_ROUND
        _ = DwmSetWindowAttribute(menu.Handle, 33, ref round, sizeof(int)); // DWMWA_WINDOW_CORNER_PREFERENCE; no-op before Windows 11.
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    private sealed class DarkColors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Surface;
        public override Color ImageMarginGradientBegin => Surface;
        public override Color ImageMarginGradientMiddle => Surface;
        public override Color ImageMarginGradientEnd => Surface;
        public override Color MenuBorder => Border;
        public override Color MenuItemBorder => Hover;
        public override Color MenuItemSelected => Hover;
        public override Color MenuItemSelectedGradientBegin => Hover;
        public override Color MenuItemSelectedGradientEnd => Hover;
        public override Color MenuItemPressedGradientBegin => Hover;
        public override Color MenuItemPressedGradientEnd => Hover;
        public override Color CheckBackground => Color.Transparent;
        public override Color CheckSelectedBackground => Color.Transparent;
        public override Color CheckPressedBackground => Color.Transparent;
        public override Color SeparatorDark => Border;
        public override Color SeparatorLight => Border;
    }
}
