param([string]$Executable,[int]$TimeoutSeconds=180)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if(!$Executable){$Executable=Join-Path $root 'builds/EungyeTaxi/EungyeTaxi.exe'}
$output=Join-Path $root ('artifacts/daily-condition-player/'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $output -Force | Out-Null
foreach($stage in @('dailyConditionCheck','dailyConditionReloadCheck')){
    $log=Join-Path $output "$stage.log"
    $argsList=@("-$stage",'-force-d3d11','-screen-width','1600','-screen-height','900',
        '-shiftCaptureOutput',('"' + $output + '"'),'-logFile',('"' + $log + '"'))
    $process=Start-Process -FilePath $Executable -ArgumentList $argsList -WorkingDirectory $root -WindowStyle Hidden -PassThru
    if(!$process.WaitForExit($TimeoutSeconds*1000)){$process.Kill();throw "Daily condition Player timeout in $stage. $log"}
    Get-Content $log | Select-String 'DAILY_CONDITION_PLAYER_OK|SHIFT_PLAYER_(PASS|FAIL)|^Exception:'
    if($process.ExitCode -ne 0 -or (Get-Content $log -Raw) -notmatch 'DAILY_CONDITION_PLAYER_OK checks='){
        throw "Daily condition Player failed in $stage ($($process.ExitCode)). $log"
    }
}
