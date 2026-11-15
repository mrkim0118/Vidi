using System.Drawing.Drawing2D;

namespace SimpleVideoPlayer;

// Shown over the black video area until a file is opened: icon, "drop a video" text, open hint.
public sealed class EmptyStateHint : Control
{
    private readonly Font _iconFont;
    private readonly Font _titleFont = new("Segoe UI", 11f);
    private readonly Font _subtitleFont = new("Segoe UI", 9f);
    private string _title = string.Empty;
    private string _subtitle = string.Empty;

    public EmptyStateHint(FontFamily iconFamily)
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw
            | ControlStyles.UserPaint,
            true);

        _iconFont = new Font(iconFamily, 22f);
        Size = new Size(340, 140);
        BackColor = Color.Black;
        Cursor = Cursors.Hand;
    }

    public void SetText(string title, string subtitle)
    {
        _title = title;
        _subtitle = subtitle;
        AccessibleName = title;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using var borderPen = new Pen(Color.FromArgb(70, 70, 70)) { DashStyle = DashStyle.Dash };
        using var path = RoundedRectangle(new Rectangle(0, 0, Width - 1, Height - 1), 12);
        g.DrawPath(borderPen, path);

        var center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        using var iconBrush = new SolidBrush(Color.FromArgb(122, 122, 122));
        using var titleBrush = new SolidBrush(Color.FromArgb(207, 207, 207));
        using var subtitleBrush = new SolidBrush(Color.FromArgb(140, 140, 140));
        g.DrawString("", _iconFont, iconBrush, new RectangleF(0, 14, Width, 40), center);
        g.DrawString(_title, _titleFont, titleBrush, new RectangleF(0, 58, Width, 28), center);
        g.DrawString(_subtitle, _subtitleFont, subtitleBrush, new RectangleF(0, 88, Width, 24), center);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _iconFont.Dispose();
            _titleFont.Dispose();
            _subtitleFont.Dispose();
        }

        base.Dispose(disposing);
    }

    private static GraphicsPath RoundedRectangle(Rectangle r, int radius)
    {
        var d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(r.Left, r.Top, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
