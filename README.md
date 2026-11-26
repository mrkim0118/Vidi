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

- `Vidi-Setup-x.y.z.exe`: installer
- `Vidi-win-Portable.zip`: no installation needed; unzip and run `Vidi.exe`

## Features

- **Drag and drop**: Drop a file on the window or double-click it in Explorer to start playing. Opening several files at once adds them all to the playlist.
- **Playlist**: Reorder, add and remove items.
- **Skip forward/back**: Choose how many seconds each skip jumps.
- **Frame capture**: Save the current frame at full resolution.
- **Video effects**: Rotate 90°/180°/270°, flip horizontally, zoom in/out, and adjust brightness and color.
- **Full screen**: Watch in full screen mode.
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
| `+` / `-` | Zoom in / out |
| `Ctrl+O` | Open file |
| `Esc` | Exit full screen |

### Supported formats

mp4, mkv, avi, mov, wmv, flv, webm, mpeg/mpg, m4v, 3gp, ts/mts/m2ts, ogv, vob, asf, rm/rmvb, divx, xvid, h264, h265/hevc

## Data & Settings

| Data | Location |
|---|---|
| Settings | `%AppData%\Vidi\settings.ini` |
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
$env:DOTNET_ROLL_FORWARD = 'Major'  # the vpk tool needs .NET 9
powershell -ExecutionPolicy Bypass -File scripts\Publish-Velopack.ps1 -Version 1.0.0
```

Output goes to `artifacts\velopack\Releases`.

## Project Layout

| Path | Contents |
|---|---|
| `Form1.cs`, `VideoControlForm.cs` | Main window and playback controls |
| `PlaylistForm.cs` | Playlist window |
| `AppSettings.cs` | Settings load and save |
| `GitHubUpdater.cs` | GitHub release update check |
| `FileAssociationService.cs` | Explorer file associations |
| `Vidi.Tests/` | Tests |
| `icons/` | App icons |
| `scripts/Publish-Velopack.ps1` | Release packaging |

## Libraries

- [LibVLCSharp](https://github.com/videolan/libvlcsharp) / [LibVLC](https://www.videolan.org/vlc/libvlc.html)
- [Velopack](https://velopack.io/)
