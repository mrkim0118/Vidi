namespace Vidi;

public sealed class PlaylistForm : Form
{
    private readonly ListBox _listBox;
    private readonly Button _addButton;
    private readonly Button _removeButton;
    private readonly Button _upButton;
    private readonly Button _downButton;
    private readonly Button _playButton;

    public PlaylistForm(
        bool useKorean,
        Action addRequested,
        Action<int> removeRequested,
        Action<int, int> moveRequested,
        Action<int> playRequested)
    {
        Text = useKorean ? "재생 목록" : "Playlist";
        Width = 420;
        Height = 460;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(320, 320);
        BackColor = Color.FromArgb(28, 28, 28);
        ForeColor = Color.White;

        _listBox = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(18, 18, 18),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            IntegralHeight = false
        };

        _addButton = CreateButton(useKorean ? "추가" : "Add");
        _removeButton = CreateButton(useKorean ? "삭제" : "Remove");
        _upButton = CreateButton(useKorean ? "위" : "Up");
        _downButton = CreateButton(useKorean ? "아래" : "Down");
        _playButton = CreateButton(useKorean ? "재생" : "Play");

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            Padding = new Padding(8, 6, 8, 6),
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.FromArgb(28, 28, 28)
        };
        buttons.Controls.AddRange(new Control[] { _addButton, _removeButton, _upButton, _downButton, _playButton });

        Controls.Add(_listBox);
        Controls.Add(buttons);

        _addButton.Click += (_, _) => addRequested();
        _removeButton.Click += (_, _) =>
        {
            if (_listBox.SelectedIndex >= 0)
            {
                removeRequested(_listBox.SelectedIndex);
            }
        };
        _upButton.Click += (_, _) =>
        {
            if (_listBox.SelectedIndex > 0)
            {
                moveRequested(_listBox.SelectedIndex, _listBox.SelectedIndex - 1);
            }
        };
        _downButton.Click += (_, _) =>
        {
            if (_listBox.SelectedIndex >= 0 && _listBox.SelectedIndex < _listBox.Items.Count - 1)
            {
                moveRequested(_listBox.SelectedIndex, _listBox.SelectedIndex + 1);
            }
        };
        _playButton.Click += (_, _) =>
        {
            if (_listBox.SelectedIndex >= 0)
            {
                playRequested(_listBox.SelectedIndex);
            }
        };
        _listBox.DoubleClick += (_, _) =>
        {
            if (_listBox.SelectedIndex >= 0)
            {
                playRequested(_listBox.SelectedIndex);
            }
        };
    }

    public void SetPlaylist(IReadOnlyList<string> files, int currentIndex)
    {
        var selectedIndex = _listBox.SelectedIndex;
        _listBox.BeginUpdate();
        try
        {
            _listBox.Items.Clear();
            for (var i = 0; i < files.Count; i++)
            {
                var prefix = i == currentIndex ? "> " : "  ";
                _listBox.Items.Add(prefix + Path.GetFileName(files[i]));
            }

            if (currentIndex >= 0 && currentIndex < _listBox.Items.Count)
            {
                _listBox.SelectedIndex = currentIndex;
            }
            else if (selectedIndex >= 0 && selectedIndex < _listBox.Items.Count)
            {
                _listBox.SelectedIndex = selectedIndex;
            }
        }
        finally
        {
            _listBox.EndUpdate();
        }
    }

    private static Button CreateButton(string text)
    {
        return new Button
        {
            Text = text,
            Width = 72,
            Height = 30,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(48, 48, 48),
            ForeColor = Color.White,
            UseVisualStyleBackColor = false
        };
    }
}
