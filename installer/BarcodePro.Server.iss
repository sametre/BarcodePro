#define AppName "Barcode Pro Server Beta"
#define AppVersion "2.1.0-beta.5"
#define PublishDir "..\artifacts\publish\win-x64"
[Setup]
AppId={{9540C7A7-5C08-4BB4-9892-9A644DF45501}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName=Barcode Pro Server 2.1 Beta 5
AppPublisher=Barcode Pro
DefaultDirName={localappdata}\Programs\Barcode Pro Server Beta
DefaultGroupName=Barcode Pro Server Beta
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
OutputDir=..\artifacts\installer
OutputBaseFilename=BarcodePro-Server-2.1.0-beta.5-Setup-x64
SetupIconFile=..\BarcodePrinter\Assets\BarcodePro.ico
UninstallDisplayIcon={app}\BarcodePrinter.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
VersionInfoVersion=2.1.0.5
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
Name: "{group}\Barcode Pro Server"; Filename: "{app}\BarcodePrinter.exe"; Parameters: "--server"; WorkingDir: "{app}"
Name: "{group}\Barcode Pro Server Yönetimi"; Filename: "{app}\BarcodePrinter.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\Barcode Pro Server"; Filename: "{app}\BarcodePrinter.exe"; Parameters: "--server"; WorkingDir: "{app}"; Tasks: desktopicon
[Run]
Filename: "{app}\BarcodePrinter.exe"; Parameters: "--server"; Description: "Barcode Pro Server'ı başlat"; Flags: nowait postinstall skipifsilent unchecked
