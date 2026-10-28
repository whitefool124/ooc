$ErrorActionPreference='Stop'
$reviewRoot=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$reviewUrl='http://127.0.0.1:8876'
$connected=$false
try {
    $existing=Invoke-RestMethod -Uri "$reviewUrl/data.json" -TimeoutSec 2
    if($existing.version -ne 'OCC_Stage1_20261022_UiFinal') { throw 'Port 8876 belongs to a different app.' }
    $connected=$true
} catch {
    if($_.Exception.Message -like '*different app*') { throw }
}
if(-not $connected) {
    $python=(Get-Command python -ErrorAction Stop).Source
    $archive=Join-Path $reviewRoot 'Worldbuilding/归档/2026-10-22_界面人工审核'
    New-Item -ItemType Directory -Path $archive -Force | Out-Null
    $server=Join-Path $PSScriptRoot 'server.py'
    Start-Process -FilePath $python -WorkingDirectory $reviewRoot -ArgumentList @(('"'+$server+'"'),'--port','8876') -WindowStyle Hidden -RedirectStandardOutput (Join-Path $archive 'server-launch.log') -RedirectStandardError (Join-Path $archive 'server-launch-error.log')
    for($attempt=0;$attempt -lt 20;$attempt++) {
        Start-Sleep -Milliseconds 250
        try {
            $loaded=Invoke-RestMethod -Uri "$reviewUrl/data.json" -TimeoutSec 1
            if($loaded.version -eq 'OCC_Stage1_20261022_UiFinal') { $connected=$true;break }
        } catch {}
    }
    if(-not $connected) { throw 'Review server could not start. See the archive log.' }
}
Start-Process $reviewUrl
