param([string]$Executable,[string]$OutputDirectory,[switch]$Pvp,[switch]$Capture,[switch]$Visible,[int]$TimeoutSeconds=240)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskExe=if($Executable){[IO.Path]::GetFullPath($Executable)}else{Join-Path $taskRoot 'builds/EungyeTaxi/EungyeTaxi.exe'}
$taskOut=if($OutputDirectory){[IO.Path]::GetFullPath($OutputDirectory)}else{Join-Path $taskRoot ('artifacts/driving-settings-player'+$(if($Pvp){'-pvp'}else{''}))}
New-Item -ItemType Directory -Path $taskOut -Force | Out-Null
$taskLog=Join-Path $taskOut 'player.log'
$taskArgs=@('-contentTest','-contentProfile',('"'+(Join-Path $taskOut 'no-profile.json')+'"'),'-drivingSettingsCheck','-drivingSettingsOutput',('"'+$taskOut+'"'),'-logFile',('"'+$taskLog+'"'))
if($Pvp){$taskArgs+=@('-drivingSettingsPvp','-pvp')}else{$taskArgs+='-singlePlayer'}
if($Capture){$taskArgs+=@('-drivingSettingsCapture','-screen-fullscreen','0','-screen-width','1280','-screen-height','720')}else{$taskArgs+=@('-batchmode','-nographics')}
$taskProcess=Start-Process -FilePath $taskExe -ArgumentList $taskArgs -WorkingDirectory $taskRoot -WindowStyle $(if($Visible){'Normal'}else{'Hidden'}) -PassThru
try{
 if(!$taskProcess.WaitForExit($TimeoutSeconds*1000)){throw 'Driving settings runtime timeout'}
 if($taskProcess.ExitCode -ne 0){throw "Driving settings exit=$($taskProcess.ExitCode) $taskLog"}
 $taskResult=Get-Content -Raw (Join-Path $taskOut 'result.json') | ConvertFrom-Json
 if(!$taskResult.success -or !(Select-String -Path $taskLog -SimpleMatch 'DRIVING_SETTINGS_PLAYER_OK' -Quiet)){throw "Driving settings runtime failed: $taskLog"}
 Write-Output "DRIVING_SETTINGS_PLAYER_OK checks=$($taskResult.checks) pvp=$Pvp capture=$Capture"
}finally{if(!$taskProcess.HasExited){Stop-Process -Id $taskProcess.Id}}
