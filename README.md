<p align="center">
  <img src="icons/TASP_icon_256x256.png" width="128" alt="Vidi icon">
</p>

<h1 align="center">Vidi</h1>

<p align="center">A lightweight, fast video player for Windows. Drop a file on it and it plays.</p>

<p align="center">Author: <b>Manggdy</b></p>

<p align="center"><b>English</b> | <a href="README.ko.md">한국어</a></p>

Built on LibVLC, so it plays almost any video format without installing extra codecs.

## Download

Get the latest release from the [Releases](https://github.com/mrkim0118/Vidi/releases/latest) page:

- `Vidi-Setup-x.y.z.exe`: installer (installs to `C:\Program Files\Vidi` and adds Vidi to Explorer's "Open with" list)

## Features

- **Drag and drop**: Drop a file on the window or double-click it in Explorer to start playing. Opening several files at once adds them all to the playlist.
- **Playlist**: Reorder, add and remove items.
- **Skip forward/back**: Choose how many seconds each skip jumps.
- **Frame capture**: Save the current frame at full resolution.
- **Video effects**: Rotate 90°/180°/270°, flip horizontally, zoom in/out, and adjust brightness and color.
- **Full screen**: Double-click the video or press `F`. The controls appear when you move the mouse and hide again, along with the cursor, after a moment.
- **Resume playback**: Each file reopens where you left off. Finished files start from the beginning.
- **Recent files**: Reopen any of the last 10 files from the right-click menu.
- **Playback speed**: 0.5x to 2x.
- **Subtitles**: Subtitle files with the same name load automatically. Drop a subtitle file on the video or open one from the menu, and switch or turn off subtitle tracks.
- **Audio tracks**: Pick the audio track in videos that have several.
- **Repeat**: Repeat one file, repeat the playlist, or loop a section between two points (A-B).
- **Frame stepping**: Move one frame at a time with `,` and `.`.
- **Korean / English**: Switch the interface language.
- **Update check**: On launch, checks the latest GitHub release and asks whether to download and install the new version.

## Requirements

- Windows 10 or 11 (x64)
- The .NET runtime is included in the installer, so nothing else needs to be installed.

## Usage

### Keyboard shortcuts

| Key | Action |
|---|---|
| `Space` | Play / pause |
| `←` / `→` | Skip back / forward |
| `↑` / `↓`, mouse wheel | Volume up / down |
| `M` | Mute |
| `F`, `Enter`, double-click | Toggle full screen |
| `,` / `.` | Previous / next frame |
| `+` / `-` | Zoom in / out |
| `Ctrl+O` | Open file |
| `Esc` | Exit full screen |

### Supported formats

mp4, mkv, avi, mov, wmv, flv, webm, mpeg/mpg, m4v, 3gp, ts/mts/m2ts, ogv, vob, asf, rm/rmvb, divx, xvid, h264, h265/hevc

## Data & Settings

| Data | Location |
|---|---|
| Settings | `%AppData%\Vidi\settings.ini` |
| Recent files and resume positions | `%AppData%\Vidiesume.txt` |
| Frame captures | `Pictures\Vidi Captures` (default, changeable) |

## Build from Source

Requires the .NET 8 SDK.

### Build

```powershell
dotnet build Vidi.csproj
```

### Test

```powershell
dotnet run --project Vidi.Tests
```

### Release

```powershell
powershell -ExecutionPolicy Bypass -File scripts\Build-Installer.ps1
```

The script publishes the app (self-contained, without PDB files) to `artifacts\publish`, signs `Vidi.exe` with the `CN=Manggdy` certificate, and builds `artifacts\installer\Vidi-Setup-<version>.exe` with [Inno Setup 6](https://jrsoftware.org/isinfo.php). The version comes from `<Version>` in `Vidi.csproj`. Pass `-NoSign` to build without a certificate.

After installing, setup runs `Vidi.exe --register` as the signed-in user to add the "Open with" entry and the startup VLC warm-up; uninstall runs `Vidi.exe --unregister`.

## Project Layout

| Path | Contents |
|---|---|
| `Form1.cs`, `VideoControlForm.cs` | Main window and playback controls |
| `PlaylistForm.cs` | Playlist window |
| `AppSettings.cs` | Settings load and save |
| `ResumeStore.cs` | Recent files and resume positions |
| `DarkMenuRenderer.cs` | Dark right-click menu style |
| `GitHubUpdater.cs` | GitHub release update check |
| `FileAssociationService.cs` | Explorer file associations |
| `Vidi.Tests/` | Tests |
| `icons/` | App icons |
| `installer/Vidi.iss` | Inno Setup installer script |
| `scripts/Build-Installer.ps1` | Release build and installer packaging |

## Libraries

- [LibVLCSharp](https://github.com/videolan/libvlcsharp) / [LibVLC](https://www.videolan.org/vlc/libvlc.html)
- [Inno Setup](https://jrsoftware.org/isinfo.php): installer
