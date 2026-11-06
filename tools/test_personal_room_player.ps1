param([string]$Executable,[int]$TimeoutSeconds=180,[switch]$Visible)
$ErrorActionPreference='Stop'
$roomRoot=Split-Path $PSScriptRoot -Parent
if(!$Executable){$Executable=Join-Path $roomRoot 'builds/EungyeTaxi/EungyeTaxi.exe'}
$roomOutput=Join-Path $roomRoot 'artifacts/personal-room-player'
New-Item -ItemType Directory -Force $roomOutput | Out-Null
# Only these fixture-owned files are reset; no player profile or campaign is touched.
foreach($roomName in @('campaign.json','campaign.json.bak')){ $roomPath=Join-Path $roomOutput $roomName;if(Test-Path -LiteralPath $roomPath){Remove-Item -LiteralPath $roomPath} }
foreach($roomMode in @('personalRoomCheck','personalRoomReloadCheck')){
 $roomLog=Join-Path $roomOutput "$roomMode.log"
 $roomArgs=@('-contentTest',"-$roomMode",'-screen-width','1600','-screen-height','900','-logFile',('"'+$roomLog+'"'))
 $roomStyle=if($Visible){'Normal'}else{'Hidden'}
 $roomProcess=Start-Process -FilePath $Executable -ArgumentList $roomArgs -WorkingDirectory $roomRoot -WindowStyle $roomStyle -PassThru
 $roomWatch=[Diagnostics.Stopwatch]::StartNew()
 while(!$roomProcess.WaitForExit(1000)){if($roomWatch.Elapsed.TotalSeconds -gt $TimeoutSeconds){$roomProcess.Kill();throw "Personal room player timeout: $roomLog"}}
 if($roomProcess.ExitCode -ne 0){throw "Personal room player exit $($roomProcess.ExitCode): $roomLog"}
 $roomMarker=if($roomMode -eq 'personalRoomCheck'){'PERSONAL_ROOM_PLAYER_OK checks='}else{'PERSONAL_ROOM_RELOAD_OK checks='}
 if((Get-Content $roomLog -Raw) -notmatch [regex]::Escape($roomMarker)){throw "Missing acceptance marker: $roomLog"}
 Write-Output "Personal room player PASS $roomMode; $roomLog"
}
