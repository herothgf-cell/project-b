param([string]$Executable,[string]$OutputDirectory,[switch]$Pvp,[switch]$Capture,[switch]$Visible,[ValidateSet('all','map','navigation','popup','workshop')][string]$View='all',[int]$TimeoutSeconds=270)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskExe=if($Executable){[IO.Path]::GetFullPath($Executable)}else{Join-Path $taskRoot 'builds/EungyeTaxi/EungyeTaxi.exe'}
$taskOut=if($OutputDirectory){[IO.Path]::GetFullPath($OutputDirectory)}else{Join-Path $taskRoot 'artifacts/accident-severity-ui-player'}
New-Item -ItemType Directory -Force -Path $taskOut | Out-Null
$taskLog=Join-Path $taskOut 'player.log'
$taskFlag=if($Pvp){'-pvpAccidentSeverityUiCheck'}else{'-accidentSeverityUiCheck'}
$taskArgs=@('-contentTest','-contentProfile',('"'+(Join-Path $taskOut 'no-profile.json')+'"'),$taskFlag,'-accidentSeverityUiOutput',('"'+$taskOut+'"'),'-accidentSeverityView',$View,'-logFile',('"'+$taskLog+'"'))
if(!$Pvp){$taskArgs+='-singlePlayer'}
if($Capture){$taskArgs+=@('-accidentSeverityCapture','-screen-fullscreen','0','-screen-width','1280','-screen-height','720')}else{$taskArgs+=@('-batchmode','-nographics')}
$taskWindowStyle=if($Visible){'Normal'}else{'Hidden'}
$taskProcess=Start-Process -FilePath $taskExe -ArgumentList $taskArgs -WorkingDirectory $taskRoot -WindowStyle $taskWindowStyle -PassThru
try{
 if(!$taskProcess.WaitForExit($TimeoutSeconds*1000)){throw 'Accident severity UI runtime timeout'}
 if($taskProcess.ExitCode -ne 0){throw ('Accident severity UI exit='+$taskProcess.ExitCode+' '+$taskLog)}
 $taskResult=Get-Content -Raw (Join-Path $taskOut 'result.json') | ConvertFrom-Json
 if(!$taskResult.success -or !(Select-String -Path $taskLog -SimpleMatch 'ACCIDENT_SEVERITY_UI_OK' -Quiet)){throw ('Runtime acceptance failed: '+$taskResult.error)}
 if(Select-String -Path $taskLog -Pattern 'NullReferenceException|ACCIDENT_SEVERITY_UI FAIL ' -Quiet){throw 'Runtime errors in player log'}
 $taskResult.assertions
}finally{if(!$taskProcess.HasExited){Stop-Process -Id $taskProcess.Id}}
Write-Output ('ACCIDENT_SEVERITY_UI_RUNNER_OK capture='+$Capture+' view='+$View)
