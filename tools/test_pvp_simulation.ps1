param([string]$Executable,[switch]$Expanded,[switch]$Services,[switch]$Regression,[int]$TimeoutSeconds=620)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskExe=if($Executable){[IO.Path]::GetFullPath($Executable)}else{Join-Path $taskRoot 'builds/PvpPrototype/PvpPrototype.exe'}
$taskTag=if($Services){'services'}elseif($Expanded){'city'}elseif($Regression){'eungye-regression'}else{'eungye'}
$taskFolder=Join-Path $taskRoot 'artifacts/pvp'
New-Item -ItemType Directory -Force $taskFolder | Out-Null
$taskLog=Join-Path $taskFolder ('simulation-'+$taskTag+'-player.log')
$taskMode=if($Services){'-pvpSimulationServicesCheck'}else{'-pvpSimulationCheck'}
$taskArgs=@('-batchmode','-nographics',$taskMode,'-pvpSimulationOutput',('"'+$taskFolder+'"'),'-logFile',('"'+$taskLog+'"'))
if($Expanded){$taskArgs+='-pvpSimulationExpanded'}
if($Regression){$taskArgs+='-pvpSimulationEungyeRegression'}
$taskProcess=Start-Process $taskExe -ArgumentList $taskArgs -WorkingDirectory (Split-Path $taskExe -Parent) -WindowStyle Hidden -PassThru
try{
 if(!$taskProcess.WaitForExit($TimeoutSeconds*1000)){throw ('Simulation timeout '+$taskLog)}
 if($taskProcess.ExitCode -ne 0){throw ('Simulation exit='+$taskProcess.ExitCode+' '+$taskLog)}
 $taskMarker=if($Services){'PVP_SIMULATION_SERVICES_OK'}else{'PVP_SIMULATION_DONE success=True'}
 if(!(Select-String -LiteralPath $taskLog -SimpleMatch $taskMarker -Quiet)){throw ('Missing acceptance marker '+$taskLog)}
 if(Select-String -LiteralPath $taskLog -Pattern 'NullReferenceException|Exception:|PVP_SIMULATION FAIL' -Quiet){throw ('Simulation error '+$taskLog)}
 Select-String -LiteralPath $taskLog -Pattern '^PVP_SIMULATION' | ForEach-Object {$_.Line}
}finally{if(!$taskProcess.HasExited){Stop-Process -Id $taskProcess.Id -ErrorAction SilentlyContinue}}
Write-Output ('PVP_SIMULATION_RUNNER_OK '+$taskTag)

