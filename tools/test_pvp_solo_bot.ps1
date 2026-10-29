param([ValidateSet('all','baseline','violation')][string]$Case='all',[string]$Executable)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskExe=if($Executable){[IO.Path]::GetFullPath($Executable)}else{Join-Path $taskRoot 'builds/PvpPrototype/PvpPrototype.exe'}
$taskLogs=Join-Path $taskRoot 'artifacts/pvp'
New-Item -ItemType Directory -Path $taskLogs -Force | Out-Null
foreach($taskCase in @('baseline','violation')){
 if($Case -ne 'all' -and $Case -ne $taskCase){continue}
 $taskLog=Join-Path $taskLogs ('solo-bot-'+$taskCase+'.log')
 $taskArgs=@('-batchmode','-nographics','-pvpSoloBotCheck','-logFile',('"'+$taskLog+'"'))
 if($taskCase -eq 'violation'){$taskArgs+='-pvpSoloViolationCheck'}
 $taskProcess=Start-Process $taskExe -ArgumentList $taskArgs -WorkingDirectory (Split-Path $taskExe -Parent) -WindowStyle Hidden -PassThru
 try{
  if(!$taskProcess.WaitForExit(210000)){throw ('Solo bot timeout: '+$taskCase)}
  if($taskProcess.ExitCode -ne 0){throw ('Solo bot exit='+$taskProcess.ExitCode+' log='+$taskLog)}
  foreach($taskMarker in @('PVP_SOLO_BOT_PLAYER_OK','PVP_SOLO_BOT_RESULT','PVP_SOLO_BOT_DRIVING_PROOF player=0','PVP_SOLO_BOT_DRIVING_PROOF player=1')){if(!(Select-String -LiteralPath $taskLog -SimpleMatch -Pattern $taskMarker -Quiet)){throw ('Missing '+$taskMarker)}}
  if(Select-String -LiteralPath $taskLog -Pattern 'PVP_NETWORK_STARTED|NullReferenceException|error CS|PVP_SOLO_BOT_PLAYER_FAIL' -Quiet){throw 'Local probe opened transport or reported an error'}
  Select-String -LiteralPath $taskLog -Pattern '^PVP_SOLO_BOT_(RESULT|DRIVING_PROOF|PLAYER_OK)' | ForEach-Object {$_.Line}
 }finally{if(!$taskProcess.HasExited){Stop-Process -Id $taskProcess.Id -ErrorAction SilentlyContinue}}
}
Write-Output 'PVP_SOLO_BOT_RUNNER_OK'
