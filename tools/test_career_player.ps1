param([string]$Executable,[int]$TimeoutSeconds=180)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if(!$Executable){$Executable=Join-Path $root 'builds/EungyeTaxi/EungyeTaxi.exe'}
$output=Join-Path $root 'artifacts/career-player'
New-Item -ItemType Directory -Force $output | Out-Null
foreach($name in @('campaign-3day20-v1.json','campaign-3day20-v1.json.bak','active-career-slot.txt','active-career-slot.txt.bak')){
    $path=Join-Path $output $name
    if(Test-Path -LiteralPath $path){Remove-Item -LiteralPath $path}
}
foreach($mode in @('careerCheck','careerReload')){
    $log=Join-Path $output "$mode.log"
    $argsList=@("-$mode",'-force-d3d11','-screen-width','1600','-screen-height','900','-logFile',('"' + $log + '"'))
    $process=Start-Process -FilePath $Executable -ArgumentList $argsList -WorkingDirectory $root -WindowStyle Hidden -PassThru
    if(!$process.WaitForExit($TimeoutSeconds*1000)){$process.Kill();throw "Career Player timeout: $log"}
    $expected=if($mode -eq 'careerCheck'){'CAREER_PLAYER_OK checks='}else{'CAREER_RELOAD_PLAYER_OK checks='}
    if($process.ExitCode -ne 0 -or (Get-Content $log -Raw) -notmatch $expected){
        Get-Content $log | Select-String 'SHIFT_PLAYER_FAIL|Exception'
        throw "Career Player failed ($($process.ExitCode)): $log"
    }
    Get-Content $log | Select-String $expected
}
