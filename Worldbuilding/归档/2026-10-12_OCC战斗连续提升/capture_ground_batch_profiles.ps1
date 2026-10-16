$ErrorActionPreference='Stop'
$occProject='E:/数据库/OCC_Codex/UnityProject'
$occProfiles='E:/数据库/OCC_Codex/Worldbuilding/归档/2026-10-12_OCC战斗连续提升/profiles'
$occReadyPath=Join-Path $occProfiles 'AcademyCourt/cleanup.json'
$occReadyDeadline=(Get-Date).AddSeconds(85)
do {
 if(Test-Path -LiteralPath $occReadyPath){
  $occReady=Get-Content -LiteralPath $occReadyPath -Raw -Encoding utf8 | ConvertFrom-Json
  $occCourt=Get-Content -LiteralPath (Join-Path $occProfiles 'AcademyCourt/capture.json') -Raw -Encoding utf8 | ConvertFrom-Json
  if(-not $occReady.play -and $null -ne $occCourt.mappedGroundAttachments){break}
 }
 if((Get-Date)-gt $occReadyDeadline){throw 'Current AcademyCourt capture did not finish'}
 Start-Sleep -Milliseconds 500
} while($true)
foreach($occFamily in @('Conservatory','StoneRoad','RuinSite','InteriorHall','AetherTower')) {
 $occStarted=Get-Date
 $occResult=unity command menu --path "OCC/Tools/Capture battle profile/$occFamily (no focus)" --project-path $occProject --caller plugin --skill unity-cli --format json | ConvertFrom-Json
 if(-not $occResult.success){throw "Capture command rejected: $occFamily"}
 $occCleanup=Join-Path $occProfiles "$occFamily/cleanup.json"
 $occFailure=Join-Path $occProfiles "$occFamily/failure.json"
 $occDeadline=(Get-Date).AddSeconds(90)
 do {
  if((Test-Path -LiteralPath $occFailure)-and (Get-Item -LiteralPath $occFailure).LastWriteTime-gt $occStarted){throw "Capture failed: $occFamily"}
  if((Test-Path -LiteralPath $occCleanup)-and (Get-Item -LiteralPath $occCleanup).LastWriteTime-gt $occStarted){
   $occState=Get-Content -LiteralPath $occCleanup -Raw -Encoding utf8 | ConvertFrom-Json
   if($occState.play -or $occState.dirty -or -not $occState.pipelineRestored){throw "Restoration failed: $occFamily"}
   break
  }
  if((Get-Date)-gt $occDeadline){throw "Capture outcome not available: $occFamily; inspect state before any retry"}
  Start-Sleep -Milliseconds 500
 } while($true)
 $occCapture=Get-Content -LiteralPath (Join-Path $occProfiles "$occFamily/capture.json") -Raw -Encoding utf8 | ConvertFrom-Json
 if($null-eq $occCapture.mappedGroundAttachments){throw "Stale capture record: $occFamily"}
 Write-Output "$occFamily completed: $($occCapture.mappedGroundAttachments.Count) exact ground attachments"
}
