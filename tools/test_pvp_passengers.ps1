param([string]$Executable,[string]$OutputDirectory,[int]$TimeoutSeconds=420)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskExe=if($Executable){[IO.Path]::GetFullPath($Executable)}else{Join-Path $taskRoot 'builds/EungyeTaxi/EungyeTaxi.exe'}
$taskOut=if($OutputDirectory){[IO.Path]::GetFullPath($OutputDirectory)}else{Join-Path $taskRoot 'artifacts/pvp/passenger-player'}
New-Item -ItemType Directory -Force -Path $taskOut | Out-Null
$taskLog=Join-Path $taskOut 'player.log'
$taskArgs=@('-batchmode','-nographics','-pvpPassengerCheck','-pvpPassengerOutput',('"'+$taskOut+'"'),'-logFile',('"'+$taskLog+'"'))
$taskProcess=Start-Process $taskExe -ArgumentList $taskArgs -WorkingDirectory (Split-Path $taskExe -Parent) -WindowStyle Hidden -PassThru
try{if(!$taskProcess.WaitForExit($TimeoutSeconds*1000)){throw 'Passenger check timeout'};if($taskProcess.ExitCode -ne 0){throw ('Passenger exit='+$taskProcess.ExitCode+' '+$taskLog)};if(!(Select-String -LiteralPath $taskLog -SimpleMatch 'PVP_PASSENGER_PLAYER_OK' -Quiet)){throw 'Missing passenger acceptance marker'};if(Select-String -LiteralPath $taskLog -Pattern 'PVP_PASSENGER FAIL|NullReferenceException|Exception:' -Quiet){throw 'Passenger failure in log'};Select-String -LiteralPath $taskLog -Pattern '^PVP_PASSENGER' | ForEach-Object{$_.Line}}finally{if(!$taskProcess.HasExited){Stop-Process -Id $taskProcess.Id}}
Write-Output 'PVP_PASSENGER_RUNNER_OK'
