param([string]$Executable,[int]$TimeoutSeconds=200,[switch]$Visible)
$ErrorActionPreference='Stop'
$garageRoot=Split-Path $PSScriptRoot -Parent
if(!$Executable){$Executable=Join-Path $garageRoot 'builds/EungyeTaxi/EungyeTaxi.exe'}
$garageOutput=Join-Path $garageRoot 'artifacts/vehicle-garage-player'
New-Item -ItemType Directory -Force $garageOutput | Out-Null
# Only these fixture-owned files are reset; no player profile, campaign or PVP unlock file is touched.
foreach($garageName in @('campaign.json','campaign.json.bak','pvp-vehicle-unlocks.json')){ $garagePath=Join-Path $garageOutput $garageName;if(Test-Path -LiteralPath $garagePath){Remove-Item -LiteralPath $garagePath} }
foreach($garageMode in @('vehicleGarageCheck','vehicleGarageReloadCheck','pvpVehicleLookCheck')){
 $garageLog=Join-Path $garageOutput "$garageMode.log"
 $garageArgs=@('-contentTest',"-$garageMode",'-screen-width','1600','-screen-height','900','-logFile',('"'+$garageLog+'"'))
 # PVP reads the unlock file the single-player run wrote beside its isolated save (read-only).
 if($garageMode -eq 'pvpVehicleLookCheck'){$garageArgs+=@('-vehicleUnlocks',('"'+(Join-Path $garageOutput 'pvp-vehicle-unlocks.json')+'"'),'-pvpVehicleLook','lpg:1:1')}
 $garageStyle=if($Visible){'Normal'}else{'Hidden'}
 $garageProcess=Start-Process -FilePath $Executable -ArgumentList $garageArgs -WorkingDirectory $garageRoot -WindowStyle $garageStyle -PassThru
 $garageWatch=[Diagnostics.Stopwatch]::StartNew()
 while(!$garageProcess.WaitForExit(1000)){if($garageWatch.Elapsed.TotalSeconds -gt $TimeoutSeconds){$garageProcess.Kill();throw "Vehicle garage player timeout: $garageLog"}}
 if($garageProcess.ExitCode -ne 0){throw "Vehicle garage player exit $($garageProcess.ExitCode): $garageLog"}
 $garageMarker=switch($garageMode){'vehicleGarageCheck'{'VEHICLE_GARAGE_PLAYER_OK checks='}'vehicleGarageReloadCheck'{'VEHICLE_GARAGE_RELOAD_OK checks='}default{'PVP_VEHICLE_LOOK_OK checks='}}
 if((Get-Content $garageLog -Raw) -notmatch [regex]::Escape($garageMarker)){throw "Missing acceptance marker: $garageLog"}
 Write-Output "Vehicle garage player PASS $garageMode; $garageLog"
}
