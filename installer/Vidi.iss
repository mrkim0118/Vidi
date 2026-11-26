; Vidi installer (Inno Setup 6). Built by scripts\Build-Installer.ps1, which passes
; AppVersion, SourceDir (the self-contained publish folder) and OutputDir with /D.

#ifndef AppVersion
  #error AppVersion is not defined. Run scripts\Build-Installer.ps1.
#endif

[Setup]
AppId={{A35BE798-B412-4706-B910-5093B7F5DA97}
AppName=Vidi
AppVersion={#AppVersion}
AppVerName=Vidi {#AppVersion}
AppPublisher=Manggdy
DefaultDirName={autopf}\Vidi
DefaultGroupName=Vidi
DisableProgramGroupPage=yes
; Machine-wide install in Program Files.
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
SetupIconFile=..\icons\TASP_icon.ico
UninstallDisplayIcon={app}\Vidi.exe
OutputDir={#OutputDir}
OutputBaseFilename=Vidi-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
ChangesAssociations=yes
#ifdef Sign
; Build-Installer.ps1 defines the "manggdysign" tool (installer\sign.ps1, "Manggdy" certificate).
SignTool=manggdysign
SignedUninstaller=yes
#endif

[Languages]
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\Vidi"; Filename: "{app}\Vidi.exe"
Name: "{autodesktop}\Vidi"; Filename: "{app}\Vidi.exe"; Tasks: desktopicon

[Run]
; The "Open with" entry and the startup VLC warm-up live in HKCU, so register them as the user who started setup.
Filename: "{app}\Vidi.exe"; Parameters: "--register"; Flags: runhidden waituntilterminated runasoriginaluser
Filename: "{app}\Vidi.exe"; Description: "{cm:LaunchProgram,Vidi}"; Flags: nowait postinstall skipifsilent runasoriginaluser

[UninstallRun]
Filename: "{app}\Vidi.exe"; Parameters: "--unregister"; RunOnceId: "VidiUnregister"; Flags: runhidden waituntilterminated
