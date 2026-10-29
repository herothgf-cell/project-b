param([string]$Executable)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskExe=if($Executable){[IO.Path]::GetFullPath($Executable)}else{Join-Path $taskRoot 'builds/PvpPrototype/PvpPrototype.exe'}
$taskLog=Join-Path $taskRoot 'artifacts/pvp/expanded-player.log'
$taskProcess=Start-Process $taskExe -ArgumentList @('-batchmode','-nographics','-pvpExpandedCheck','-logFile',('"'+$taskLog+'"')) -WorkingDirectory (Split-Path $taskExe -Parent) -WindowStyle Hidden -PassThru
try{
 if(!$taskProcess.WaitForExit(320000)){throw 'Expanded Player timeout'}
 if($taskProcess.ExitCode -ne 0){throw ('Expanded Player exit='+$taskProcess.ExitCode+' '+$taskLog)}
 foreach($taskMarker in @('PVP_EXPANDED_PLAYER_OK','PVP_EXPANDED_DRIVING player=0','PVP_EXPANDED_DRIVING player=1')){if(!(Select-String -LiteralPath $taskLog -SimpleMatch -Pattern $taskMarker -Quiet)){throw ('Missing '+$taskMarker)}}
 if(Select-String -LiteralPath $taskLog -Pattern 'PVP_EXPANDED_PLAYER_FAIL|NullReferenceException|error CS' -Quiet){throw 'Expanded Player error'}
 Select-String -LiteralPath $taskLog -Pattern '^PVP_EXPANDED_' | ForEach-Object {$_.Line}
}finally{if(!$taskProcess.HasExited){Stop-Process -Id $taskProcess.Id -ErrorAction SilentlyContinue}}
Write-Output 'PVP_EXPANDED_RUNNER_OK'
