param([int]$TimeoutSeconds=240)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskExe=Join-Path $taskRoot 'builds/EungyeTaxi/EungyeTaxi.exe'
$taskArtifacts=Join-Path $taskRoot 'artifacts/passenger-traits'
New-Item -ItemType Directory -Force -Path $taskArtifacts | Out-Null
if (!(Test-Path -LiteralPath $taskExe)) {throw 'Build the current hybrid player first'}
$taskLog=Join-Path $taskArtifacts 'player.log'
$taskReport=Join-Path $taskArtifacts 'player.txt'
$taskArgs=@('-oneDayMode','-hybridV2','-passengerBehaviorTest','-passengerTraitsTest','-batchmode','-nographics','-logFile',('"'+$taskLog+'"'))
$taskStarted=[DateTime]::UtcNow
$taskProcess=Start-Process -FilePath $taskExe -ArgumentList $taskArgs -WorkingDirectory $taskRoot -WindowStyle Hidden -PassThru
$taskWatch=[Diagnostics.Stopwatch]::StartNew()
while (!$taskProcess.WaitForExit(1000)) {
 if ($taskWatch.Elapsed.TotalSeconds -gt $TimeoutSeconds) {$taskProcess.Kill();throw "Passenger traits player timed out; stopped only this test process. See $taskLog"}
}
if ($taskProcess.ExitCode -ne 0) {throw "Passenger traits player exit $($taskProcess.ExitCode); see $taskLog"}
if (!(Test-Path -LiteralPath $taskReport) -or (Get-Item -LiteralPath $taskReport).LastWriteTimeUtc -lt $taskStarted) {throw 'No fresh passenger traits report'}
if ((Get-Content -LiteralPath $taskReport -Raw) -notmatch '^PASS') {throw "Passenger traits report failed: $taskReport"}
Write-Output "Passenger traits player exit0 ($([Math]::Round($taskWatch.Elapsed.TotalSeconds,1)) seconds)"
Get-Content -LiteralPath $taskReport -TotalCount 1
