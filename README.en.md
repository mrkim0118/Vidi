# Vidi

[한국어](README.md) | **English**

A lightweight, fast video player for Windows. Drop a file on it and it plays.

Built on LibVLC, so it plays almost any video format without installing extra codecs.

## Features

- **Drag and drop**: Drop a file on the window or double-click it in Explorer to start playing. Opening several files at once adds them all to the playlist.
- **Playlist**: Reorder, add and remove items.
- **Skip forward/back**: Choose how many seconds each skip jumps.
- **Frame capture**: Save the current frame at full resolution, to `Pictures\Vidi Captures` by default.
- **Video effects**: Rotate 90°/180°/270°, flip horizontally, zoom in/out, and adjust brightness and color.
- **Full screen** mode.
- **Korean / English** interface.
- **Automatic updates**: Vidi tells you when a new version is out and installs it in place.

## Keyboard shortcuts

| Key | Action |
|---|---|
| `Space` | Play / pause |
| `←` / `→` | Skip back / forward |
| `+` / `-` | Zoom in / out |
| `Ctrl+O` | Open file |
| `Esc` | Exit full screen |

## Supported formats

mp4, mkv, avi, mov, wmv, flv, webm, mpeg/mpg, m4v, 3gp, ts/mts/m2ts, ogv, vob, asf, rm/rmvb, divx, xvid, h264, h265/hevc

## Install

Download `Vidi-Setup-x.y.z.exe` from the [latest release](https://github.com/mrkim0118/Vidi/releases/latest) and run it. To use Vidi without installing, download `Vidi-win-Portable.zip` and extract it.

- Runs on 64-bit Windows 10 and 11.
- The .NET runtime is included, so nothing else needs to be installed.

## Build from source

Requires the .NET 8 SDK.

```bash
dotnet build Vidi.csproj
```

Tests:

```bash
dotnet run --project Vidi.Tests
```

Release package (Velopack):

```powershell
$env:DOTNET_ROLL_FORWARD = 'Major'  # the vpk tool needs .NET 9
powershell -ExecutionPolicy Bypass -File scripts\Publish-Velopack.ps1 -Version 1.0.0
```

Output goes to `artifacts\velopack\Releases`.

## Built with

- [LibVLCSharp](https://github.com/videolan/libvlcsharp) / [LibVLC](https://www.videolan.org/vlc/libvlc.html)
- [Velopack](https://velopack.io/)

## Author

Manggdy
