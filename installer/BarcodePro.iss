#define AppName "Barcode Pro Beta"
#define AppVersion "2.1.0-beta.4"
#ifndef PublishDir
  #define PublishDir "..\artifacts\publish\win-x64"
#endif
[Setup]
AppId={{ED32C4BC-57DA-43EE-890A-2257A1B2C19D}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName=Barcode Pro 2.1 Beta 4
AppPublisher=Barcode Pro
DefaultDirName={localappdata}\Programs\Barcode Pro Beta
DefaultGroupName=Barcode Pro Beta
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
OutputDir=..\artifacts\installer
OutputBaseFilename=BarcodePro-2.1.0-beta.4-Setup-x64
SetupIconFile=..\BarcodePrinter\Assets\BarcodePro.ico
UninstallDisplayIcon={app}\BarcodePrinter.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
VersionInfoVersion=2.1.0.3
VersionInfoDescription=Barcode Pro Beta Kurulumu
CloseApplications=no
RestartApplications=no
SetupLogging=yes
InfoAfterFile=Beta-Readme.txt

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Tasks]
Name: "desktopicon"; Description: "Masaüstü kısayolu oluştur"; GroupDescription: "Kısayollar:"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb"
Source: "Beta-Readme.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\docs\MYSQL-CONNECTION.md"; DestDir: "{app}\docs"; Flags: ignoreversion
Source: "licenses\*"; DestDir: "{app}\licenses"; Flags: ignoreversion

[Icons]
Name: "{group}\Barcode Pro Beta"; Filename: "{app}\BarcodePrinter.exe"; WorkingDir: "{app}"
Name: "{group}\Kullanım notları"; Filename: "{app}\Beta-Readme.txt"
Name: "{autodesktop}\Barcode Pro Beta"; Filename: "{app}\BarcodePrinter.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\BarcodePrinter.exe"; Description: "Barcode Pro Beta uygulamasını aç"; Flags: nowait postinstall skipifsilent unchecked

; Envanter ve kullanıcı ayarları uygulama dizininin dışında tutulur.
; Kaldırıcı %LOCALAPPDATA%\BarcodePro dizinine dokunmaz.

[Code]
function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  if FileExists(ExpandConstant('{app}\BarcodePrinter.exe')) and
     CheckForMutexes('Local\BarcodePro.Inventory') then
    Result := 'Güncellemeden önce açık Barcode Pro uygulamasını kapatın.';
end;

