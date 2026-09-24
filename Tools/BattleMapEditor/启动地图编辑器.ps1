$ErrorActionPreference = 'Stop'
$editorRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$editorPort = 4179
$editorUrl = "http://127.0.0.1:$editorPort"
$alreadyRunning = $false
try {
    $response = Invoke-WebRequest -Uri $editorUrl -UseBasicParsing -TimeoutSec 1
    $alreadyRunning = $response.StatusCode -eq 200
} catch {
    $alreadyRunning = $false
}
if (-not $alreadyRunning) {
    Start-Process -FilePath 'node' -ArgumentList @((Join-Path $editorRoot 'server.mjs'), $editorPort) -WorkingDirectory $editorRoot -WindowStyle Hidden
    for ($attempt = 0; $attempt -lt 20; $attempt++) {
        Start-Sleep -Milliseconds 150
        try {
            $response = Invoke-WebRequest -Uri $editorUrl -UseBasicParsing -TimeoutSec 1
            if ($response.StatusCode -eq 200) { break }
        } catch { }
    }
}
Start-Process $editorUrl
