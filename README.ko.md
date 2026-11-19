<p align="center">
  <img src="icons/TASP_icon_256x256.png" width="128" alt="Vidi 아이콘">
</p>

<h1 align="center">Vidi</h1>

<p align="center">가볍고 빠른 Windows용 동영상 플레이어입니다. 파일을 끌어다 놓으면 바로 재생됩니다.</p>

<p align="center">Author: <b>Manggdy</b></p>

<p align="center"><a href="README.md">English</a> | <b>한국어</b></p>

LibVLC 기반이라 거의 모든 동영상 형식을 별도 코덱 설치 없이 재생합니다.

## 다운로드

[Releases](https://github.com/mrkim0118/Vidi/releases/latest) 페이지에서 최신 버전을 받습니다.

- `Vidi-Setup-x.y.z.exe`: 설치 파일
- `Vidi-win-Portable.zip`: 설치 없이 사용. 압축을 풀고 `Vidi.exe`를 실행합니다.

## 주요 기능

- **끌어다 놓기 재생**: 파일을 창에 놓거나 탐색기에서 더블클릭하면 바로 재생합니다. 여러 파일을 한 번에 열면 모두 재생 목록에 들어갑니다.
- **재생 목록**: 순서 바꾸기, 추가, 삭제를 지원합니다.
- **구간 건너뛰기**: 앞뒤로 건너뛸 시간을 직접 정할 수 있습니다.
- **화면 캡처**: 현재 프레임을 원본 해상도로 저장합니다.
- **영상 효과**: 90°/180°/270° 회전, 좌우 반전, 확대·축소, 밝기 등 색 조절을 할 수 있습니다.
- **전체화면**: 전체화면으로 볼 수 있습니다.
- **한국어 / English**: 화면 언어를 전환할 수 있습니다.
- **자동 업데이트**: 새 버전이 나오면 알려 주고 바로 설치합니다.

## 요구 사항

- Windows 10/11 64비트
- 설치 파일에 .NET 런타임이 포함돼 있어 따로 설치할 필요가 없습니다.

## 사용법

### 단축키

| 키 | 동작 |
|---|---|
| `Space` | 재생 / 일시정지 |
| `←` / `→` | 뒤로 / 앞으로 건너뛰기 |
| `+` / `-` | 확대 / 축소 |
| `Ctrl+O` | 파일 열기 |
| `Esc` | 전체화면 종료 |

### 지원 형식

mp4, mkv, avi, mov, wmv, flv, webm, mpeg/mpg, m4v, 3gp, ts/mts/m2ts, ogv, vob, asf, rm/rmvb, divx, xvid, h264, h265/hevc

## 데이터와 설정

| 데이터 | 위치 |
|---|---|
| 설정 | `%AppData%\Vidi\settings.ini` |
| 화면 캡처 | `사진\Vidi Captures` (기본값, 변경 가능) |

## 소스에서 빌드

.NET 8 SDK가 필요합니다.

### 빌드

```powershell
dotnet build Vidi.csproj
```

### 테스트

```powershell
dotnet run --project Vidi.Tests
```

### 배포

```powershell
$env:DOTNET_ROLL_FORWARD = 'Major'  # vpk 도구가 .NET 9를 요구합니다
powershell -ExecutionPolicy Bypass -File scripts\Publish-Velopack.ps1 -Version 1.0.0
```

결과물은 `artifacts\velopack\Releases`에 생깁니다.

## 프로젝트 구조

| 경로 | 내용 |
|---|---|
| `Form1.cs`, `VideoControlForm.cs` | 메인 창과 재생 컨트롤 |
| `PlaylistForm.cs` | 재생 목록 창 |
| `AppSettings.cs` | 설정 불러오기·저장 |
| `AppUpdateService.cs` | Velopack 업데이트 확인 |
| `FileAssociationService.cs` | 탐색기 파일 연결 |
| `Vidi.Tests/` | 테스트 |
| `icons/` | 앱 아이콘 |
| `scripts/Publish-Velopack.ps1` | 배포 패키지 생성 |

## 사용한 라이브러리

- [LibVLCSharp](https://github.com/videolan/libvlcsharp) / [LibVLC](https://www.videolan.org/vlc/libvlc.html)
- [Velopack](https://velopack.io/)
