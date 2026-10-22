using LibVLCSharp.Shared;

namespace SimpleVideoPlayer;

public sealed class VideoControlForm : Form
{
    private readonly MediaPlayer _mediaPlayer;
    private readonly TrackBar _brightnessBar;
    private readonly TrackBar _contrastBar;
    private readonly TrackBar _saturationBar;
    private readonly TrackBar _gammaBar;
    private readonly TrackBar _hueBar;
    private readonly Label _brightnessValue;
    private readonly Label _contrastValue;
    private readonly Label _saturationValue;
    private readonly Label _gammaValue;
    private readonly Label _hueValue;
    private bool _syncing;

    public VideoControlForm(MediaPlayer mediaPlayer)
    {
        _mediaPlayer = mediaPlayer;

        Text = "Effect";
        ClientSize = new Size(420, 250);
        MinimumSize = new Size(380, 240);
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 3,
            RowCount = 6
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 54));

        _brightnessBar = CreateBar(0, 200, 100);
        _contrastBar = CreateBar(0, 200, 100);
        _saturationBar = CreateBar(0, 300, 100);
        _gammaBar = CreateBar(10, 400, 100);
        _hueBar = CreateBar(-180, 180, 0);

        _brightnessValue = CreateValueLabel();
        _contrastValue = CreateValueLabel();
        _saturationValue = CreateValueLabel();
        _gammaValue = CreateValueLabel();
        _hueValue = CreateValueLabel();

        AddRow(layout, 0, "Brightness", _brightnessBar, _brightnessValue);
        AddRow(layout, 1, "Contrast", _contrastBar, _contrastValue);
        AddRow(layout, 2, "Saturation", _saturationBar, _saturationValue);
        AddRow(layout, 3, "Gamma", _gammaBar, _gammaValue);
        AddRow(layout, 4, "Hue", _hueBar, _hueValue);

        var resetButton = new Button
        {
            Text = "Reset",
            Dock = DockStyle.Right,
            Width = 88
        };
        layout.Controls.Add(resetButton, 1, 5);
        layout.SetColumnSpan(resetButton, 2);

        Controls.Add(layout);

        _brightnessBar.Scroll += (_, _) => ApplyAdjustments();
        _contrastBar.Scroll += (_, _) => ApplyAdjustments();
        _saturationBar.Scroll += (_, _) => ApplyAdjustments();
        _gammaBar.Scroll += (_, _) => ApplyAdjustments();
        _hueBar.Scroll += (_, _) => ApplyAdjustments();
        resetButton.Click += (_, _) => ResetAdjustments();

        ApplyAdjustments();
    }

    private static TrackBar CreateBar(int minimum, int maximum, int value)
    {
        return new TrackBar
        {
            Minimum = minimum,
            Maximum = maximum,
            Value = value,
            TickFrequency = Math.Max((maximum - minimum) / 10, 1),
            Dock = DockStyle.Fill
        };
    }

    private static Label CreateValueLabel()
    {
        return new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight
        };
    }

    private static void AddRow(TableLayoutPanel layout, int row, string text, Control slider, Control valueLabel)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        layout.Controls.Add(new Label
        {
            Text = text,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, row);

        layout.Controls.Add(slider, 1, row);
        layout.Controls.Add(valueLabel, 2, row);
    }

    private void ApplyAdjustments()
    {
        if (_syncing)
        {
            return;
        }

        _mediaPlayer.SetAdjustInt(VideoAdjustOption.Enable, 1);
        _mediaPlayer.SetAdjustFloat(VideoAdjustOption.Brightness, PercentToFloat(_brightnessBar.Value));
        _mediaPlayer.SetAdjustFloat(VideoAdjustOption.Contrast, PercentToFloat(_contrastBar.Value));
        _mediaPlayer.SetAdjustFloat(VideoAdjustOption.Saturation, PercentToFloat(_saturationBar.Value));
        _mediaPlayer.SetAdjustFloat(VideoAdjustOption.Gamma, PercentToFloat(_gammaBar.Value));
        _mediaPlayer.SetAdjustFloat(VideoAdjustOption.Hue, _hueBar.Value);

        _brightnessValue.Text = $"{_brightnessBar.Value}%";
        _contrastValue.Text = $"{_contrastBar.Value}%";
        _saturationValue.Text = $"{_saturationBar.Value}%";
        _gammaValue.Text = $"{_gammaBar.Value}%";
        _hueValue.Text = _hueBar.Value.ToString();
    }

    private void ResetAdjustments()
    {
        _syncing = true;
        _brightnessBar.Value = 100;
        _contrastBar.Value = 100;
        _saturationBar.Value = 100;
        _gammaBar.Value = 100;
        _hueBar.Value = 0;
        _syncing = false;

        ApplyAdjustments();
    }

    private static float PercentToFloat(int value)
    {
        return value / 100f;
    }
}
