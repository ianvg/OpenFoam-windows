#ifndef PayloadDir
  #error PayloadDir must point to the published Windows application
#endif
#ifndef OutputDir
  #error OutputDir must point to the installer output folder
#endif
#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif

[Setup]
AppId={{E045D29F-4461-4DB0-A1FC-53CBF945B93C}
AppName=Foam Studio
AppVersion={#AppVersion}
AppPublisher=Ian Van Giesen
AppPublisherURL=https://github.com/ianvg/OpenFoam-windows
AppSupportURL=https://github.com/ianvg/OpenFoam-windows/issues
DefaultDirName={localappdata}\Programs\Foam Studio
DefaultGroupName=Foam Studio
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19041
OutputDir={#OutputDir}
OutputBaseFilename=Foam-Studio-{#AppVersion}-win-x64-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
LicenseFile=..\LICENSE
InfoBeforeFile=Prerequisites.txt
UninstallDisplayIcon={app}\FoamStudio.App.exe
AppMutex=Local\FoamStudio.Running
SetupMutex=Local\FoamStudio.Setup
CloseApplications=no
RestartApplications=no
DisableProgramGroupPage=yes
SetupLogging=yes

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked

[Files]
Source: "{#PayloadDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{userprograms}\Foam Studio"; Filename: "{app}\FoamStudio.App.exe"; WorkingDir: "{app}"
Name: "{userdesktop}\Foam Studio"; Filename: "{app}\FoamStudio.App.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\FoamStudio.App.exe"; Description: "Launch Foam Studio"; Flags: nowait postinstall skipifsilent
