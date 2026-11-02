param([int]$TimeoutSeconds=180)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskExe=Join-Path $taskRoot 'builds/EungyeTaxi/EungyeTaxi.exe'
foreach($taskCase in @('passengerActivitiesTest','passengerActivitiesReloadTest')) {
 $taskLog=Join-Path $taskRoot "artifacts/$taskCase.log"
 $taskReport=Join-Path $taskRoot $(if($taskCase -eq 'passengerActivitiesTest'){'artifacts/passenger-activities-player.txt'}else{'artifacts/passenger-activities-reload.txt'})
 $taskStarted=[DateTime]::UtcNow
 $taskArgs=@('-oneDayMode','-hybridV2',"-$taskCase",'-batchmode','-nographics','-logFile',('"'+$taskLog+'"'))
 $taskProcess=Start-Process -FilePath $taskExe -ArgumentList $taskArgs -WorkingDirectory $taskRoot -WindowStyle Hidden -PassThru
 if(!$taskProcess.WaitForExit($TimeoutSeconds*1000)){$taskProcess.Kill();throw "$taskCase timed out; stopped test process"}
 if($taskProcess.ExitCode -ne 0){throw "$taskCase exit $($taskProcess.ExitCode); see $taskLog"}
 if(!(Test-Path -LiteralPath $taskReport) -or (Get-Item -LiteralPath $taskReport).LastWriteTimeUtc -lt $taskStarted -or (Get-Content -LiteralPath $taskReport -Raw) -notmatch '^PASS'){throw "Missing fresh PASS report: $taskReport"}
 Write-Output "$taskCase exit0"
}
