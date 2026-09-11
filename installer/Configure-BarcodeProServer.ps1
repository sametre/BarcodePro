param(
    [Parameter(Mandatory=$true)][string]$InstallDir,
    [string]$DataDir = (Join-Path $env:ProgramData 'BarcodePro\Server'),
    [string]$ServiceName = 'BarcodeProServer',
    [ValidateRange(1024,65535)][int]$Port = 5088,
    [string]$SourceData = (Join-Path $env:LOCALAPPDATA 'BarcodePro\inventory.db'),
    [switch]$Prepare,
    [switch]$Remove,
    [switch]$SkipPublicAddress
)
$ErrorActionPreference = 'Stop'
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal($identity)
if (!$principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Bu script yonetici olarak calistirilmalidir.' }
if ($ServiceName -notmatch '^BarcodePro[A-Za-z0-9_-]*$') { throw 'Gecersiz servis adi.' }
$InstallDir = [IO.Path]::GetFullPath($InstallDir).TrimEnd('\')
$DataDir = [IO.Path]::GetFullPath($DataDir).TrimEnd('\')
$exePath = Join-Path $InstallDir 'BarcodePrinter.exe'
$tcpRule = $ServiceName + '-TCP'
$udpRule = $ServiceName + '-Discovery'

function Invoke-ServiceCommand([string[]]$Arguments) {
    $result = & "$env:SystemRoot\System32\sc.exe" @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) { throw ('Servis ayari basarisiz: ' + ($result -join ' ')) }
}
function Stop-BarcodeService {
    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($service -and $service.Status -ne 'Stopped') {
        Stop-Service -Name $ServiceName -ErrorAction Stop
        $service.WaitForStatus('Stopped',[TimeSpan]::FromSeconds(30))
    }
}
if ($Prepare) { Stop-BarcodeService; exit 0 }
if ($Remove) {
    Stop-BarcodeService
    if (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) { Invoke-ServiceCommand @('delete',$ServiceName) }
    foreach ($rule in @($tcpRule,$udpRule)) { Get-NetFirewallRule -Name $rule -ErrorAction SilentlyContinue | Remove-NetFirewallRule -ErrorAction Stop }
    if ($ServiceName -eq 'BarcodeProServer') {
        Get-NetFirewallRule -DisplayName 'Barcode Pro Server TCP 5088' -ErrorAction SilentlyContinue | Remove-NetFirewallRule -ErrorAction Stop
        $legacyShortcut = Join-Path ([Environment]::GetFolderPath('Startup')) 'Barcode Pro Server.lnk'
        Remove-Item -LiteralPath $legacyShortcut -Force -ErrorAction SilentlyContinue
    }
    # The server database, images, credentials and backups intentionally survive uninstall.
    exit 0
}
if (!(Test-Path -LiteralPath $exePath)) { throw "BarcodePrinter.exe bulunamadi: $exePath" }
Stop-BarcodeService

# Executables are installed in Program Files; only this service, Administrators and SYSTEM can change data.
New-Item -ItemType Directory -Path $DataDir -Force | Out-Null
$binaryPath = '"' + $exePath + '" --service --data-dir "' + $DataDir + '" --service-name ' + $ServiceName
if (!(Get-Service -Name $ServiceName -ErrorAction SilentlyContinue)) {
    Invoke-ServiceCommand @('create',$ServiceName,'binPath=',$binaryPath,'start=','delayed-auto','obj=',('NT SERVICE\'+$ServiceName),'DisplayName=','Barcode Pro Server')
} else {
    Invoke-ServiceCommand @('config',$ServiceName,'binPath=',$binaryPath,'start=','delayed-auto','obj=',('NT SERVICE\'+$ServiceName))
}
Invoke-ServiceCommand @('sidtype',$ServiceName,'unrestricted')
Invoke-ServiceCommand @('description',$ServiceName,'Barcode Pro merkezi SQLite envanter ve istemci servisi.')
Invoke-ServiceCommand @('failure',$ServiceName,'reset=','86400','actions=','restart/5000/restart/15000/restart/60000')
Invoke-ServiceCommand @('failureflag',$ServiceName,'1')

$acl = New-Object Security.AccessControl.DirectorySecurity
$acl.SetAccessRuleProtection($true,$false)
$rights = [Security.AccessControl.FileSystemRights]::FullControl
$inherit = [Security.AccessControl.InheritanceFlags]'ContainerInherit,ObjectInherit'
$propagation = [Security.AccessControl.PropagationFlags]::None
$allow = [Security.AccessControl.AccessControlType]::Allow
$serviceSid = (New-Object Security.Principal.NTAccount('NT SERVICE',$ServiceName)).Translate([Security.Principal.SecurityIdentifier])
foreach ($sid in @((New-Object Security.Principal.SecurityIdentifier('S-1-5-18')),(New-Object Security.Principal.SecurityIdentifier('S-1-5-32-544')),$serviceSid)) {
    $acl.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule($sid,$rights,$inherit,$propagation,$allow)))
}
Set-Acl -LiteralPath $DataDir -AclObject $acl

$seedPath = Join-Path $InstallDir 'seed\inventory.db'
$initializeArgs = '--initialize-server --data-dir "' + $DataDir + '" --source-data "' + $SourceData + '" --seed-data "' + $seedPath + '" --port ' + $Port
$process = Start-Process -FilePath $exePath -ArgumentList $initializeArgs -WindowStyle Hidden -PassThru -Wait
if ($process.ExitCode -ne 0) { throw ('SQLite hazirlanamadi. Mevcut veriler korunmustur. Ayrinti: ' + (Join-Path $DataDir 'service-error.log')) }

# The shared service SID needs read/execute access to a custom installation folder too.
$installAcl = Get-Acl -LiteralPath $InstallDir
$installAcl.SetAccessRule((New-Object Security.AccessControl.FileSystemAccessRule($serviceSid,[Security.AccessControl.FileSystemRights]::ReadAndExecute,$inherit,$propagation,$allow)))
Set-Acl -LiteralPath $InstallDir -AclObject $installAcl

foreach ($rule in @($tcpRule,$udpRule)) { Get-NetFirewallRule -Name $rule -ErrorAction SilentlyContinue | Remove-NetFirewallRule -ErrorAction Stop }
New-NetFirewallRule -Name $tcpRule -DisplayName 'Barcode Pro Server - Yerel ag' -Direction Inbound -Action Allow -Protocol TCP -LocalPort $Port -Program $exePath -Profile Private,Domain -RemoteAddress LocalSubnet | Out-Null
New-NetFirewallRule -Name $udpRule -DisplayName 'Barcode Pro Server - Otomatik bulma' -Direction Inbound -Action Allow -Protocol UDP -LocalPort 5089 -Program $exePath -Profile Private,Domain -RemoteAddress LocalSubnet | Out-Null
if ($ServiceName -eq 'BarcodeProServer') {
    Get-NetFirewallRule -DisplayName 'Barcode Pro Server TCP 5088' -ErrorAction SilentlyContinue | Remove-NetFirewallRule -ErrorAction Stop
    Remove-Item -LiteralPath (Join-Path ([Environment]::GetFolderPath('Startup')) 'Barcode Pro Server.lnk') -Force -ErrorAction SilentlyContinue
}
Start-Service -Name $ServiceName
(Get-Service -Name $ServiceName).WaitForStatus('Running',[TimeSpan]::FromSeconds(30))
$configuration = Get-Content -LiteralPath (Join-Path $DataDir 'server.json') -Raw | ConvertFrom-Json
$healthy = $false
for ($attempt=0;$attempt -lt 20;$attempt++) {
    try {
        $health = Invoke-RestMethod -Uri ('http://127.0.0.1:'+$Port+'/api/health') -Headers @{'X-BarcodePro-Key'=$configuration.AccessKey} -TimeoutSec 2
        if ($health.status -eq 'ok') { $healthy=$true;break }
    } catch { Start-Sleep -Milliseconds 500 }
}
if (!$healthy) { throw 'Servis kaydedildi ancak saglik kontrolu basarisiz. Server servis gunlugunu kontrol edin.' }

$publicAddress = 'Alinamadi'
if (!$SkipPublicAddress) {
    try { $candidate = ([string](Invoke-RestMethod -Uri 'https://api.ipify.org' -TimeoutSec 4)).Trim();$parsed=$null;if([Net.IPAddress]::TryParse($candidate,[ref]$parsed)){$publicAddress=$candidate} } catch { }
}
$addresses = @(Get-NetIPConfiguration | Where-Object { $_.NetAdapter.Status -eq 'Up' } | ForEach-Object { $_.IPv4Address.IPAddress } | Where-Object { $_ -and $_ -notlike '169.254.*' -and $_ -ne '127.0.0.1' } | Sort-Object -Unique)
$lines = @('BARCODE PRO SERVER BAGLANTI BILGILERI','',('Bilgisayar: '+$env:COMPUTERNAME),('Servis: '+$ServiceName+' (otomatik baslangic)'),('Port: '+$Port),'','Client adresleri:') +
    @($addresses | ForEach-Object { 'http://'+$_+':'+$Port }) + @('http://'+$env:COMPUTERNAME+':'+$Port,'',('Dis IP: '+$publicAddress),'Dis ag icin kurumsal VPN veya HTTPS ag gecidi gerekir.','',('SQLite: '+(Join-Path $DataDir 'inventory.db')),'Erisim anahtari Server yonetim ekraninda gosterilir.','Server penceresi kapali olsa da servis calisir.','Guvenlik duvari: yalniz Ozel/Etki alani profili ve yerel alt ag.')
$lines | Set-Content -LiteralPath (Join-Path $DataDir 'Server-Baglanti-Bilgileri.txt') -Encoding UTF8
if ($ServiceName -eq 'BarcodeProServer') { $lines | Set-Content -LiteralPath (Join-Path ([Environment]::GetFolderPath('CommonDesktopDirectory')) 'Barcode Pro Server Baglanti Bilgileri.txt') -Encoding UTF8 }
Write-Output ('PASS: '+$ServiceName+' calisiyor. SQLite: '+(Join-Path $DataDir 'inventory.db'))
