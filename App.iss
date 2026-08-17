#define MyAppName "DS Hub"
#define MyAppVersion "3.12.0.0"
#define MyAppExeName "DS Hub.exe"

[Setup]
AppId={{B7C4E1A2-9F3D-4E8B-8A5C-2D6F0E1C4B7A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=DS Hub Project
AppPublisherURL=https://github.com/
DefaultDirName={localappdata}\Programs\{#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=D:\DSH_start\release
OutputBaseFilename=DS-Hub-Setup-{#MyAppVersion}
SetupIconFile=D:\DSH_start\DeepSeek.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}

[Languages]
Name: "chinesesimp"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加任务:"; Flags: checkedonce

[Files]
Source: "D:\DSH_start\DS Hub.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "D:\DSH_start\DeepSeek.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "D:\DSH_start\DSH-Web-Launcher.ps1"; DestDir: "{app}"; Flags: ignoreversion
Source: "D:\DSH_start\DSH-Web-Stop.ps1"; DestDir: "{app}"; Flags: ignoreversion
Source: "D:\DSH_start\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "D:\DSH_start\LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\DeepSeek.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\DeepSeek.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "启动 {#MyAppName}"; Flags: nowait postinstall skipifsilent
