param([string]$Compiler = "$PSScriptRoot\..\artifacts\tools\InnoSetup\ISCC.exe")
$ErrorActionPreference = 'Stop'
$repository = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $repository
try {
    & dotnet build BarcodePrinter.slnx -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Derleme başarısız.' }
    & dotnet run --project BarcodePrinter.Checks -c Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'Kontroller başarısız.' }
    & dotnet publish BarcodePrinter/BarcodePrinter.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false -p:DebugType=None -p:DebugSymbols=false -o artifacts/publish/win-x64
    if ($LASTEXITCODE -ne 0) { throw 'Yayın başarısız.' }
    if (!(Test-Path -LiteralPath $Compiler)) { throw 'Inno Setup 6.7+ ISCC.exe yolunu -Compiler ile belirtin.' }
    & $Compiler installer/BarcodePro.iss
    if ($LASTEXITCODE -ne 0) { throw 'Setup derlemesi başarısız.' }
    $setup = Join-Path $repository 'artifacts/installer/BarcodePro-2.1.0-beta.4-Setup-x64.exe'
    $hash = (Get-FileHash -LiteralPath $setup -Algorithm SHA256).Hash
    Set-Content -LiteralPath ($setup + '.sha256') -Value ($hash + '  ' + [IO.Path]::GetFileName($setup)) -Encoding ascii
    Get-Item -LiteralPath $setup | Select-Object FullName,Length
} finally { Pop-Location }

