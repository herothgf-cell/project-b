param([string]$Executable,[string]$OutputDirectory,[switch]$Pvp,[switch]$Reload,[switch]$Capture,[int]$TimeoutSeconds=210)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskExe=if($Executable){[IO.Path]::GetFullPath($Executable)}else{Join-Path $taskRoot 'builds/EungyeTaxi/EungyeTaxi.exe'}
$taskMode=if($Pvp){'pvp'}elseif($Reload){'reload'}else{'single'}
$taskOut=if($OutputDirectory){[IO.Path]::GetFullPath($OutputDirectory)}else{Join-Path $taskRoot ('artifacts/accident-workshop/'+$taskMode)}
New-Item -ItemType Directory -Force -Path $taskOut | Out-Null
$taskLog=Join-Path $taskOut 'player.log'
$taskFlag=if($Pvp){'-pvpAccidentWorkshopCheck'}elseif($Reload){'-accidentWorkshopReloadCheck'}else{'-accidentWorkshopCheck'}
$taskArgs=@($taskFlag,'-accidentWorkshopOutput',('"'+$taskOut+'"'),'-logFile',('"'+$taskLog+'"'))
if(!$Pvp){$taskArgs+='-singlePlayer'}
if($Capture){$taskArgs+=@('-accidentWorkshopCapture','-screen-fullscreen','0','-screen-width','1280','-screen-height','720')}else{$taskArgs+=@('-batchmode','-nographics')}
$taskProcess=Start-Process -FilePath $taskExe -ArgumentList $taskArgs -WorkingDirectory $taskRoot -WindowStyle Hidden -PassThru
try{
 if(!$taskProcess.WaitForExit($TimeoutSeconds*1000)){throw 'Accident workshop runtime timeout'}
 if($taskProcess.ExitCode -ne 0){throw ('Accident workshop exit='+$taskProcess.ExitCode+' '+$taskLog)}
 $taskResult=Get-Content -Raw (Join-Path $taskOut 'result.json') | ConvertFrom-Json
 if(!$taskResult.success -or !(Select-String -Path $taskLog -SimpleMatch 'ACCIDENT_WORKSHOP_PLAYER_OK' -Quiet)){throw ('Runtime acceptance failed: '+$taskResult.error)}
 if(Select-String -Path $taskLog -Pattern 'NullReferenceException|ACCIDENT_WORKSHOP FAIL' -Quiet){throw 'Runtime errors in player log'}
 $taskResult.assertions
}finally{if(!$taskProcess.HasExited){Stop-Process -Id $taskProcess.Id}}
Write-Output ('ACCIDENT_WORKSHOP_RUNNER_OK mode='+$taskMode)
