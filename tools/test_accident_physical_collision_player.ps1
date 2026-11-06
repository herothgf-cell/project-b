param([string]$Executable,[string]$OutputDirectory,[int]$TimeoutSeconds=210)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskExe=if($Executable){[IO.Path]::GetFullPath($Executable)}else{Join-Path $taskRoot 'builds/EungyeTaxi/EungyeTaxi.exe'}
$taskOut=if($OutputDirectory){[IO.Path]::GetFullPath($OutputDirectory)}else{Join-Path $taskRoot 'artifacts/accident-physical-collision-player'}
New-Item -ItemType Directory -Force -Path $taskOut | Out-Null
$taskLog=Join-Path $taskOut 'player.log'
$taskArgs=@('-batchmode','-nographics','-singlePlayer','-contentTest','-contentProfile',('"'+(Join-Path $taskOut 'no-profile.json')+'"'),'-accidentPhysicalCollisionCheck','-accidentPhysicalCollisionOutput',('"'+$taskOut+'"'),'-logFile',('"'+$taskLog+'"'))
$taskProcess=Start-Process -FilePath $taskExe -ArgumentList $taskArgs -WorkingDirectory $taskRoot -WindowStyle Hidden -PassThru
try{
 if(!$taskProcess.WaitForExit($TimeoutSeconds*1000)){throw 'Real PhysX accident runtime timeout'}
 if($taskProcess.ExitCode -ne 0){throw ('Real PhysX accident exit='+$taskProcess.ExitCode+' '+$taskLog)}
 $taskResult=Get-Content -Raw (Join-Path $taskOut 'result.json') | ConvertFrom-Json
 if(!$taskResult.success -or !(Select-String -Path $taskLog -SimpleMatch 'ACCIDENT_PHYSX_OK' -Quiet)){throw ('Real PhysX acceptance failed: '+$taskResult.error)}
 if(Select-String -Path $taskLog -Pattern 'NullReferenceException|ACCIDENT_PHYSX FAIL ' -Quiet){throw 'Runtime errors in player log'}
 $taskResult.assertions
}finally{if(!$taskProcess.HasExited){Stop-Process -Id $taskProcess.Id}}
Write-Output 'ACCIDENT_PHYSX_RUNNER_OK'
