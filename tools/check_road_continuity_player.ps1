param([string]$Root = (Split-Path $PSScriptRoot -Parent))
$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath($Root)
$taskShadow=Join-Path $taskRoot 'artifacts/color-road-survey/road-continuity-player'
$taskManifest=Get-Content (Join-Path $taskRoot 'distribution/EungyeTaxi-build-manifest.json') -Raw | ConvertFrom-Json
foreach($file in $taskManifest.files){
    $destination=Join-Path $taskShadow $file.path
    New-Item -ItemType Directory -Path (Split-Path $destination) -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $taskRoot $file.path) -Destination $destination -Force
}
$taskPlayer=Join-Path $taskShadow 'builds/EungyeTaxi/EungyeTaxi.exe'
$taskLog=Join-Path $taskRoot 'artifacts/color-road-survey/continuity-grid-player.log'
$taskProcess=Start-Process -FilePath $taskPlayer -ArgumentList @('-batchmode','-force-d3d11','-presentationTest','-roadContinuityViews','-logFile',$taskLog) -WorkingDirectory $taskShadow -WindowStyle Hidden -PassThru
if(!$taskProcess.WaitForExit(600000)){Stop-Process -Id $taskProcess.Id;throw 'Road screen capture timed out'}
if($taskProcess.ExitCode -ne 0 -or !(Select-String -LiteralPath $taskLog -Quiet -Pattern 'ROAD_SIGNAGE_PLAYER_OK')){throw 'Road screen capture failed'}
$taskCoverage=Get-Content (Join-Path $taskShadow 'artifacts/road-continuity-grid/coverage.json') -Raw | ConvertFrom-Json
if(!$taskCoverage.allRoadsCovered){throw 'Incomplete road capture coverage'}
Get-Content (Join-Path $taskShadow 'artifacts/road-signage-player.txt') | Select-Object -Last 8
