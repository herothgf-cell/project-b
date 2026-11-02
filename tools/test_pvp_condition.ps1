param([string]$Executable,[string]$OutputDirectory,[switch]$Capture,[switch]$Drive,[int]$TimeoutSeconds=660)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskExe=if($Executable){[IO.Path]::GetFullPath($Executable)}else{Join-Path $taskRoot 'builds/EungyeTaxi/EungyeTaxi.exe'}
$taskOut=if($OutputDirectory){[IO.Path]::GetFullPath($OutputDirectory)}else{Join-Path $taskRoot 'artifacts/pvp/condition-player'}
New-Item -ItemType Directory -Force -Path $taskOut | Out-Null
$taskLog=Join-Path $taskOut 'player.log'
$taskArgs=@('-pvpConditionCheck','-pvpConditionOutput',('"'+$taskOut+'"'),'-logFile',('"'+$taskLog+'"'))
if($Drive){$taskArgs+='-pvpConditionDriveCheck'}
if($Capture){$taskArgs+=@('-pvpConditionCaptureOutput',('"'+$taskOut+'"'),'-screen-fullscreen','0','-screen-width','1600','-screen-height','900')}else{$taskArgs+=@('-batchmode','-nographics')}
$taskStyle=if($Capture){'Normal'}else{'Hidden'}
$taskProcess=Start-Process -FilePath $taskExe -ArgumentList $taskArgs -WorkingDirectory (Split-Path $taskExe -Parent) -WindowStyle $taskStyle -PassThru
try{
 if(!$taskProcess.WaitForExit($TimeoutSeconds*1000)){throw 'PVP condition check timeout'}
 if($taskProcess.ExitCode -ne 0){throw ('PVP condition exit='+$taskProcess.ExitCode+' '+$taskLog)}
 if(!(Select-String -Path $taskLog -SimpleMatch 'PVP_CONDITION_PLAYER_OK' -Quiet)){throw 'Missing PVP condition acceptance marker'}
 if(Select-String -Path $taskLog -Pattern 'PVP_CONDITION FAIL|PVP_CONDITION_EXCEPTION|NullReferenceException' -Quiet){throw 'PVP condition failure in log'}
 Select-String -Path $taskLog -Pattern '^PVP_CONDITION' | ForEach-Object {$_.Line}
}finally{if(!$taskProcess.HasExited){Stop-Process -Id $taskProcess.Id}}
Write-Output 'PVP_CONDITION_RUNNER_OK'
