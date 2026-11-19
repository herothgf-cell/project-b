param([string]$Executable,[int]$TimeoutSeconds=180)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if(!$Executable){$Executable=Join-Path $root 'builds/EungyeTaxi/EungyeTaxi.exe'}
$output=Join-Path $root 'artifacts/cabin-controls-player'
New-Item -ItemType Directory -Force $output | Out-Null
$log=Join-Path $output 'player.log'
$argsList=@('-cabinControlsCheck','-force-d3d11','-screen-width','1600','-screen-height','900',
    '-shiftCaptureOutput',('"' + $output + '"'),'-logFile',('"' + $log + '"'))
$process=Start-Process -FilePath $Executable -ArgumentList $argsList -WorkingDirectory $root -WindowStyle Hidden -PassThru
if(!$process.WaitForExit($TimeoutSeconds*1000)){$process.Kill();throw "Cabin controls Player timeout: $log"}
Get-Content $log | Select-String 'CABIN_PLAYER_OK|SHIFT_PLAYER_(PASS|FAIL)|Exception:'
if($process.ExitCode -ne 0 -or (Get-Content $log -Raw) -notmatch 'CABIN_PLAYER_OK checks='){
    throw "Cabin controls Player failed ($($process.ExitCode)): $log"
}
