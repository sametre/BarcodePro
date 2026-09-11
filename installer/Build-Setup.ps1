param([string]$Compiler = "$PSScriptRoot\..\artifacts\tools\InnoSetup\ISCC.exe",[switch]$SkipChecks,[switch]$IncludeCustomerSeed)
$ErrorActionPreference = 'Stop'
$repository = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $repository
try {
    & dotnet build BarcodePrinter.slnx -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Derleme başarısız.' }
    if (!$SkipChecks) {
        & dotnet run --project BarcodePrinter.Checks -c Release --no-build
        if ($LASTEXITCODE -ne 0) { throw 'Kontroller başarısız.' }
    }
    & dotnet publish BarcodePrinter/BarcodePrinter.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false -p:DebugType=None -p:DebugSymbols=false -o artifacts/publish/win-x64
    if ($LASTEXITCODE -ne 0) { throw 'Yayın başarısız.' }
    if (!(Test-Path -LiteralPath $Compiler)) { throw 'Inno Setup 6.7+ ISCC.exe yolunu -Compiler ile belirtin.' }
    foreach($script in @('installer/BarcodePro.Server.iss','installer/BarcodePro.Client.iss')) {
        if ($IncludeCustomerSeed -and $script -like '*Server.iss') { & $Compiler /DIncludeCustomerSeed=1 $script } else { & $Compiler $script }
        if ($LASTEXITCODE -ne 0) { throw "Setup derlemesi başarısız: $script" }
    }
    $serverName=if($IncludeCustomerSeed){'BarcodePro-Server-2.1.0-Customer-Setup-x64.exe'}else{'BarcodePro-Server-2.1.0-Setup-x64.exe'}
    foreach($name in @($serverName,'BarcodePro-Client-2.1.0-Setup-x64.exe')) {
        $setup = Join-Path $repository ('artifacts/installer/' + $name)
        $hash = (Get-FileHash -LiteralPath $setup -Algorithm SHA256).Hash
        Set-Content -LiteralPath ($setup + '.sha256') -Value ($hash + '  ' + $name) -Encoding ascii
        Get-Item -LiteralPath $setup | Select-Object FullName,Length
    }
} finally { Pop-Location }

