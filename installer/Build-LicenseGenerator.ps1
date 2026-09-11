$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
dotnet publish (Join-Path $root 'R3LicenseGenerator/R3LicenseGenerator.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -p:DebugType=None -p:DebugSymbols=false -o (Join-Path $root 'artifacts/local-license-generator')
Write-Host "Local license generator: $root\artifacts\local-license-generator\R3LicenseGenerator.exe"
