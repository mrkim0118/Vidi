using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace SimpleVideoPlayer;

public partial class Form1 : Form
{
    private const int SeekMaximum = 10_000;
    private const int ShowNormal = 5;
    private const string AppTitle = "Tech Archive Video Player";
    private const string AppVersion = "1.0.0";
    private const string UpdateUrl = "https://tech.trendarchive.io/Utility/VideoPlayer/";

    private readonly AppUpdateService _appUpdateService = new(UpdateUrl);
    private readonly VideoHostPanel _videoHost;
    private readonly VideoView _videoView;
    private readonly Panel _bottomPanel;
    private readonly Button _openButton;
    private readonly PlaybackButton _playPauseButton;
    private readonly Button _skipBackButton;
    private readonly Button _skipForwardButton;
    private readonly Button _captureButton;
    private readonly Button _donateButton;
    private readonly Label _volumeLabel;
    private readonly PlayerSlider _volumeBar;
    private readonly Label _volumeValueLabel;
    private readonly PlayerSlider _seekBar;
    private readonly Label _timeLabel;
    private readonly ContextMenuStrip _videoMenu;
    private readonly ToolStripMenuItem _fullScreenMenuItem;
    private readonly ToolStripMenuItem _windowModeMenuItem;
    private readonly ToolStripMenuItem _effectMenuItem;
    private readonly ToolStripMenuItem _videoTransformMenuItem;
    private readonly ToolStripMenuItem _rotateNormalMenuItem;
    private readonly ToolStripMenuItem _rotate90MenuItem;
    private readonly ToolStripMenuItem _rotate180MenuItem;
    private readonly ToolStripMenuItem _rotate270MenuItem;
    private readonly ToolStripMenuItem _flipHorizontalMenuItem;
    private readonly ToolStripMenuItem _resetZoomMenuItem;
    private readonly ToolStripMenuItem _playlistMenuItem;
    private readonly ToolStripMenuItem _captureSettingsMenuItem;
    private readonly ToolStripMenuItem _skipSettingsMenuItem;
    private readonly ToolStripMenuItem _languageMenuItem;
    private readonly ToolStripMenuItem _koreanMenuItem;
    private readonly ToolStripMenuItem _englishMenuItem;
    private readonly System.Windows.Forms.Timer _uiTimer;
    private readonly System.Windows.Forms.Timer _seekCommitTimer;
    private readonly object _playerSync = new();
    private readonly object _seekSync = new();
    private readonly AppSettings _settings;
    private readonly List<string> _playlist = [];
    private readonly PlaybackRequestGate _playbackRequestGate = new();

    private LibVLC? _libVlc;
    private MediaPlayer? _mediaPlayer;
    private Task<(LibVLC libVlc, MediaPlayer mediaPlayer)>? _mediaPlayerInitializationTask;
    private string? _currentFile;
    private bool _isSeeking;
    private bool _isFullScreen;
    private bool _useKorean;
    private bool _startupUpdateQueued;
    private bool _playbackEnded;
    private long _pendingSeekTime = -1;
    private long _requestedSeekTime = -1;
    private long _requestedSeekLength;
    private MediaPlayer? _requestedSeekPlayer;
    private bool _seekWorkerActive;
    private bool _isPanningVideo;
    private bool _needsVideoSurfaceReconnect;
    private DateTime _suppressStateRefreshUntil = DateTime.MinValue;
    private float _videoScale;
    private Point _videoPanOffset;
    private Point _panStartMouse;
    private Point _panStartOffset;
    private Size _lastVideoViewSize;
    private VideoTransformMode _videoTransform;
    private int _desiredVolume = 100;
    private int _skipSeconds;
    private int _playlistIndex = -1;
    private Rectangle _windowModeBounds;
    private FormBorderStyle _windowModeBorderStyle;
    private FormWindowState _windowModeState;
    private VideoControlForm? _videoControlForm;
    private SkipSettingsForm? _skipSettingsForm;
    private ScreenCaptureSettingsForm? _captureSettingsForm;
    private PlaylistForm? _playlistForm;
    private ScreenCaptureOptions _captureOptions;

    public Form1(string? startupFile = null)
    {
        InitializeComponent();
        _settings = AppSettings.Load();
        _useKorean = _settings.UseKorean;
        _skipSeconds = _settings.SkipSeconds;
        _desiredVolume = Math.Clamp(_settings.Volume, 0, 100);
        _captureOptions = _settings.CaptureOptions;

        _videoHost = new VideoHostPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Black
        };

        _videoView = new VideoView
        {
            Bounds = Rectangle.Empty,
            BackColor = Color.Black
        };

        _bottomPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 78,
            Padding = new Padding(10, 8, 10, 8),
            BackColor = Color.FromArgb(22, 22, 22)
        };

        _openButton = CreateButton("Open", 72);
        _playPauseButton = new PlaybackButton();
        _skipBackButton = CreateButton("<<", 48);
        _skipForwardButton = CreateButton(">>", 48);
        _captureButton = CreateButton("Capture", 80);
        _donateButton = CreateDonateButton();

        _volumeLabel = CreateLabel("Vol", ContentAlignment.MiddleRight);
        _volumeBar = CreateSlider(0, 100, _desiredVolume);
        _volumeValueLabel = CreateLabel($"{_desiredVolume}%", ContentAlignment.MiddleRight);
        _seekBar = CreateSlider(0, SeekMaximum, 0);
        _seekBar.Enabled = false;
        _timeLabel = CreateLabel("00:00 / 00:00", ContentAlignment.MiddleRight);

        _fullScreenMenuItem = new ToolStripMenuItem("Fullscreen");
        _windowModeMenuItem = new ToolStripMenuItem("Window mode");
        _effectMenuItem = new ToolStripMenuItem("Effect");
        _videoTransformMenuItem = new ToolStripMenuItem("Video Transform");
        _rotateNormalMenuItem = new ToolStripMenuItem("Normal");
        _rotate90MenuItem = new ToolStripMenuItem("Rotate 90");
        _rotate180MenuItem = new ToolStripMenuItem("Rotate 180");
        _rotate270MenuItem = new ToolStripMenuItem("Rotate 270");
        _flipHorizontalMenuItem = new ToolStripMenuItem("Flip Horizontal");
        _resetZoomMenuItem = new ToolStripMenuItem("Reset Zoom");
        _playlistMenuItem = new ToolStripMenuItem("Playlist");
        _videoTransformMenuItem.DropDownItems.AddRange(new ToolStripItem[]
        {
            _rotateNormalMenuItem,
            _rotate90MenuItem,
            _rotate180MenuItem,
            _rotate270MenuItem,
            new ToolStripSeparator(),
            _flipHorizontalMenuItem
        });
        _captureSettingsMenuItem = new ToolStripMenuItem("Screen Capture");
        _skipSettingsMenuItem = new ToolStripMenuItem("Skip seconds");
        _languageMenuItem = new ToolStripMenuItem("Language");
        _koreanMenuItem = new ToolStripMenuItem("한국어");
        _englishMenuItem = new ToolStripMenuItem("English");
        _languageMenuItem.DropDownItems.AddRange(new ToolStripItem[]
        {
            _koreanMenuItem,
            _englishMenuItem
        });

        _videoMenu = new ContextMenuStrip();
        _videoMenu.Items.AddRange(new ToolStripItem[]
        {
            _fullScreenMenuItem,
            _windowModeMenuItem,
            new ToolStripSeparator(),
            _effectMenuItem,
            _videoTransformMenuItem,
            _resetZoomMenuItem,
            _playlistMenuItem,
            _captureSettingsMenuItem,
            _skipSettingsMenuItem,
            _languageMenuItem
        });

        BuildLayout();
        WireEvents();
        ApplyLanguage();

        _uiTimer = new System.Windows.Forms.Timer { Interval = 250 };
        _uiTimer.Tick += (_, _) => RefreshPlayerState();
        _uiTimer.Start();

        _seekCommitTimer = new System.Windows.Forms.Timer { Interval = 120 };
        _seekCommitTimer.Tick += (_, _) => CommitPendingSeek();

        Shown += async (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(startupFile))
            {
                await PlayFilesAsync([startupFile]);
            }

            await QueueStartupUpdateCheckAsync();
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _uiTimer.Dispose();
            _seekCommitTimer.Dispose();
            _videoControlForm?.Dispose();
            _skipSettingsForm?.Dispose();
            _captureSettingsForm?.Dispose();
            _playlistForm?.Dispose();
            _videoMenu.Dispose();
            _mediaPlayer?.Dispose();
            _libVlc?.Dispose();
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (IsZoomInKey(keyData))
        {
            AdjustVideoScale(0.1f);
            return true;
        }

        if (IsZoomOutKey(keyData))
        {
            AdjustVideoScale(-0.1f);
            return true;
        }

        switch (keyData)
        {
            case Keys.Left:
                Skip(-GetSkipMilliseconds());
                return true;
            case Keys.Right:
                Skip(GetSkipMilliseconds());
                return true;
            case Keys.Space:
                _ = TogglePlaybackAsync();
                return true;
            case Keys.Escape when _isFullScreen:
                ExitFullScreen();
                return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private static Button CreateButton(string text, int width)
    {
        return new Button
        {
            Text = text,
            Width = width,
            Height = 32,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(46, 46, 46),
            Margin = new Padding(0, 0, 8, 0),
            UseVisualStyleBackColor = false
        };
    }

    private static Button CreateDonateButton()
    {
        return new Button
        {
            Text = "Donate",
            Width = 76,
            Height = 32,
            Dock = DockStyle.Right,
            FlatStyle = FlatStyle.Flat,
            FlatAppearance =
            {
                BorderColor = Color.White,
                BorderSize = 1
            },
            ForeColor = Color.Black,
            BackColor = Color.FromArgb(255, 150, 46),
            Margin = new Padding(0, 0, 0, 0),
            UseVisualStyleBackColor = false
        };
    }

    private static Label CreateLabel(string text, ContentAlignment alignment)
    {
        return new Label
        {
            Text = text,
            AutoSize = false,
            ForeColor = Color.FromArgb(210, 210, 210),
            TextAlign = alignment,
            Dock = DockStyle.Fill
        };
    }

    private static PlayerSlider CreateSlider(int minimum, int maximum, int value)
    {
        return new PlayerSlider
        {
            Minimum = minimum,
            Maximum = maximum,
            Value = value,
            Height = 26,
            Dock = DockStyle.Fill,
            ProgressColor = Color.FromArgb(133, 199, 255),
            TrackColor = Color.FromArgb(58, 58, 58)
        };
    }

    private void BuildLayout()
    {
        SuspendLayout();

        Text = GetWindowTitle();
        SetWindowIcon();
        MinimumSize = new Size(760, 420);
        ClientSize = new Size(1060, 640);
        BackColor = Color.Black;
        KeyPreview = true;
        AllowDrop = true;
        _videoHost.AllowDrop = true;
        _videoView.AllowDrop = true;
        _bottomPanel.AllowDrop = true;

        var controlsRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 10,
            RowCount = 2,
            BackColor = Color.Transparent
        };

        controlsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        controlsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56));
        controlsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56));
        controlsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56));
        controlsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
        controlsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        controlsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44));
        controlsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132));
        controlsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48));
        controlsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 148));
        controlsRow.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        controlsRow.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

        controlsRow.Controls.Add(_openButton, 0, 0);
        controlsRow.Controls.Add(_playPauseButton, 1, 0);
        controlsRow.Controls.Add(_skipBackButton, 2, 0);
        controlsRow.Controls.Add(_skipForwardButton, 3, 0);
        controlsRow.Controls.Add(_captureButton, 4, 0);
        controlsRow.Controls.Add(_volumeLabel, 6, 0);
        controlsRow.Controls.Add(_volumeBar, 7, 0);
        controlsRow.Controls.Add(_volumeValueLabel, 8, 0);
        controlsRow.Controls.Add(_donateButton, 9, 0);
        controlsRow.Controls.Add(_seekBar, 0, 1);
        controlsRow.Controls.Add(_timeLabel, 9, 1);
        controlsRow.SetColumnSpan(_seekBar, 9);

        _bottomPanel.Controls.Add(controlsRow);
        ContextMenuStrip = _videoMenu;
        _videoHost.ContextMenuStrip = _videoMenu;
        _videoView.ContextMenuStrip = _videoMenu;
        _videoHost.Controls.Add(_videoView);
        Controls.Add(_videoHost);
        Controls.Add(_bottomPanel);

        ResumeLayout();
        ApplyVideoScale();
    }

    private void WireEvents()
    {
        _openButton.Click += async (_, _) => await OpenVideoAsync();
        _playPauseButton.Click += async (_, _) => await TogglePlaybackAsync();
        _skipBackButton.Click += (_, _) => Skip(-GetSkipMilliseconds());
        _skipForwardButton.Click += (_, _) => Skip(GetSkipMilliseconds());
        _captureButton.Click += (_, _) => CaptureCurrentFrame();
        _donateButton.Click += (_, _) => OpenDonateLink();
        _fullScreenMenuItem.Click += (_, _) => EnterFullScreen();
        _windowModeMenuItem.Click += (_, _) => ExitFullScreen();
        _effectMenuItem.Click += async (_, _) => await ShowVideoControlWindowAsync();
        _rotateNormalMenuItem.Click += async (_, _) => await SetVideoTransformAsync(VideoTransformMode.None);
        _rotate90MenuItem.Click += async (_, _) => await SetVideoTransformAsync(VideoTransformMode.Rotate90);
        _rotate180MenuItem.Click += async (_, _) => await SetVideoTransformAsync(VideoTransformMode.Rotate180);
        _rotate270MenuItem.Click += async (_, _) => await SetVideoTransformAsync(VideoTransformMode.Rotate270);
        _flipHorizontalMenuItem.Click += async (_, _) => await SetVideoTransformAsync(VideoTransformMode.FlipHorizontal);
        _resetZoomMenuItem.Click += (_, _) => ResetVideoScale();
        _playlistMenuItem.Click += (_, _) => ShowPlaylistWindow();
        _captureSettingsMenuItem.Click += (_, _) => ShowCaptureSettingsWindow();
        _skipSettingsMenuItem.Click += (_, _) => ShowSkipSettingsWindow();
        _koreanMenuItem.Click += (_, _) => SetLanguage(true);
        _englishMenuItem.Click += (_, _) => SetLanguage(false);
        _videoMenu.Opening += (_, _) => UpdateVideoMenu();
        _videoHost.Resize += (_, _) => ApplyVideoScale();

        _seekBar.DragStarted += (_, _) => _isSeeking = true;
        _seekBar.UserValueChanged += (_, _) =>
        {
            UpdateTimeLabelFromTrackBar();
            if (!_seekBar.IsDragging)
            {
                SeekToTrackBarPosition();
            }
        };
        _seekBar.DragCompleted += (_, _) =>
        {
            SeekToTrackBarPosition();
            _isSeeking = false;
        };

        _volumeBar.UserValueChanged += (_, _) => ApplyVolumeFromMainBar();

        _videoView.MouseDown += HandleVideoPanMouseDown;
        _videoView.MouseMove += HandleVideoPanMouseMove;
        _videoView.MouseUp += HandleVideoViewMouseUp;
        _videoHost.MouseDown += HandleVideoPanMouseDown;
        _videoHost.MouseMove += HandleVideoPanMouseMove;
        _videoHost.MouseUp += HandleVideoHostMouseUp;
        _videoHost.DragEnter += HandleVideoDragEnter;
        _videoHost.DragDrop += HandleVideoDragDrop;
        _videoView.DragEnter += HandleVideoDragEnter;
        _videoView.DragDrop += HandleVideoDragDrop;

        _bottomPanel.DragEnter += HandleVideoDragEnter;
        _bottomPanel.DragDrop += HandleVideoDragDrop;

        MouseUp += (_, e) =>
        {
            if (e.Button == MouseButtons.Right)
            {
                _videoMenu.Show(this, e.Location);
            }
        };
        DragEnter += HandleVideoDragEnter;
        DragDrop += HandleVideoDragDrop;

        KeyDown += Form1_KeyDown;
    }

    private async Task<MediaPlayer> EnsureMediaPlayerAsync()
    {
        if (_mediaPlayer is not null)
        {
            return _mediaPlayer;
        }

        Task<(LibVLC libVlc, MediaPlayer mediaPlayer)> initializationTask;
        lock (_playerSync)
        {
            if (_mediaPlayer is not null)
            {
                return _mediaPlayer;
            }

            _mediaPlayerInitializationTask ??= Task.Run(() =>
                CreateMediaPlayerBundle(_desiredVolume, _videoTransform));
            initializationTask = _mediaPlayerInitializationTask;
        }

        try
        {
            var bundle = await initializationTask;
            if (IsDisposed)
            {
                DisposeMediaPlayerBundle(bundle);
                throw new ObjectDisposedException(nameof(Form1));
            }

            if (_mediaPlayer is null)
            {
                AttachMediaPlayer(bundle);
            }
            else
            {
                DisposeMediaPlayerBundle(bundle);
            }

            return _mediaPlayer!;
        }
        catch
        {
            lock (_playerSync)
            {
                if (ReferenceEquals(_mediaPlayerInitializationTask, initializationTask))
                {
                    _mediaPlayerInitializationTask = null;
                }
            }

            throw;
        }
    }

    private (LibVLC libVlc, MediaPlayer mediaPlayer) CreateMediaPlayerBundle(int volume, VideoTransformMode transform)
    {
        Core.Initialize();
        var options = new List<string>
        {
            "--no-video-title-show",
            "--quiet",
            "--avcodec-hw=any",
            "--input-fast-seek",
            "--no-snapshot-preview"
        };

        var transformType = GetTransformType(transform);
        if (transformType is not null)
        {
            options.Add("--video-filter=transform");
            options.Add($"--transform-type={transformType}");
        }

        var libVlc = new LibVLC(options.ToArray());
        var mediaPlayer = new MediaPlayer(libVlc)
        {
            EnableMouseInput = false,
            EnableKeyInput = false,
            Volume = volume
        };
        mediaPlayer.EndReached += (_, _) => QueuePlaybackEnded();
        return (libVlc, mediaPlayer);
    }

    private void AttachMediaPlayer((LibVLC libVlc, MediaPlayer mediaPlayer) bundle)
    {
        _libVlc = bundle.libVlc;
        _mediaPlayer = bundle.mediaPlayer;
        _videoView.MediaPlayer = _mediaPlayer;
        ApplyDesiredVolumeToPlayer();
        ApplyVideoScale();
    }

    private static void DisposeMediaPlayerBundle((LibVLC libVlc, MediaPlayer mediaPlayer) bundle)
    {
        bundle.mediaPlayer.Dispose();
        bundle.libVlc.Dispose();
    }

    private async Task OpenVideoAsync()
    {
        using var dialog = new OpenFileDialog
        {
            Title = _useKorean ? "열기" : "Open",
            Filter = VideoFileTypes.CreateOpenDialogFilter(
                _useKorean ? "동영상 파일" : "Video files",
                _useKorean ? "모든 파일" : "All files"),
            CheckFileExists = true,
            Multiselect = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            await PlayFilesAsync(dialog.FileNames);
        }
    }

    private void HandleVideoDragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = TryGetDroppedFiles(e.Data, out _)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private async void HandleVideoDragDrop(object? sender, DragEventArgs e)
    {
        if (TryGetDroppedFiles(e.Data, out var filePaths))
        {
            await PlayFilesAsync(filePaths);
        }
    }

    private static bool TryGetDroppedFiles(IDataObject? data, out string[] filePaths)
    {
        filePaths = [];
        if (data is null || !data.GetDataPresent(DataFormats.FileDrop))
        {
            return false;
        }

        var files = data.GetData(DataFormats.FileDrop) as string[];
        filePaths = NormalizePlaylistFiles(files ?? []);
        return filePaths.Length > 0;
    }

    public async Task OpenExternalFileAsync(string filePath)
    {
        if (IsDisposed || string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return;
        }

        BringAppToFront();
        await PlayFilesAsync([filePath]);
        BringAppToFront();
    }

    public void ShowMainWindow()
    {
        if (IsDisposed)
        {
            return;
        }

        BringAppToFront();
    }

    private void BringAppToFront()
    {
        var wasHidden = !Visible || WindowState == FormWindowState.Minimized || Opacity < 1 || !ShowInTaskbar;
        if (WindowState == FormWindowState.Minimized)
        {
            WindowState = FormWindowState.Normal;
        }

        if (Opacity < 1)
        {
            Opacity = 1;
        }

        if (!ShowInTaskbar)
        {
            ShowInTaskbar = true;
        }

        Show();
        ShowWindow(Handle, ShowNormal);
        BringToFront();
        Activate();
        SetForegroundWindow(Handle);
        PerformLayout();
        _videoHost.PerformLayout();
        ApplyVideoScale();

        if (wasHidden)
        {
            _needsVideoSurfaceReconnect = true;
        }

        if (!_isFullScreen)
        {
            TopMost = true;
            TopMost = false;
        }
    }

    private async Task PlayFilesAsync(IEnumerable<string> filePaths, int startIndex = 0)
    {
        var files = NormalizePlaylistFiles(filePaths);
        if (files.Length == 0)
        {
            return;
        }

        await _playbackRequestGate.RunAsync(async () =>
        {
            BringAppToFront();
            _playlist.Clear();
            _playlist.AddRange(files);
            _playlistIndex = Math.Clamp(startIndex, 0, _playlist.Count - 1);
            UpdatePlaylistWindow();
            await PlayPlaylistIndexCoreAsync(_playlistIndex);
        });
    }

    private async Task PlayPlaylistIndexAsync(int index)
    {
        await _playbackRequestGate.RunAsync(() => PlayPlaylistIndexCoreAsync(index));
    }

    private async Task PlayPlaylistIndexCoreAsync(int index)
    {
        if (index < 0 || index >= _playlist.Count)
        {
            return;
        }

        _playlistIndex = index;
        UpdatePlaylistWindow();
        await PlayFileCoreAsync(_playlist[_playlistIndex]);
    }

    private async Task PlayNextPlaylistItemAsync()
    {
        var nextIndex = _playlistIndex + 1;
        if (nextIndex < 0 || nextIndex >= _playlist.Count)
        {
            return;
        }

        await PlayPlaylistIndexAsync(nextIndex);
    }

    private static string[] NormalizePlaylistFiles(IEnumerable<string> filePaths)
    {
        return filePaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path.Trim('"'))
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private Task PlayFileAsync(string filePath, long startTime = 0, bool shouldPlay = true)
    {
        return _playbackRequestGate.RunAsync(() => PlayFileCoreAsync(filePath, startTime, shouldPlay));
    }

    private async Task PlayFileCoreAsync(string filePath, long startTime = 0, bool shouldPlay = true)
    {
        UseWaitCursor = true;
        try
        {
            var player = await EnsureMediaPlayerAsync();
            EnsureVideoSurfaceReady(player);
            player.Stop();
            ApplyDesiredVolumeToPlayer();
            _currentFile = filePath;
            _playbackEnded = false;

            var started = false;
            for (var attempt = 0; attempt < 2 && !started; attempt++)
            {
                using var media = new Media(_libVlc!, new Uri(filePath));
                media.AddOption(":input-fast-seek");
                started = player.Play(media);
                if (!started)
                {
                    await Task.Delay(150);
                }
            }

            if (!started)
            {
                throw new InvalidOperationException("VLC could not start the selected media.");
            }

            ApplyDesiredVolumeToPlayer();
            player.EnableMouseInput = false;
            player.EnableKeyInput = false;

            if (startTime > 0)
            {
                await Task.Delay(150);
                var maxTime = player.Length > 0 ? player.Length : startTime;
                SeekToTime(Math.Clamp(startTime, 0, maxTime));
            }

            if (!shouldPlay)
            {
                player.Pause();
            }

            _seekBar.Enabled = true;
            _playPauseButton.IsPlaying = shouldPlay;
            ApplyVideoScale();
            Text = GetWindowTitle(filePath);
        }
        catch (Exception ex)
        {
            LogPlaybackFailure(filePath, ex);
            _currentFile = null;
            _playbackEnded = true;
            _seekBar.Enabled = false;
            _playPauseButton.IsPlaying = false;
            _timeLabel.Text = "00:00 / 00:00";
            Text = GetWindowTitle();

            if (!IsDisposed && !Disposing)
            {
                ShowInfo(
                    _useKorean
                        ? $"동영상을 재생할 수 없습니다.\n{ex.Message}"
                        : $"Could not play the video.\n{ex.Message}",
                    AppTitle);
            }
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void EnsureVideoSurfaceReady(MediaPlayer player)
    {
        if (!_videoView.IsHandleCreated)
        {
            _ = _videoView.Handle;
        }

        if (_videoHost.ClientSize.Width > 0 && _videoHost.ClientSize.Height > 0)
        {
            ApplyVideoScale();
        }

        if (_videoView.Width <= 0 || _videoView.Height <= 0)
        {
            _videoView.Bounds = _videoHost.ClientRectangle;
        }

        if (_needsVideoSurfaceReconnect || !ReferenceEquals(_videoView.MediaPlayer, player))
        {
            _videoView.MediaPlayer = null;
            _videoView.MediaPlayer = player;
            _needsVideoSurfaceReconnect = false;
        }
    }

    private async Task TogglePlaybackAsync()
    {
        if (_currentFile is null)
        {
            await OpenVideoAsync();
            return;
        }

        var player = await EnsureMediaPlayerAsync();
        if (_playbackEnded)
        {
            await PlayFileAsync(_currentFile);
            return;
        }

        if (player.IsPlaying)
        {
            player.Pause();
            _playPauseButton.IsPlaying = false;
            return;
        }

        player.Play();
        _playPauseButton.IsPlaying = true;
    }

    private async Task SetVideoTransformAsync(VideoTransformMode transform)
    {
        if (_videoTransform == transform)
        {
            return;
        }

        _videoTransform = transform;
        UpdateVideoMenu();

        if (_mediaPlayer is null)
        {
            return;
        }

        var filePath = _currentFile;
        var resumeTime = _mediaPlayer.Time;
        var shouldPlay = _mediaPlayer.IsPlaying;
        await RecreateMediaPlayerForTransformAsync();

        if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
        {
            await PlayFileAsync(filePath, resumeTime, shouldPlay);
        }
    }

    private async Task RecreateMediaPlayerForTransformAsync()
    {
        var oldMediaPlayer = _mediaPlayer;
        var oldLibVlc = _libVlc;

        _videoControlForm?.Dispose();
        _videoControlForm = null;
        _videoView.MediaPlayer = null;
        _mediaPlayer = null;
        _libVlc = null;

        try
        {
            oldMediaPlayer?.Stop();
        }
        catch
        {
            // Ignore VLC shutdown failures while switching video filters.
        }

        oldMediaPlayer?.Dispose();
        oldLibVlc?.Dispose();

        var bundle = await Task.Run(() => CreateMediaPlayerBundle(_desiredVolume, _videoTransform));
        if (IsDisposed)
        {
            DisposeMediaPlayerBundle(bundle);
            return;
        }

        AttachMediaPlayer(bundle);
    }

    private void AdjustVideoScale(float delta)
    {
        var oldScale = GetVideoDisplayScale();
        var newScale = Math.Clamp((float)Math.Round(oldScale + delta, 2), 1.0f, 3.0f);
        SetVideoScale(newScale, preserveCenter: true);
    }

    private void ResetVideoScale()
    {
        _videoScale = 1.0f;
        _videoPanOffset = Point.Empty;
        ApplyVideoScale();
    }

    private void ApplyVideoScale()
    {
        if (_videoHost.ClientSize.Width <= 0 || _videoHost.ClientSize.Height <= 0)
        {
            return;
        }

        var scale = GetVideoDisplayScale();
        var nextSize = GetVirtualVideoSize(scale);

        _videoPanOffset = ClampVideoPanOffset(_videoPanOffset, nextSize);
        if (_videoView.Dock != DockStyle.None)
        {
            _videoView.Dock = DockStyle.None;
        }

        SetVideoViewBounds(_videoPanOffset, nextSize);
        _videoView.Cursor = scale > 1.0f ? Cursors.SizeAll : Cursors.Default;
        _videoHost.Cursor = scale > 1.0f ? Cursors.SizeAll : Cursors.Default;

        if (_mediaPlayer is null)
        {
            return;
        }

        if (Math.Abs(_mediaPlayer.Scale) > 0.001f)
        {
            _mediaPlayer.Scale = 0;
        }

        ClearVideoCrop(_mediaPlayer);
    }

    private void SetVideoScale(float newScale, bool preserveCenter)
    {
        var oldScale = GetVideoDisplayScale();
        var hostSize = _videoHost.ClientSize;
        var contentCenterX = hostSize.Width > 0 && oldScale > 0
            ? (hostSize.Width / 2.0 - _videoPanOffset.X) / oldScale
            : 0;
        var contentCenterY = hostSize.Height > 0 && oldScale > 0
            ? (hostSize.Height / 2.0 - _videoPanOffset.Y) / oldScale
            : 0;

        _videoScale = newScale;

        if (!preserveCenter || newScale <= 1.0f)
        {
            _videoPanOffset = Point.Empty;
        }
        else if (hostSize.Width > 0 && hostSize.Height > 0)
        {
            _videoPanOffset = new Point(
                (int)Math.Round(hostSize.Width / 2.0 - contentCenterX * newScale),
                (int)Math.Round(hostSize.Height / 2.0 - contentCenterY * newScale));
        }

        ApplyVideoScale();
    }

    private float GetVideoDisplayScale()
    {
        return _videoScale <= 0 ? 1.0f : _videoScale;
    }

    private Size GetVirtualVideoSize(float scale)
    {
        return new Size(
            Math.Max(1, (int)Math.Round(_videoHost.ClientSize.Width * scale)),
            Math.Max(1, (int)Math.Round(_videoHost.ClientSize.Height * scale)));
    }

    private Point ClampVideoPanOffset(Point offset, Size videoSize)
    {
        var hostSize = _videoHost.ClientSize;
        var x = videoSize.Width <= hostSize.Width
            ? (hostSize.Width - videoSize.Width) / 2
            : Math.Clamp(offset.X, hostSize.Width - videoSize.Width, 0);
        var y = videoSize.Height <= hostSize.Height
            ? (hostSize.Height - videoSize.Height) / 2
            : Math.Clamp(offset.Y, hostSize.Height - videoSize.Height, 0);

        return new Point(x, y);
    }

    private void SetVideoViewBounds(Point location, Size size)
    {
        if (_videoView.Size == size && _videoView.Location == location)
        {
            return;
        }

        if (_videoView.Size != size)
        {
            _videoView.Bounds = new Rectangle(location, size);
            _lastVideoViewSize = size;
            return;
        }

        _videoView.Location = location;
    }

    private static void ClearVideoCrop(MediaPlayer player)
    {
        try
        {
            if (!string.IsNullOrEmpty(player.CropGeometry))
            {
                player.CropGeometry = string.Empty;
            }
        }
        catch
        {
            // Some video outputs may reject crop changes while switching media.
        }
    }

    private void HandleVideoPanMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || GetVideoDisplayScale() <= 1.0f)
        {
            return;
        }

        _isPanningVideo = true;
        _panStartMouse = Control.MousePosition;
        _panStartOffset = _videoPanOffset;
        _videoView.Capture = true;
        _videoHost.Capture = true;
    }

    private void HandleVideoPanMouseMove(object? sender, MouseEventArgs e)
    {
        if (!_isPanningVideo)
        {
            return;
        }

        var delta = new Size(
            Control.MousePosition.X - _panStartMouse.X,
            Control.MousePosition.Y - _panStartMouse.Y);
        var videoSize = _lastVideoViewSize.IsEmpty
            ? GetVirtualVideoSize(GetVideoDisplayScale())
            : _lastVideoViewSize;
        if (videoSize.Width <= 0 || videoSize.Height <= 0)
        {
            return;
        }

        _videoPanOffset = ClampVideoPanOffset(_panStartOffset + delta, videoSize);
        SetVideoViewBounds(_videoPanOffset, videoSize);
    }

    private void HandleVideoViewMouseUp(object? sender, MouseEventArgs e)
    {
        EndVideoPan();
        if (e.Button == MouseButtons.Right)
        {
            _videoMenu.Show(_videoView, e.Location);
        }
    }

    private void HandleVideoHostMouseUp(object? sender, MouseEventArgs e)
    {
        EndVideoPan();
        if (e.Button == MouseButtons.Right)
        {
            _videoMenu.Show(_videoHost, e.Location);
        }
    }

    private void EndVideoPan()
    {
        if (!_isPanningVideo)
        {
            return;
        }

        _isPanningVideo = false;
        _videoView.Capture = false;
        _videoHost.Capture = false;
    }

    private static bool IsZoomInKey(Keys keyData)
    {
        var keyCode = keyData & Keys.KeyCode;
        return keyCode == Keys.Add
            || keyCode == Keys.Oemplus
            || keyCode == Keys.OemSemicolon;
    }

    private static bool IsZoomOutKey(Keys keyData)
    {
        var keyCode = keyData & Keys.KeyCode;
        return keyCode == Keys.Subtract
            || keyCode == Keys.OemMinus;
    }

    private static string? GetTransformType(VideoTransformMode transform)
    {
        return transform switch
        {
            VideoTransformMode.Rotate90 => "90",
            VideoTransformMode.Rotate180 => "180",
            VideoTransformMode.Rotate270 => "270",
            VideoTransformMode.FlipHorizontal => "hflip",
            _ => null
        };
    }

    private int GetSkipMilliseconds()
    {
        return _skipSeconds * 1000;
    }

    private void Skip(int milliseconds)
    {
        var player = _mediaPlayer;
        var length = player?.Length ?? 0;
        if (length <= 0)
        {
            return;
        }

        var baseTime = _pendingSeekTime >= 0 ? _pendingSeekTime : player!.Time;
        QueueSeek(Math.Clamp(baseTime + milliseconds, 0, length), defer: true);
    }

    private void SeekToTrackBarPosition()
    {
        var player = _mediaPlayer;
        var length = player?.Length ?? 0;
        if (length <= 0)
        {
            return;
        }

        QueueSeek(length * _seekBar.Value / SeekMaximum, defer: false);
    }

    private void SeekToTime(long targetTime)
    {
        QueueSeek(targetTime, defer: false);
    }

    private void QueueSeek(long targetTime, bool defer)
    {
        var player = _mediaPlayer;
        var length = player?.Length ?? 0;
        if (player is null || length <= 0)
        {
            return;
        }

        var nextTime = Math.Clamp(targetTime, 0, length);
        _pendingSeekTime = nextTime;
        _suppressStateRefreshUntil = DateTime.UtcNow.AddMilliseconds(500);

        if (!_isSeeking)
        {
            _seekBar.Value = (int)Math.Clamp(nextTime * SeekMaximum / length, 0, SeekMaximum);
        }

        _timeLabel.Text = $"{FormatTime(nextTime)} / {FormatTime(length)}";

        if (defer)
        {
            _seekCommitTimer.Stop();
            _seekCommitTimer.Start();
            return;
        }

        CommitPendingSeek();
    }

    private void CommitPendingSeek()
    {
        _seekCommitTimer.Stop();

        var player = _mediaPlayer;
        var length = player?.Length ?? 0;
        if (player is null || length <= 0 || _pendingSeekTime < 0)
        {
            _pendingSeekTime = -1;
            return;
        }

        var nextTime = Math.Clamp(_pendingSeekTime, 0, length);
        _pendingSeekTime = -1;
        _suppressStateRefreshUntil = DateTime.UtcNow.AddMilliseconds(500);

        try
        {
            QueueBackgroundSeek(player, nextTime, length);
        }
        catch
        {
            // Ignore seek requests that race with media-player disposal.
        }
    }

    private void QueueBackgroundSeek(MediaPlayer player, long nextTime, long length)
    {
        lock (_seekSync)
        {
            _requestedSeekPlayer = player;
            _requestedSeekTime = nextTime;
            _requestedSeekLength = length;
            if (_seekWorkerActive)
            {
                return;
            }

            _seekWorkerActive = true;
        }

        _ = Task.Run(ProcessSeekRequests);
    }

    private void ProcessSeekRequests()
    {
        while (true)
        {
            MediaPlayer? player;
            long nextTime;
            long length;

            lock (_seekSync)
            {
                if (_requestedSeekPlayer is null || _requestedSeekTime < 0 || _requestedSeekLength <= 0)
                {
                    _seekWorkerActive = false;
                    return;
                }

                player = _requestedSeekPlayer;
                nextTime = _requestedSeekTime;
                length = _requestedSeekLength;
                _requestedSeekPlayer = null;
                _requestedSeekTime = -1;
                _requestedSeekLength = 0;
            }

            try
            {
                player.Position = (float)Math.Clamp(nextTime / (double)length, 0, 1);
            }
            catch
            {
                try
                {
                    player.Time = nextTime;
                }
                catch
                {
                    // The player may be disposed while loading another file.
                }
            }

            if (!IsDisposed && IsHandleCreated)
            {
                BeginInvoke(() => _suppressStateRefreshUntil = DateTime.UtcNow.AddMilliseconds(250));
            }
        }
    }

    private void UpdateTimeLabelFromTrackBar()
    {
        var length = _mediaPlayer?.Length ?? 0;
        if (length <= 0)
        {
            return;
        }

        var previewTime = length * _seekBar.Value / SeekMaximum;
        _timeLabel.Text = $"{FormatTime(previewTime)} / {FormatTime(length)}";
    }

    private void RefreshPlayerState()
    {
        var player = _mediaPlayer;
        if (player is null)
        {
            return;
        }

        if (DateTime.UtcNow < _suppressStateRefreshUntil)
        {
            return;
        }

        var length = player.Length;
        var time = Math.Clamp(player.Time, 0, Math.Max(length, 0));

        if (length > 0)
        {
            _seekBar.Enabled = true;
            if (!_isSeeking)
            {
                _seekBar.Value = (int)Math.Clamp(time * SeekMaximum / length, 0, SeekMaximum);
            }

            _timeLabel.Text = $"{FormatTime(time)} / {FormatTime(length)}";
        }
        else
        {
            _timeLabel.Text = "00:00 / 00:00";
        }

        if (_currentFile is not null)
        {
            _playPauseButton.IsPlaying = player.IsPlaying;
        }

    }

    private void QueuePlaybackEnded()
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        BeginInvoke(OnPlaybackEnded);
    }

    private void OnPlaybackEnded()
    {
        if (IsDisposed)
        {
            return;
        }

        if (_playlistIndex >= 0 && _playlistIndex + 1 < _playlist.Count)
        {
            _ = PlayNextPlaylistItemAsync();
            return;
        }

        _playbackEnded = true;
        _playPauseButton.IsPlaying = false;
        if (_seekBar.Enabled)
        {
            _seekBar.Value = SeekMaximum;
        }
    }

    private void Form1_KeyDown(object? sender, KeyEventArgs e)
    {
        if (IsZoomInKey(e.KeyData))
        {
            AdjustVideoScale(0.1f);
            e.Handled = true;
            return;
        }

        if (IsZoomOutKey(e.KeyData))
        {
            AdjustVideoScale(-0.1f);
            e.Handled = true;
            return;
        }

        switch (e.KeyCode)
        {
            case Keys.Space:
                _ = TogglePlaybackAsync();
                e.Handled = true;
                break;
            case Keys.Left:
                Skip(-GetSkipMilliseconds());
                e.Handled = true;
                break;
            case Keys.Right:
                Skip(GetSkipMilliseconds());
                e.Handled = true;
                break;
            case Keys.Escape when _isFullScreen:
                ExitFullScreen();
                e.Handled = true;
                break;
            case Keys.O when e.Control:
                _ = OpenVideoAsync();
                e.Handled = true;
                break;
        }
    }

    private void UpdateVideoMenu()
    {
        _fullScreenMenuItem.Enabled = !_isFullScreen;
        _windowModeMenuItem.Enabled = _isFullScreen;
        _fullScreenMenuItem.Checked = _isFullScreen;
        _windowModeMenuItem.Checked = !_isFullScreen;
        _skipSettingsMenuItem.Text = _useKorean
            ? $"건너뛰기 시간 ({_skipSeconds}초)"
            : $"Skip seconds ({_skipSeconds}s)";
        _rotateNormalMenuItem.Checked = _videoTransform == VideoTransformMode.None;
        _rotate90MenuItem.Checked = _videoTransform == VideoTransformMode.Rotate90;
        _rotate180MenuItem.Checked = _videoTransform == VideoTransformMode.Rotate180;
        _rotate270MenuItem.Checked = _videoTransform == VideoTransformMode.Rotate270;
        _flipHorizontalMenuItem.Checked = _videoTransform == VideoTransformMode.FlipHorizontal;
        _resetZoomMenuItem.Enabled = Math.Abs(GetVideoDisplayScale() - 1.0f) > 0.001f || _videoPanOffset != Point.Empty;
        _koreanMenuItem.Checked = _useKorean;
        _englishMenuItem.Checked = !_useKorean;
    }

    private void EnterFullScreen()
    {
        if (_isFullScreen)
        {
            return;
        }

        _windowModeBounds = Bounds;
        _windowModeBorderStyle = FormBorderStyle;
        _windowModeState = WindowState;

        _isFullScreen = true;
        _bottomPanel.Visible = false;
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Normal;
        Bounds = Screen.FromControl(this).Bounds;
        TopMost = true;
    }

    private void ExitFullScreen()
    {
        if (!_isFullScreen)
        {
            return;
        }

        _isFullScreen = false;
        TopMost = false;
        FormBorderStyle = _windowModeBorderStyle;
        WindowState = FormWindowState.Normal;
        Bounds = _windowModeBounds;
        WindowState = _windowModeState;
        _bottomPanel.Visible = true;
    }

    private async Task ShowVideoControlWindowAsync()
    {
        var player = await EnsureMediaPlayerAsync();
        if (_videoControlForm is { IsDisposed: false })
        {
            _videoControlForm.Show();
            _videoControlForm.Activate();
            return;
        }

        _videoControlForm = new VideoControlForm(player);
        _videoControlForm.Show(this);
    }

    private void ApplyVolumeFromMainBar()
    {
        _desiredVolume = _volumeBar.Value;
        if (_mediaPlayer is not null)
        {
            _mediaPlayer.Volume = _desiredVolume;
        }

        _volumeValueLabel.Text = $"{_desiredVolume}%";
        SaveSettings();
    }

    private void ApplyDesiredVolumeToPlayer()
    {
        _desiredVolume = Math.Clamp(_desiredVolume, _volumeBar.Minimum, _volumeBar.Maximum);
        if (_volumeBar.Value != _desiredVolume)
        {
            _volumeBar.Value = _desiredVolume;
        }

        if (_mediaPlayer is not null && _mediaPlayer.Volume != _desiredVolume)
        {
            _mediaPlayer.Volume = _desiredVolume;
        }

        _volumeValueLabel.Text = $"{_desiredVolume}%";
    }

    private void ShowSkipSettingsWindow()
    {
        if (_skipSettingsForm is { IsDisposed: false })
        {
            _skipSettingsForm.Show();
            _skipSettingsForm.Activate();
            return;
        }

        _skipSettingsForm = new SkipSettingsForm(_skipSeconds, value =>
        {
            _skipSeconds = value;
            SaveSettings();
            UpdateVideoMenu();
        });
        _skipSettingsForm.Show(this);
    }

    private void ShowCaptureSettingsWindow()
    {
        if (_captureSettingsForm is { IsDisposed: false })
        {
            _captureSettingsForm.Show();
            _captureSettingsForm.Activate();
            return;
        }

        _captureSettingsForm = new ScreenCaptureSettingsForm(
            _captureOptions,
            options =>
            {
                _captureOptions = options;
                SaveSettings();
            });
        _captureSettingsForm.Show(this);
    }

    private void ShowPlaylistWindow()
    {
        if (_playlistForm is { IsDisposed: false })
        {
            UpdatePlaylistWindow();
            _playlistForm.Show();
            _playlistForm.Activate();
            return;
        }

        _playlistForm = new PlaylistForm(
            _useKorean,
            () => _ = AddPlaylistFilesAsync(),
            index => _ = RemovePlaylistIndexAsync(index),
            MovePlaylistIndex,
            index => _ = PlayPlaylistIndexAsync(index));
        UpdatePlaylistWindow();
        _playlistForm.Show(this);
    }

    private async Task AddPlaylistFilesAsync()
    {
        using var dialog = new OpenFileDialog
        {
            Title = _useKorean ? "추가" : "Add",
            Filter = VideoFileTypes.CreateOpenDialogFilter(
                _useKorean ? "동영상 파일" : "Video files",
                _useKorean ? "모든 파일" : "All files"),
            CheckFileExists = true,
            Multiselect = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var wasEmpty = _playlist.Count == 0;
        AppendPlaylistFiles(dialog.FileNames);
        if (wasEmpty && _playlist.Count > 0)
        {
            await PlayPlaylistIndexAsync(0);
            return;
        }

        UpdatePlaylistWindow();
    }

    private void AppendPlaylistFiles(IEnumerable<string> filePaths)
    {
        foreach (var filePath in NormalizePlaylistFiles(filePaths))
        {
            if (!_playlist.Contains(filePath, StringComparer.OrdinalIgnoreCase))
            {
                _playlist.Add(filePath);
            }
        }
    }

    private async Task RemovePlaylistIndexAsync(int index)
    {
        if (index < 0 || index >= _playlist.Count)
        {
            return;
        }

        var removingCurrent = index == _playlistIndex;
        _playlist.RemoveAt(index);

        if (_playlist.Count == 0)
        {
            _playlistIndex = -1;
            UpdatePlaylistWindow();
            return;
        }

        if (index < _playlistIndex)
        {
            _playlistIndex--;
        }

        if (removingCurrent)
        {
            await PlayPlaylistIndexAsync(Math.Min(index, _playlist.Count - 1));
            return;
        }

        UpdatePlaylistWindow();
    }

    private void MovePlaylistIndex(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= _playlist.Count || toIndex < 0 || toIndex >= _playlist.Count)
        {
            return;
        }

        var item = _playlist[fromIndex];
        _playlist.RemoveAt(fromIndex);
        _playlist.Insert(toIndex, item);

        if (_playlistIndex == fromIndex)
        {
            _playlistIndex = toIndex;
        }
        else if (fromIndex < _playlistIndex && toIndex >= _playlistIndex)
        {
            _playlistIndex--;
        }
        else if (fromIndex > _playlistIndex && toIndex <= _playlistIndex)
        {
            _playlistIndex++;
        }

        UpdatePlaylistWindow();
    }

    private void UpdatePlaylistWindow()
    {
        if (_playlistForm is { IsDisposed: false })
        {
            _playlistForm.SetPlaylist(_playlist, _playlistIndex);
        }
    }

    private void CaptureCurrentFrame()
    {
        var player = _mediaPlayer;
        if (player is null || _currentFile is null)
        {
            ShowInfo(
                _useKorean ? "재생 중인 영상이 없습니다." : "No video is currently playing.",
                _useKorean ? "화면 캡쳐" : "Screen Capture");
            return;
        }

        Directory.CreateDirectory(_captureOptions.SaveFolder);
        var filePath = Path.Combine(
            _captureOptions.SaveFolder,
            $"capture_{DateTime.Now:yyyyMMdd_HHmmss}.{_captureOptions.Extension}");

        if (!SaveVisibleVideoFrame(filePath) && !player.TakeSnapshot(0, filePath, 0, 0))
        {
            ShowInfo(
                _useKorean ? "현재 프레임을 캡쳐하지 못했습니다." : "Could not capture the current frame.",
                _useKorean ? "화면 캡쳐" : "Screen Capture");
            return;
        }

        ShowInfo(
            _useKorean ? $"저장했습니다.\n{filePath}" : $"Saved.\n{filePath}",
            _useKorean ? "화면 캡쳐" : "Screen Capture");
    }

    private bool SaveVisibleVideoFrame(string filePath)
    {
        try
        {
            if (!_videoHost.Visible || _videoHost.Width <= 0 || _videoHost.Height <= 0)
            {
                return false;
            }

            using var bitmap = new Bitmap(_videoHost.Width, _videoHost.Height);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(
                    _videoHost.PointToScreen(Point.Empty),
                    Point.Empty,
                    _videoHost.ClientSize,
                    CopyPixelOperation.SourceCopy);
            }

            bitmap.Save(filePath, GetCaptureImageFormat(_captureOptions.Extension));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static ImageFormat GetCaptureImageFormat(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            "jpg" or "jpeg" => ImageFormat.Jpeg,
            "tiff" or "tif" => ImageFormat.Tiff,
            _ => ImageFormat.Png
        };
    }

    private void SetLanguage(bool useKorean)
    {
        _useKorean = useKorean;
        ApplyLanguage();
        SaveSettings();
    }

    private void SaveSettings()
    {
        _settings.UseKorean = _useKorean;
        _settings.SkipSeconds = _skipSeconds;
        _settings.Volume = _desiredVolume;
        _settings.CaptureOptions = _captureOptions;

        try
        {
            _settings.Save();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    private void ApplyLanguage()
    {
        _openButton.Text = _useKorean ? "열기" : "Open";
        _captureButton.Text = _useKorean ? "캡쳐" : "Capture";
        _donateButton.Text = _useKorean ? "후원" : "Donate";
        _volumeLabel.Text = _useKorean ? "볼륨" : "Vol";
        _fullScreenMenuItem.Text = _useKorean ? "전체화면" : "Fullscreen";
        _windowModeMenuItem.Text = _useKorean ? "창모드" : "Window mode";
        _effectMenuItem.Text = _useKorean ? "효과" : "Effect";
        _resetZoomMenuItem.Text = _useKorean ? "확대 초기화" : "Reset Zoom";
        _playlistMenuItem.Text = _useKorean ? "재생 목록" : "Playlist";
        _captureSettingsMenuItem.Text = _useKorean ? "화면 캡쳐" : "Screen Capture";
        _languageMenuItem.Text = _useKorean ? "언어" : "Language";
        UpdateVideoMenu();
    }

    private void OpenDonateLink()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = AppLinks.DonateUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ShowInfo(
                _useKorean ? $"후원 페이지를 열 수 없습니다.\n{ex.Message}" : $"Could not open the donation page.\n{ex.Message}",
                AppTitle);
        }
    }

    private async Task QueueStartupUpdateCheckAsync()
    {
        if (_startupUpdateQueued)
        {
            return;
        }

        _startupUpdateQueued = true;
        await Task.Delay(TimeSpan.FromSeconds(60));
        if (!IsDisposed)
        {
            await CheckForUpdatesOnStartupAsync();
        }
    }

    private async Task CheckForUpdatesOnStartupAsync()
    {
        var checkResult = await _appUpdateService.CheckForUpdatesAsync();
        if (checkResult.Status == AppUpdateStatus.UpdateAvailable)
        {
            await PromptAndInstallUpdateAsync(checkResult);
        }
    }

    private async Task PromptAndInstallUpdateAsync(AppUpdateResult updateResult)
    {
        if (updateResult.UpdateInfo is null)
        {
            return;
        }

        var targetVersion = updateResult.Version ?? "new";
        var message = _useKorean
            ? $"새 버전 v{targetVersion}을 사용할 수 있습니다.\n현재 버전: {AppVersion}\n\n지금 다운로드하고 설치할까요?"
            : $"Version v{targetVersion} is available.\nCurrent version: {AppVersion}\n\nDownload and install it now?";
        var title = _useKorean ? "업데이트 확인" : "Update Available";

        if (MessageBox.Show(this, message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Information) != DialogResult.Yes)
        {
            return;
        }

        UseWaitCursor = true;
        try
        {
            var downloadResult = await _appUpdateService.DownloadUpdateAsync(updateResult.UpdateInfo);
            if (downloadResult.Status != AppUpdateStatus.Downloaded)
            {
                ShowInfo(
                    _useKorean ? $"업데이트 다운로드에 실패했습니다.\n{downloadResult.Message}" : $"Failed to download the update.\n{downloadResult.Message}",
                    title);
                return;
            }

            var applyResult = _appUpdateService.ApplyDownloadedUpdateAndRestart();
            if (applyResult.Status == AppUpdateStatus.Installing)
            {
                Application.Exit();
                return;
            }

            ShowInfo(
                _useKorean ? $"업데이트 적용에 실패했습니다.\n{applyResult.Message}" : $"Failed to apply the update.\n{applyResult.Message}",
                title);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void SetWindowIcon()
    {
        var icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        if (icon is not null)
        {
            Icon = icon;
        }
    }

    private void ShowInfo(string message, string title)
    {
        MessageBox.Show(this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static void LogPlaybackFailure(string filePath, Exception exception)
    {
        try
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Tech Archive",
                "Video Player");
            Directory.CreateDirectory(directory);
            File.AppendAllText(
                Path.Combine(directory, "playback.log"),
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {filePath}{Environment.NewLine}{exception}{Environment.NewLine}");
        }
        catch
        {
            // Diagnostics must never prevent the player from closing.
        }
    }

    private static string GetWindowTitle(string? filePath = null)
    {
        var title = $"{AppTitle} v{AppVersion}";
        return string.IsNullOrWhiteSpace(filePath)
            ? title
            : $"{title} - {Path.GetFileName(filePath)}";
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private static string FormatTime(long milliseconds)
    {
        var time = TimeSpan.FromMilliseconds(Math.Max(milliseconds, 0));
        return time.TotalHours >= 1
            ? time.ToString(@"h\:mm\:ss")
            : time.ToString(@"mm\:ss");
    }
}

internal enum VideoTransformMode
{
    None,
    Rotate90,
    Rotate180,
    Rotate270,
    FlipHorizontal
}
