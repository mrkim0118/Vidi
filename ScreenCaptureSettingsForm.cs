namespace SimpleVideoPlayer;

public sealed class ScreenCaptureSettingsForm : Form
{
    private readonly ComboBox _formatBox;
    private readonly TextBox _folderBox;
    private readonly Action<ScreenCaptureOptions> _onChanged;

    public ScreenCaptureSettingsForm(ScreenCaptureOptions current, Action<ScreenCaptureOptions> onChanged)
    {
        _onChanged = onChanged;

        Text = "Screen Capture";
        ClientSize = new Size(500, 142);
        MinimumSize = new Size(460, 142);
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 3,
            RowCount = 3
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        _formatBox = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        foreach (var format in ScreenCaptureOptions.Formats)
        {
            _formatBox.Items.Add(format.ToUpperInvariant());
        }

        var selectedIndex = Math.Max(0, Array.IndexOf(ScreenCaptureOptions.Formats.ToArray(), current.Extension));
        _formatBox.SelectedIndex = selectedIndex;

        _folderBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Text = current.SaveFolder
        };

        var browseButton = new Button
        {
            Text = "Browse",
            Dock = DockStyle.Fill
        };

        var saveButton = new Button
        {
            Text = "OK",
            Dock = DockStyle.Right,
            Width = 86
        };

        layout.Controls.Add(CreateLabel("Format"), 0, 0);
        layout.Controls.Add(_formatBox, 1, 0);
        layout.SetColumnSpan(_formatBox, 2);
        layout.Controls.Add(CreateLabel("Folder"), 0, 1);
        layout.Controls.Add(_folderBox, 1, 1);
        layout.Controls.Add(browseButton, 2, 1);
        layout.Controls.Add(saveButton, 2, 2);

        Controls.Add(layout);

        browseButton.Click += (_, _) => BrowseFolder();
        saveButton.Click += (_, _) => SaveAndClose();
        AcceptButton = saveButton;
    }

    private static Label CreateLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private void BrowseFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Screen capture folder",
            SelectedPath = Directory.Exists(_folderBox.Text)
                ? _folderBox.Text
                : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _folderBox.Text = dialog.SelectedPath;
        }
    }

    private void SaveAndClose()
    {
        var folder = string.IsNullOrWhiteSpace(_folderBox.Text)
            ? ScreenCaptureOptions.CreateDefault().SaveFolder
            : _folderBox.Text.Trim();
        var format = (_formatBox.SelectedItem?.ToString() ?? "PNG").ToLowerInvariant();

        _onChanged(new ScreenCaptureOptions(format, folder));
        Close();
    }
}
