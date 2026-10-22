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
        Width = 48;
        Height = 32;
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

        var fill = Enabled ? Color.White : Color.FromArgb(120, 120, 120);
        var border = Enabled ? Color.White : Color.FromArgb(120, 120, 120);
        var background = ClientRectangle;
        background.Width -= 1;
        background.Height -= 1;

        using var backgroundBrush = new SolidBrush(BackColor);
        using var borderPen = new Pen(border);
        using var brush = new SolidBrush(fill);

        graphics.FillRectangle(backgroundBrush, ClientRectangle);
        graphics.DrawRectangle(borderPen, background);

        if (_isPlaying)
        {
            var barWidth = 5;
            var barHeight = 15;
            var y = (Height - barHeight) / 2;
            var x = Width / 2 - 7;
            graphics.FillRectangle(brush, x, y, barWidth, barHeight);
            graphics.FillRectangle(brush, x + 9, y, barWidth, barHeight);
        }
        else
        {
            var points = new[]
            {
                new Point(Width / 2 - 5, Height / 2 - 9),
                new Point(Width / 2 - 5, Height / 2 + 9),
                new Point(Width / 2 + 10, Height / 2)
            };
            graphics.FillPolygon(brush, points);
        }
    }
}
