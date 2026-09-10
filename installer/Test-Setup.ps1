param(
    [string]$Setup = "$PSScriptRoot\..\artifacts\installer\BarcodePro-Client-2.1.0-beta.6-Setup-x64.exe",
    [string]$AppId = '58D77DCF-22F5-42D1-BBC7-203B6A8EEA61',
    [switch]$MachineInstall
)
$ErrorActionPreference='Stop'
$repository=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$testRoot=Join-Path $repository ('artifacts\SetupSmoke-'+[Guid]::NewGuid())
$uninstallRoot = if($MachineInstall){'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{'}else{'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{'}
$uninstallKey=$uninstallRoot+$AppId+'}_is1'
if(Test-Path -LiteralPath $uninstallKey){throw 'Mevcut beta kurulumu var; kurulum testi çalıştırılmadı.'}
New-Item -ItemType Directory -Path $testRoot | Out-Null
$installDir=Join-Path $testRoot 'app'
$setupPath=(Resolve-Path -LiteralPath $Setup).Path
$argsList=@('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/NOICONS','/TASKS=""',('/DIR="'+$installDir+'"'),('/LOG="'+(Join-Path $testRoot 'install.log')+'"'))
try {
    $process=Start-Process -FilePath $setupPath -ArgumentList $argsList -WindowStyle Hidden -PassThru -Wait
    if($process.ExitCode -ne 0){throw "Setup hata kodu: $($process.ExitCode)"}
    $files=Get-ChildItem -LiteralPath (Join-Path $repository 'artifacts\publish\win-x64') -File -Recurse | Where-Object Extension -ne '.pdb'
    foreach($file in $files){$relative=[IO.Path]::GetRelativePath((Join-Path $repository 'artifacts\publish\win-x64'),$file.FullName);$installed=Join-Path $installDir $relative;if(!(Test-Path -LiteralPath $installed) -or (Get-FileHash -LiteralPath $installed).Hash -ne (Get-FileHash -LiteralPath $file.FullName).Hash){throw "Kurulan dosya uyuşmuyor: $relative"}}
    $runtime=Get-Content -LiteralPath (Join-Path $installDir 'BarcodePrinter.runtimeconfig.json') -Raw | ConvertFrom-Json
    if(!$runtime.runtimeOptions.includedFrameworks -or !(Test-Path -LiteralPath (Join-Path $installDir 'coreclr.dll'))){throw 'Self-contained çalışma zamanı eksik.'}
    foreach($privateFile in @('inventory.json','mysql-connection.json','company.json','printers.json')){if(Test-Path -LiteralPath (Join-Path $installDir $privateFile)){throw "Kişisel veri pakete girmiş: $privateFile"}}
    if(!(Test-Path -LiteralPath $uninstallKey)){throw 'Kaldırma kaydı oluşturulmadı.'}
    Write-Output "PASS: Kurulum, kaldırma kaydı ve $($files.Count) dosya özeti doğrulandı."
} finally {
    $uninstaller=Join-Path $installDir 'unins000.exe'
    $expectedPrefix=[IO.Path]::GetFullPath((Join-Path $repository 'artifacts'))+[IO.Path]::DirectorySeparatorChar
    if(![IO.Path]::GetFullPath($uninstaller).StartsWith($expectedPrefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Test kaldırma yolu çalışma alanı dışında.'}
    if(Test-Path -LiteralPath $uninstaller){$process=Start-Process -FilePath $uninstaller -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',('/LOG="'+(Join-Path $testRoot 'uninstall.log')+'"')) -WindowStyle Hidden -PassThru -Wait;if($process.ExitCode -ne 0){throw "Kaldırma hata kodu: $($process.ExitCode)"}}
}
if((Test-Path -LiteralPath (Join-Path $installDir 'BarcodePrinter.exe')) -or (Test-Path -LiteralPath $uninstallKey)){throw 'Test kurulumu tam kaldırılamadı.'}
Write-Output "PASS: Kaldırma başarılı. Günlükler: $testRoot"

