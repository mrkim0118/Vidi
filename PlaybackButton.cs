namespace SimpleVideoPlayer;

public sealed class PlaybackButton : Button
{
    private bool _isPlaying;

    public PlaybackButton()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw
            | ControlStyles.UserPaint,
            true);

        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Color.FromArgb(46, 46, 46);
        ForeColor = Color.White;
        Width = 44;
        Height = 44;
        Margin = new Padding(0, 0, 8, 0);
        UseVisualStyleBackColor = false;
        Cursor = Cursors.Hand;
        Text = string.Empty;
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        set
        {
            if (_isPlaying == value)
            {
                return;
            }

            _isPlaying = value;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        var graphics = pevent.Graphics;
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // White disc with a dark glyph; slightly dimmed while hovered.
        var hovered = ClientRectangle.Contains(PointToClient(MousePosition));
        var disc = Enabled ? (hovered ? Color.FromArgb(225, 225, 225) : Color.FromArgb(242, 242, 242)) : Color.FromArgb(110, 110, 110);
        var glyph = Color.FromArgb(20, 20, 20);
        var circle = new Rectangle(0, 0, Math.Min(Width, Height) - 1, Math.Min(Width, Height) - 1);
        circle.Offset((Width - circle.Width) / 2, (Height - circle.Height) / 2);

        using var backgroundBrush = new SolidBrush(BackColor);
        using var discBrush = new SolidBrush(disc);
        using var brush = new SolidBrush(glyph);

        graphics.FillRectangle(backgroundBrush, ClientRectangle);
        graphics.FillEllipse(discBrush, circle);

        var cx = circle.Left + circle.Width / 2;
        var cy = circle.Top + circle.Height / 2;
        if (_isPlaying)
        {
            graphics.FillRectangle(brush, cx - 6, cy - 7, 4, 14);
            graphics.FillRectangle(brush, cx + 2, cy - 7, 4, 14);
        }
        else
        {
            graphics.FillPolygon(brush, new[]
            {
                new Point(cx - 4, cy - 8),
                new Point(cx - 4, cy + 8),
                new Point(cx + 9, cy)
            });
        }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        Invalidate();
    }
}
