param([string]$Executable,[int]$TimeoutSeconds=240,[switch]$SignageOnly,[switch]$PerformanceOnly)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if(!$Executable){$Executable=Join-Path $root 'builds/EungyeTaxi/EungyeTaxi.exe'}
$folder=Join-Path $root 'artifacts/night-lighting-player'
New-Item -ItemType Directory -Path $folder -Force | Out-Null
function Invoke-NightCheck([string]$Name,[string[]]$Extra,[string]$Marker){
    $log=Join-Path $folder "$Name.log"
    $arguments=@('-batchmode','-force-d3d11','-screen-width','1600','-screen-height','900','-screen-fullscreen','0',
        '-dayLightNightProbe','-logFile',('"' + $log + '"'))+$Extra
    $proc=Start-Process -FilePath $Executable -ArgumentList $arguments -WorkingDirectory $root -WindowStyle Hidden -PassThru
    if(!$proc.WaitForExit($TimeoutSeconds*1000)){$proc.Kill();throw "Night Player timed out. $log"}
    if($proc.ExitCode -ne 0 -or (Get-Content $log -Raw) -notmatch $Marker){throw "Night Player failed ($($proc.ExitCode)). $log"}
    Get-Content $log | Select-String $Marker,'DAYLIGHT_SINGLE_PERF','DAYLIGHT_READY'
}
if(!$PerformanceOnly){Invoke-NightCheck 'night-signage' @('-presentationTest') 'ROAD_SIGNAGE_PLAYER_OK'}
if(!$SignageOnly){Invoke-NightCheck 'night-performance' @('-contentTest','-dayLightFrameCheck') 'DAYLIGHT_PLAYER_OK'}
