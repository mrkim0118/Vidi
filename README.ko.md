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

- `Vidi-Setup-x.y.z.exe`: 설치 파일 (`C:\Program Files\Vidi`에 설치하고 탐색기 "연결 프로그램" 목록에 Vidi를 추가합니다)

## 주요 기능

- **끌어다 놓기 재생**: 파일을 창에 놓거나 탐색기에서 더블클릭하면 바로 재생합니다. 여러 파일을 한 번에 열면 모두 재생 목록에 들어갑니다.
- **재생 목록**: 순서 바꾸기, 추가, 삭제를 지원합니다.
- **구간 건너뛰기**: 앞뒤로 건너뛸 시간을 직접 정할 수 있습니다.
- **화면 캡처**: 현재 프레임을 원본 해상도로 저장합니다.
- **영상 효과**: 90°/180°/270° 회전, 좌우 반전, 확대·축소, 밝기 등 색 조절을 할 수 있습니다.
- **전체화면**: 영상을 더블클릭하거나 `F`를 누릅니다. 마우스를 움직이면 조작 막대가 나타나고, 잠시 뒤 마우스 커서와 함께 숨겨집니다.
- **이어보기**: 파일을 다시 열면 마지막으로 보던 곳부터 재생합니다. 끝까지 본 파일은 처음부터 재생합니다.
- **최근 파일**: 우클릭 메뉴에서 최근에 연 파일 10개를 바로 열 수 있습니다.
- **재생 속도**: 0.5배에서 2배까지 바꿀 수 있습니다.
- **자막**: 영상과 이름이 같은 자막 파일은 자동으로 불러옵니다. 자막 파일을 영상에 끌어다 놓거나 메뉴에서 열 수 있고, 자막 트랙을 바꾸거나 끌 수 있습니다.
- **음성 트랙**: 음성이 여러 개 들어 있는 영상에서 원하는 음성을 고릅니다.
- **반복**: 한 개 반복, 목록 전체 반복, 두 지점 사이 구간 반복(A-B)을 지원합니다.
- **프레임 이동**: `,`와 `.`로 한 프레임씩 움직입니다.
- **한국어 / English**: 화면 언어를 전환할 수 있습니다.
- **업데이트 확인**: 실행할 때 GitHub 최신 릴리스를 확인하고, 새 버전이 있으면 내려받아 설치할지 묻습니다.

## 요구 사항

- Windows 10/11 64비트
- 설치 파일에 .NET 런타임이 포함돼 있어 따로 설치할 필요가 없습니다.

## 사용법

### 단축키

| 키 | 동작 |
|---|---|
| `Space` | 재생 / 일시정지 |
| `←` / `→` | 뒤로 / 앞으로 건너뛰기 |
| `↑` / `↓`, 마우스 휠 | 음량 올리기 / 내리기 |
| `M` | 음소거 |
| `F`, `Enter`, 더블클릭 | 전체화면 전환 |
| `,` / `.` | 이전 / 다음 프레임 |
| `+` / `-` | 확대 / 축소 |
| `Ctrl+O` | 파일 열기 |
| `Esc` | 전체화면 종료 |

### 지원 형식

mp4, mkv, avi, mov, wmv, flv, webm, mpeg/mpg, m4v, 3gp, ts/mts/m2ts, ogv, vob, asf, rm/rmvb, divx, xvid, h264, h265/hevc

## 데이터와 설정

| 데이터 | 위치 |
|---|---|
| 설정 | `%AppData%\Vidi\settings.ini` |
| 최근 파일과 이어보기 위치 | `%AppData%\Vidiesume.txt` |
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
powershell -ExecutionPolicy Bypass -File scripts\Build-Installer.ps1
```

이 스크립트는 앱을 `artifacts\publish`에 게시하고(.NET 런타임 포함, PDB 제외) `Vidi.exe`를 `CN=Manggdy` 인증서로 서명한 뒤, [Inno Setup 6](https://jrsoftware.org/isinfo.php)로 `artifacts\installer\Vidi-Setup-<버전>.exe`를 만듭니다. 버전은 `Vidi.csproj`의 `<Version>`을 씁니다. 인증서 없이 빌드하려면 `-NoSign`을 붙입니다.

설치가 끝나면 설치 프로그램이 로그인한 사용자 권한으로 `Vidi.exe --register`를 실행해 "연결 프로그램" 항목과 시작 시 VLC 미리 준비를 등록하고, 제거할 때는 `Vidi.exe --unregister`를 실행합니다.

## 프로젝트 구조

| 경로 | 내용 |
|---|---|
| `Form1.cs`, `VideoControlForm.cs` | 메인 창과 재생 컨트롤 |
| `PlaylistForm.cs` | 재생 목록 창 |
| `AppSettings.cs` | 설정 불러오기·저장 |
| `ResumeStore.cs` | 최근 파일과 이어보기 위치 |
| `DarkMenuRenderer.cs` | 어두운 우클릭 메뉴 스타일 |
| `GitHubUpdater.cs` | GitHub 릴리스 업데이트 확인 |
| `FileAssociationService.cs` | 탐색기 파일 연결 |
| `Vidi.Tests/` | 테스트 |
| `icons/` | 앱 아이콘 |
| `installer/Vidi.iss` | Inno Setup 설치 스크립트 |
| `scripts/Build-Installer.ps1` | 릴리스 빌드와 설치 파일 생성 |

## 사용한 라이브러리

- [LibVLCSharp](https://github.com/videolan/libvlcsharp) / [LibVLC](https://www.videolan.org/vlc/libvlc.html)
- [Inno Setup](https://jrsoftware.org/isinfo.php): 설치 프로그램
