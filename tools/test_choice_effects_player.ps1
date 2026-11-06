param([string]$Executable,[int]$TimeoutSeconds=240,[switch]$Visible)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
if(!$Executable){$Executable=Join-Path $taskRoot 'builds/EungyeTaxi/EungyeTaxi.exe'}
if(!(Test-Path -LiteralPath $Executable)){throw 'Build the unified Eungye player first'}
$taskOutput=Join-Path $taskRoot 'artifacts/choice-effects-player'
New-Item -ItemType Directory -Force -Path $taskOutput | Out-Null
$taskLog=Join-Path $taskOutput 'player.log'
$taskArgs=@('-contentTest','-choiceEffectsPlayerCheck','-logFile',('"'+$taskLog+'"'))
if(!$Visible){$taskArgs+=@('-batchmode','-nographics')}
$taskWindowStyle=if($Visible){'Normal'}else{'Hidden'}
$taskStarted=[DateTime]::UtcNow
$taskProcess=Start-Process -FilePath $Executable -ArgumentList $taskArgs -WorkingDirectory $taskRoot -WindowStyle $taskWindowStyle -PassThru
$taskWatch=[Diagnostics.Stopwatch]::StartNew()
while(!$taskProcess.WaitForExit(1000)){if($taskWatch.Elapsed.TotalSeconds -gt $TimeoutSeconds){$taskProcess.Kill();throw "Choice effects player timed out; see $taskLog"}}
if($taskProcess.ExitCode -ne 0){throw "Choice effects player exit $($taskProcess.ExitCode); see $taskLog"}
if(!(Test-Path -LiteralPath $taskLog) -or (Get-Content -LiteralPath $taskLog -Raw) -notmatch 'CHOICE_EFFECTS_PLAYER_OK checks='){throw "Choice effects acceptance success marker missing: $taskLog"}
Write-Output "Choice effects player PASS ($([Math]::Round($taskWatch.Elapsed.TotalSeconds,1)) seconds); $taskLog"
