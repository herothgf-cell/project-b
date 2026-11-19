param([string]$Executable, [switch]$PreviousRoute, [int]$TimeoutSeconds=900)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if(!$Executable){$Executable=Join-Path $root 'builds/EungyeTaxi/EungyeTaxi.exe'}
$run=New-Item -ItemType Directory -Force (Join-Path $root ('artifacts/first-fare/' + [guid]::NewGuid().ToString('N')))
$output=New-Item -ItemType Directory (Join-Path $run.FullName 'artifacts')
$log=Join-Path $run.FullName 'player.log'
$arguments=@('-batchmode','-nographics','-contentTest','-oneDayTrafficTest','-logFile',('"'+$log+'"'))
if($PreviousRoute){$arguments+='-oneDayMode','-firstFareRoute3038'}else{$arguments+='-firstFareCheck'}
$process=Start-Process -FilePath $Executable -ArgumentList $arguments -WorkingDirectory $run.FullName -WindowStyle Hidden -PassThru
Write-Output "Player evidence: $($run.FullName)"
if(!$process.WaitForExit($TimeoutSeconds*1000)){
    $process.Kill()
    throw "First fare Player timeout: $log"
}
$report=Get-Content -LiteralPath (Join-Path $output.FullName 'one-day-traffic-drive.txt') -Raw
($report -split '\r?\n') | Where-Object {$_ -match '^(PASS|FAIL|RESULT|FARE|OFFER|DROP|LATE_DROP|GARAGE_|DRIVING_AUDIT)'}
if($process.ExitCode -ne 0 -or $report -notmatch 'RESULT success=True'){throw "First fare Player failed: $log"}
$expected=if($PreviousRoute){'LATE_DROP settled=True'}else{'PASS save settled first shift'}
if(!$report.Contains($expected)){throw "Missing expected outcome '$expected': $log"}
