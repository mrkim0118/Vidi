namespace SimpleVideoPlayer;

public sealed class SkipSettingsForm : Form
{
    private readonly NumericUpDown _secondsInput;
    private readonly Action<int> _onChanged;

    public SkipSettingsForm(int currentSeconds, Action<int> onChanged)
    {
        _onChanged = onChanged;

        Text = "Skip seconds";
        ClientSize = new Size(260, 94);
        MinimumSize = new Size(240, 94);
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 3,
            RowCount = 1
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46));

        _secondsInput = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 3600,
            Value = Math.Clamp(currentSeconds, 1, 3600),
            Dock = DockStyle.Fill,
            TextAlign = HorizontalAlignment.Right
        };

        layout.Controls.Add(new Label
        {
            Text = "Seconds",
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        layout.Controls.Add(_secondsInput, 1, 0);
        layout.Controls.Add(new Label
        {
            Text = "sec",
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 2, 0);

        Controls.Add(layout);

        _secondsInput.ValueChanged += (_, _) => _onChanged((int)_secondsInput.Value);
    }
}
