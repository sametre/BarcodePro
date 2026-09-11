#define AppName "R3 M-Kobi Client"
#define AppVersion "2.1.0"
#define PublishDir "..\artifacts\publish\win-x64"
[Setup]
AppId={{58D77DCF-22F5-42D1-BBC7-203B6A8EEA61}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName=R3 M-Kobi Client 2.1.0
AppPublisher=R3 M-Kobi
DefaultDirName={autopf}\BarcodeProClient
UsePreviousAppDir=no
DefaultGroupName=R3 M-Kobi Client
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
OutputDir=..\artifacts\installer
OutputBaseFilename=BarcodePro-Client-2.1.0-Setup-x64
SetupIconFile=..\BarcodePrinter\Assets\BarcodePro.ico
UninstallDisplayIcon={app}\BarcodePrinter.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
VersionInfoVersion=2.1.0.0
InfoAfterFile=Beta-Readme.txt
[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
[Tasks]
Name: "desktopicon"; Description: "Masaüstü kısayolu oluştur"; GroupDescription: "Kısayollar:"
[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb"
Source: "Beta-Readme.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "licenses\*"; DestDir: "{app}\licenses"; Flags: ignoreversion
[Icons]
Name: "{group}\R3 M-Kobi Client"; Filename: "{app}\BarcodePrinter.exe"; Parameters: "--client"; WorkingDir: "{app}"
Name: "{autodesktop}\R3 M-Kobi Client"; Filename: "{app}\BarcodePrinter.exe"; Parameters: "--client"; WorkingDir: "{app}"; Tasks: desktopicon
[Run]
Filename: "{app}\BarcodePrinter.exe"; Parameters: "--client"; Description: "R3 M-Kobi Client'ı başlat"; Flags: nowait postinstall skipifsilent unchecked
