param(
    [Parameter(Mandatory=$true)][string]$InstallDir,
    [switch]$Remove
)
$ErrorActionPreference = 'Stop'
$ruleName = 'Barcode Pro Server TCP 5088'
$shortcutPath = Join-Path ([Environment]::GetFolderPath('Startup')) 'Barcode Pro Server.lnk'
$infoPath = Join-Path ([Environment]::GetFolderPath('Desktop')) 'Barcode Pro Server Baglanti Bilgileri.txt'
$exePath = Join-Path $InstallDir 'BarcodePrinter.exe'

if ($Remove) {
    Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue | Remove-NetFirewallRule -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $shortcutPath -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $infoPath -Force -ErrorAction SilentlyContinue
    exit 0
}

if (!(Test-Path -LiteralPath $exePath)) { throw "BarcodePrinter.exe bulunamadı: $exePath" }

Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue | Remove-NetFirewallRule -ErrorAction SilentlyContinue
New-NetFirewallRule -DisplayName $ruleName -Direction Inbound -Action Allow -Protocol TCP -LocalPort 5088 -Program $exePath -Profile Private,Domain | Out-Null

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $exePath
$shortcut.Arguments = '--server'
$shortcut.WorkingDirectory = $InstallDir
$shortcut.IconLocation = "$exePath,0"
$shortcut.Description = 'Barcode Pro Server otomatik başlangıç'
$shortcut.Save()

$addresses = @(Get-NetIPConfiguration | Where-Object { $_.NetAdapter.Status -eq 'Up' } | ForEach-Object { $_.IPv4Address.IPAddress } | Where-Object { $_ -and $_ -notlike '169.254.*' -and $_ -ne '127.0.0.1' } | Sort-Object -Unique)
if ($addresses.Count -eq 0) { $addresses = @('IP adresi bulunamadı; ağ bağlantısını kontrol edin') }
$lines = @(
    'BARCODE PRO SERVER BAGLANTI BILGILERI',
    '',
    ('Bilgisayar: ' + $env:COMPUTERNAME),
    'Port: 5088',
    'Erisim anahtari: owner',
    '',
    'Client adresleri:'
) + @($addresses | ForEach-Object { 'http://' + $_ + ':5088' }) + @(
    '',
    'Server penceresi acik kalmalidir.',
    'Windows Guvenlik Duvari izni otomatik olusturuldu.',
    'Server Windows oturum acilisina otomatik eklendi.'
)
$lines | Set-Content -LiteralPath $infoPath -Encoding UTF8
$lines | Set-Content -LiteralPath (Join-Path $InstallDir 'Server-Baglanti-Bilgileri.txt') -Encoding UTF8
