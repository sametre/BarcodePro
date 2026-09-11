param([string]$PublishDir = "$PSScriptRoot\..\artifacts\publish\win-x64",[string]$ResultPath)
$ErrorActionPreference='Stop'
$repository=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$runId=[Guid]::NewGuid().ToString('N')
$testRoot=Join-Path $repository ('artifacts\ServiceSmoke-'+$runId)
New-Item -ItemType Directory -Path $testRoot | Out-Null
if (!$ResultPath) { $ResultPath=Join-Path $testRoot 'result.txt' }
$serviceName='BarcodeProCheck'+$runId.Substring(0,12)
$dataDir=Join-Path $testRoot 'data'
$installDir=Join-Path $testRoot 'app'
$sourceData=Join-Path $testRoot 'empty-source.db'
$configure=Join-Path $PSScriptRoot 'Configure-BarcodeProServer.ps1'
$identity=[Security.Principal.WindowsIdentity]::GetCurrent()
$principal=New-Object Security.Principal.WindowsPrincipal($identity)
if (!$principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Test-Service.ps1 yonetici olarak calistirilmalidir.' }
$listener=New-Object Net.Sockets.TcpListener([Net.IPAddress]::Loopback,0)
$listener.Start();$port=$listener.LocalEndpoint.Port;$listener.Stop()
$results=New-Object 'System.Collections.Generic.List[string]'
function Check([bool]$Condition,[string]$Name) { if(!$Condition){throw $Name};$results.Add('PASS: '+$Name) }
try {
    Copy-Item -LiteralPath (Resolve-Path -LiteralPath $PublishDir).Path -Destination $installDir -Recurse
    & $configure -InstallDir $installDir -DataDir $dataDir -ServiceName $serviceName -Port $port -SourceData $sourceData -SkipPublicAddress
    $settings=Get-Content -LiteralPath (Join-Path $dataDir 'server.json') -Raw | ConvertFrom-Json
    $headers=@{'X-BarcodePro-Key'=$settings.AccessKey}
    $base='http://127.0.0.1:'+$port
    Check ($settings.AccessKey.Length -ge 48 -and $settings.AccessKey -ne 'owner') 'Installation generates a persistent random API key'
    Check ((Get-Service $serviceName).Status -eq 'Running') 'SCM service runs after configuration process exits'
    $service=Get-CimInstance Win32_Service -Filter ("Name='"+$serviceName+"'")
    Check ($service.StartName -eq ('NT SERVICE\'+$serviceName)) 'Service runs under its dedicated virtual account'
    Check ($service.StartMode -eq 'Auto') 'Service automatically starts with Windows'
    Check (Test-Path -LiteralPath (Join-Path $dataDir 'inventory.db')) 'Service owns central SQLite database'
    $sid=(New-Object Security.Principal.NTAccount('NT SERVICE',$serviceName)).Translate([Security.Principal.SecurityIdentifier]).Value
    $acl=Get-Acl -LiteralPath $dataDir
    $allowed=@($acl.Access | ForEach-Object {$_.IdentityReference.Translate([Security.Principal.SecurityIdentifier]).Value})
    Check ($acl.AreAccessRulesProtected -and $allowed.Count -eq 3 -and $allowed.Contains($sid) -and !$allowed.Contains('S-1-5-32-545')) 'Data directory grants only SYSTEM, administrators and this service access'
    $rule=Get-NetFirewallRule -Name ($serviceName+'-TCP')
    $scope=$rule | Get-NetFirewallAddressFilter
    Check ($rule.Profile.ToString() -notmatch 'Public' -and $scope.RemoteAddress -contains 'LocalSubnet') 'Firewall limits access to private/domain local subnet'
    $recovery=& "$env:SystemRoot\System32\sc.exe" qfailure $serviceName
    Check (($recovery -join ' ') -match '5000') 'SCM restart recovery is configured'
    $unauthorized=$false
    try { Invoke-RestMethod -Uri ($base+'/api/inventory') -Headers @{'X-BarcodePro-Key'='wrong'} -TimeoutSec 5 | Out-Null } catch { $unauthorized=([int]$_.Exception.Response.StatusCode -eq 401) }
    Check $unauthorized 'Service rejects incorrect access key'
    $productId=[Guid]::NewGuid().ToString()
    $product=@{Product=@{Id=$productId;Name='Service test';Barcode='8699000000001';Sku='SERVICE-TEST';Stock=10;Minimum=1;Maximum=100;Unit='Adet'}} | ConvertTo-Json -Depth 4
    Invoke-RestMethod -Method Post -Uri ($base+'/api/products') -Headers $headers -ContentType 'application/json' -Body $product -TimeoutSec 10 | Out-Null
    $move=@{ProductId=$productId;Kind=('Stok giri'+[char]0x15f+'i');Quantity=3;Note='Service persistence test'} | ConvertTo-Json
    Invoke-RestMethod -Method Post -Uri ($base+'/api/stock') -Headers $headers -ContentType 'application/json; charset=utf-8' -Body ([Text.Encoding]::UTF8.GetBytes($move)) -TimeoutSec 10 | Out-Null
    Restart-Service -Name $serviceName
    (Get-Service $serviceName).WaitForStatus('Running',[TimeSpan]::FromSeconds(20))
    $inventory=$null
    for($attempt=0;$attempt -lt 20;$attempt++){try{$inventory=Invoke-RestMethod -Uri ($base+'/api/inventory') -Headers $headers -TimeoutSec 3;break}catch{Start-Sleep -Milliseconds 500}}
    Check ($null -ne $inventory -and ($inventory.products | Where-Object id -eq $productId).stock -eq 13) 'Product and stock survive service restart'
    Check (@($inventory.movements).Count -ge 2) 'Stock audit survives service restart'
    $correction=@{ProductId=$productId;Kind=('Stok d'+[char]0xfc+'zeltme');Quantity=0;Note='Zero stock before deletion'} | ConvertTo-Json
    Invoke-RestMethod -Method Post -Uri ($base+'/api/stock') -Headers $headers -ContentType 'application/json; charset=utf-8' -Body ([Text.Encoding]::UTF8.GetBytes($correction)) -TimeoutSec 10 | Out-Null
    $delete=@{ProductId=$productId} | ConvertTo-Json
    $after=Invoke-RestMethod -Method Post -Uri ($base+'/api/products/delete') -Headers $headers -ContentType 'application/json' -Body $delete -TimeoutSec 10
    Check (@($after.products).Count -eq 0) 'Product deletion persists through API'
    & $configure -InstallDir $installDir -DataDir $dataDir -ServiceName $serviceName -Port $port -SourceData $sourceData -SkipPublicAddress
    $again=Get-Content -LiteralPath (Join-Path $dataDir 'server.json') -Raw | ConvertFrom-Json
    Check ($again.AccessKey -eq $settings.AccessKey) 'Repair preserves API key and database'
} catch {
    $results.Add('FAIL: '+$_)
    throw
} finally {
    try {
        if(Get-Service -Name $serviceName -ErrorAction SilentlyContinue){& $configure -InstallDir $installDir -DataDir $dataDir -ServiceName $serviceName -Remove}
        Check (!(Get-Service -Name $serviceName -ErrorAction SilentlyContinue)) 'Uninstall removes only the isolated service'
        Check (!(Get-NetFirewallRule -Name ($serviceName+'-TCP') -ErrorAction SilentlyContinue)) 'Uninstall removes isolated firewall rules'
        Check (Test-Path -LiteralPath (Join-Path $dataDir 'inventory.db')) 'Uninstall preserves central SQLite data'
    } catch { $results.Add('FAIL cleanup: '+$_) }
    $results | Set-Content -LiteralPath $ResultPath -Encoding UTF8
    $results | Write-Output
}
