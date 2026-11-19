param([string]$Executable,[int]$TimeoutSeconds=180)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if(!$Executable){$Executable=Join-Path $root 'builds/EungyeTaxi/EungyeTaxi.exe'}
$output=Join-Path $root 'artifacts/home-base-player'
New-Item -ItemType Directory -Force $output | Out-Null
$log=Join-Path $output 'player.log'
$argsList=@('-homeBaseCheck','-force-d3d11','-screen-width','1600','-screen-height','900',
    '-shiftCaptureOutput',('"' + $output + '"'),'-logFile',('"' + $log + '"'))
$process=Start-Process -FilePath $Executable -ArgumentList $argsList -WorkingDirectory $root -WindowStyle Hidden -PassThru
if(!$process.WaitForExit($TimeoutSeconds*1000)){$process.Kill();throw "Home base Player timeout: $log"}
if($process.ExitCode -ne 0 -or (Get-Content $log -Raw) -notmatch 'HOME_BASE_PLAYER_OK checks='){
    throw "Home base Player failed ($($process.ExitCode)): $log"
}
Get-Content $log | Select-String 'HOME_BASE_PLAYER_OK|SHIFT_PLAYER_PASS'
