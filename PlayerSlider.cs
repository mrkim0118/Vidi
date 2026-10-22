using System.Drawing.Drawing2D;

namespace SimpleVideoPlayer;

public sealed class PlayerSlider : Control
{
    private int _minimum;
    private int _maximum = 100;
    private int _value;
    private bool _dragging;

    public event EventHandler? UserValueChanged;
    public event EventHandler? DragStarted;
    public event EventHandler? DragCompleted;

    public PlayerSlider()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw
            | ControlStyles.UserPaint,
            true);

        Height = 24;
        BackColor = Color.FromArgb(22, 22, 22);
        TrackColor = Color.FromArgb(62, 62, 62);
        ProgressColor = Color.FromArgb(116, 185, 255);
        ThumbColor = Color.White;
        DisabledTrackColor = Color.FromArgb(48, 48, 48);
        Cursor = Cursors.Hand;
    }

    public int Minimum
    {
        get => _minimum;
        set
        {
            _minimum = value;
            if (_maximum < _minimum)
            {
                _maximum = _minimum;
            }

            Value = _value;
            Invalidate();
        }
    }

    public int Maximum
    {
        get => _maximum;
        set
        {
            _maximum = Math.Max(value, _minimum);
            Value = _value;
            Invalidate();
        }
    }

    public int Value
    {
        get => _value;
        set
        {
            var nextValue = Math.Clamp(value, _minimum, _maximum);
            if (_value == nextValue)
            {
                return;
            }

            _value = nextValue;
            Invalidate();
        }
    }

    public Color TrackColor { get; set; }

    public Color ProgressColor { get; set; }

    public Color ThumbColor { get; set; }

    public Color DisabledTrackColor { get; set; }

    public bool IsDragging => _dragging;

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var thumbRadius = 6;
        var trackHeight = 5;
        var trackLeft = thumbRadius;
        var trackWidth = Math.Max(Width - thumbRadius * 2, 1);
        var trackTop = (Height - trackHeight) / 2;
        var trackRect = new Rectangle(trackLeft, trackTop, trackWidth, trackHeight);
        var ratio = (_maximum == _minimum) ? 0 : (_value - _minimum) / (double)(_maximum - _minimum);
        var progressWidth = (int)Math.Round(trackWidth * ratio);
        var thumbCenterX = trackLeft + progressWidth;
        var thumbRect = new Rectangle(
            thumbCenterX - thumbRadius,
            Height / 2 - thumbRadius,
            thumbRadius * 2,
            thumbRadius * 2);

        using var trackBrush = new SolidBrush(Enabled ? TrackColor : DisabledTrackColor);
        using var progressBrush = new SolidBrush(Enabled ? ProgressColor : DisabledTrackColor);
        using var thumbBrush = new SolidBrush(Enabled ? ThumbColor : Color.FromArgb(110, 110, 110));

        FillRoundedRectangle(e.Graphics, trackBrush, trackRect, trackHeight);

        if (progressWidth > 0)
        {
            var progressRect = new Rectangle(trackLeft, trackTop, progressWidth, trackHeight);
            FillRoundedRectangle(e.Graphics, progressBrush, progressRect, trackHeight);
        }

        e.Graphics.FillEllipse(thumbBrush, thumbRect);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left || !Enabled)
        {
            return;
        }

        Focus();
        Capture = true;
        _dragging = true;
        DragStarted?.Invoke(this, EventArgs.Empty);
        SetValueFromMouse(e.X, userChange: true);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_dragging || !Enabled)
        {
            return;
        }

        SetValueFromMouse(e.X, userChange: true);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (!_dragging || e.Button != MouseButtons.Left)
        {
            return;
        }

        SetValueFromMouse(e.X, userChange: true);
        _dragging = false;
        Capture = false;
        DragCompleted?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        Invalidate();
    }

    private void SetValueFromMouse(int x, bool userChange)
    {
        var usableWidth = Math.Max(Width - 12, 1);
        var ratio = Math.Clamp((x - 6) / (double)usableWidth, 0, 1);
        Value = _minimum + (int)Math.Round(ratio * (_maximum - _minimum));

        if (userChange)
        {
            UserValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private static void FillRoundedRectangle(Graphics graphics, Brush brush, Rectangle rectangle, int radius)
    {
        if (rectangle.Width <= 0 || rectangle.Height <= 0)
        {
            return;
        }

        using var path = new GraphicsPath();
        var diameter = Math.Max(radius, 1);
        path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        graphics.FillPath(brush, path);
    }
}
