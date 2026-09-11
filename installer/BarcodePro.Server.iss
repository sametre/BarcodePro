#define AppName "Barcode Pro Server"
#define AppVersion "2.1.0"
#define PublishDir "..\artifacts\publish\win-x64"
[Setup]
AppId={{9540C7A7-5C08-4BB4-9892-9A644DF45501}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName=Barcode Pro Server 2.1.0
AppPublisher=Barcode Pro
DefaultDirName={autopf}\Barcode Pro Server
UsePreviousAppDir=no
DefaultGroupName=Barcode Pro Server
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
OutputDir=..\artifacts\installer
#ifdef IncludeCustomerSeed
OutputBaseFilename=BarcodePro-Server-2.1.0-Customer-Setup-x64
#else
OutputBaseFilename=BarcodePro-Server-2.1.0-Setup-x64
#endif
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
Source: "Configure-BarcodeProServer.ps1"; DestDir: "{app}"; Flags: ignoreversion
#ifdef IncludeCustomerSeed
#if FileExists("..\artifacts\customer-seed\inventory.db")
Source: "..\artifacts\customer-seed\inventory.db"; DestDir: "{app}\seed"; Flags: ignoreversion
#endif
#endif
[Icons]
Name: "{group}\Barcode Pro Server"; Filename: "{app}\BarcodePrinter.exe"; Parameters: "--server"; WorkingDir: "{app}"
Name: "{group}\Barcode Pro Server Yönetimi"; Filename: "{app}\BarcodePrinter.exe"; Parameters: "--server-admin"; WorkingDir: "{app}"
Name: "{autodesktop}\Barcode Pro Server"; Filename: "{app}\BarcodePrinter.exe"; Parameters: "--server"; WorkingDir: "{app}"; Tasks: desktopicon
[Run]
Filename: "{app}\BarcodePrinter.exe"; Parameters: "--server"; Description: "Barcode Pro Server'ı başlat"; Flags: nowait postinstall skipifsilent unchecked
[Code]
function ConfigureServer(ScriptPath, Extra: String): Boolean;
var ExitCode: Integer; SettingsPath, ServiceName, DataPath, Port, SourcePath, Arguments: String;
begin
  SettingsPath := ExpandConstant('{app}\server-install.ini');
  ServiceName := GetIniString('Server', 'ServiceName', ExpandConstant('{param:SERVICENAME|BarcodeProServer}'), SettingsPath);
  DataPath := GetIniString('Server', 'DataDirectory', ExpandConstant('{param:SERVERDATADIR|{commonappdata}\BarcodePro\Server}'), SettingsPath);
  Port := GetIniString('Server', 'Port', ExpandConstant('{param:SERVERPORT|5088}'), SettingsPath);
  SourcePath := ExpandConstant('{param:SOURCEDATA|{localappdata}\BarcodePro\inventory.db}');
  if Extra = '' then begin
    SetIniString('Server', 'ServiceName', ServiceName, SettingsPath);
    SetIniString('Server', 'DataDirectory', DataPath, SettingsPath);
    SetIniString('Server', 'Port', Port, SettingsPath);
  end;
  Arguments := '-NoProfile -ExecutionPolicy Bypass -File "' + ScriptPath + '" -InstallDir "' + ExpandConstant('{app}') + '" -ServiceName "' + ServiceName + '" -DataDir "' + DataPath + '" -Port ' + Port + ' -SourceData "' + SourcePath + '" ' + Extra;
  Result := Exec(ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'),
    Arguments, '', SW_HIDE, ewWaitUntilTerminated, ExitCode);
  Result := Result and (ExitCode = 0);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  ExtractTemporaryFile('Configure-BarcodeProServer.ps1');
  if not ConfigureServer(ExpandConstant('{tmp}\Configure-BarcodeProServer.ps1'), '-Prepare') then
    Result := 'Barcode Pro Server servisi durdurulamadi. Kurulumu yonetici olarak tekrar calistirin.';
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    if not ConfigureServer(ExpandConstant('{app}\Configure-BarcodeProServer.ps1'), '') then
      RaiseException('Server servisi veya ag ayarlari tamamlanamadi. ProgramData\BarcodePro\Server\service-error.log dosyasini kontrol edin. Kurulumu tekrar calistirarak onarin.');
end;

function InitializeUninstall(): Boolean;
begin
  Result := ConfigureServer(ExpandConstant('{app}\Configure-BarcodeProServer.ps1'), '-Remove');
  if not Result then
    MsgBox('Server servisi kaldirilamadi. Kaldirma islemi iptal edildi; yonetici olarak tekrar deneyin.', mbError, MB_OK);
end;
