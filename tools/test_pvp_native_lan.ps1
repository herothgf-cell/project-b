param(
 [ValidateRange(1024,65000)][int]$Port=17971,
 [string]$Executable,
 [switch]$Reconnect,
 [ValidateRange(60,600)][int]$TimeoutSeconds=300
)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
if(-not $Executable){$Executable=Join-Path $root 'builds/EungyeTaxi/EungyeTaxi.exe'}
$Executable=(Resolve-Path -LiteralPath $Executable).Path
$runName='native-lan-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[Guid]::NewGuid().ToString('N').Substring(0,8)
$output=Join-Path (Join-Path $root 'artifacts/pvp') $runName
New-Item -ItemType Directory -Path $output -Force | Out-Null
$owned=[System.Collections.Generic.List[System.Diagnostics.Process]]::new()
function Start-NativeProbe([string]$Role){
 $log=Join-Path $output ($Role+'.log')
 $arguments=@('-batchmode','-nographics','-pvpNativeLanCheck','-pvpNativeLanRole',$Role,'-pvpNativeLanAddress','127.0.0.1','-pvpNativeLanPort',"$Port",'-pvpNativeLanOutput',('"'+$output+'"'),'-logFile',('"'+$log+'"'))
 if($Reconnect){$arguments+='-pvpNativeLanReconnect'}
 $process=Start-Process -FilePath $Executable -ArgumentList $arguments -WindowStyle Hidden -PassThru
 $owned.Add($process)
 return [pscustomobject]@{Role=$Role;Process=$process;Log=$log;Metrics=(Join-Path $output ($Role+'-metrics.json'))}
}
try {
 $hostRun=Start-NativeProbe 'host'
 Start-Sleep -Seconds 2
 $clientRun=Start-NativeProbe 'client'
 $deadline=[DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
 while((-not $hostRun.Process.HasExited -or -not $clientRun.Process.HasExited) -and [DateTime]::UtcNow -lt $deadline){
  if(($hostRun.Process.HasExited -and $hostRun.Process.ExitCode -ne 0) -or ($clientRun.Process.HasExited -and $clientRun.Process.ExitCode -ne 0)){break}
  Start-Sleep -Milliseconds 500
 }
 foreach($run in @($hostRun,$clientRun)){
  if(-not $run.Process.HasExited){throw ($run.Role+' did not finish; evidence: '+$output)}
  if($run.Process.ExitCode -ne 0){throw ($run.Role+' exit='+$run.Process.ExitCode+'; log: '+$run.Log)}
  if(-not (Test-Path -LiteralPath $run.Metrics)){throw ($run.Role+' missing JSON metrics: '+$run.Metrics)}
  $metrics=Get-Content -LiteralPath $run.Metrics -Raw | ConvertFrom-Json
  if(-not $metrics.passed -or $metrics.failed -ne 0 -or $metrics.checks -lt 10){throw ($run.Role+' metrics failed: '+$run.Metrics)}
  if(-not (Select-String -LiteralPath $run.Log -SimpleMatch ('PVP_NATIVE_LAN_OK '+$run.Role) -Quiet)){throw ($run.Role+' missing completion marker')}
  Write-Output ($run.Role+' PASS checks='+$metrics.checks+' native-map='+$metrics.map+' guest-metres='+$metrics.metres)
 }
 Write-Output ('PVP_NATIVE_LAN_TWO_PROCESS_OK reconnect='+[bool]$Reconnect+' evidence='+$output)
 Write-Output 'Scope: two loopback player processes using native input and WheelCollider authority; remote-PC LAN behavior requires separate verification.'
} finally {
 foreach($process in $owned){if(-not $process.HasExited){Stop-Process -Id $process.Id -ErrorAction SilentlyContinue}}
}
