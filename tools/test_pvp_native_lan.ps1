param(
 [ValidateRange(1024,65000)][int]$Port=17971,
 [string]$Executable,
 [switch]$Relay,
 [switch]$Reconnect,
 [switch]$ChoiceEffects,
 [switch]$MotionOnly,
 [ValidateRange(0,60)][int]$MotionSeconds=0,
 [switch]$Rendered,
 [switch]$RenderBoth,
 [ValidateRange(800,3840)][int]$RenderWidth=1280,
 [ValidateRange(600,2160)][int]$RenderHeight=720,
 [switch]$LegacyRouting,
 [ValidateRange(60,600)][int]$TimeoutSeconds=300,
 [string]$ContentHostProfile,
 [string]$ContentClientProfile,
 [ValidatePattern('^$|^[a-f0-9]{64}$')][string]$ExpectedContentHash=''
)
$ErrorActionPreference='Stop'
if($RenderBoth -and -not $Rendered){throw 'RenderBoth requires Rendered'}
if($MotionOnly -and ($MotionSeconds -lt 10 -or $Reconnect)){throw 'MotionOnly requires at least10 seconds and runs separately from Reconnect'}
if($ChoiceEffects -and ($MotionOnly -or !$ContentHostProfile)){throw 'ChoiceEffects requires a custom ContentHostProfile and the full model route'}
$root=Split-Path -Parent $PSScriptRoot
if(-not $Executable){$Executable=Join-Path $root 'builds/EungyeTaxi/EungyeTaxi.exe'}
$Executable=(Resolve-Path -LiteralPath $Executable).Path
$transport=if($Relay){'relay'}else{'lan'}
$runName='native-'+$transport+'-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[Guid]::NewGuid().ToString('N').Substring(0,8)
$output=Join-Path (Join-Path $root 'artifacts/pvp') $runName
New-Item -ItemType Directory -Path $output -Force | Out-Null
$owned=[System.Collections.Generic.List[System.Diagnostics.Process]]::new()
function Start-NativeProbe([string]$Role){
 $log=Join-Path $output ($Role+'.log')
 $arguments=@('-batchmode','-nographics','-pvpNativeLanCheck','-pvpNativeLanRole',$Role,'-pvpNativeLanAddress','127.0.0.1','-pvpNativeLanPort',"$Port",'-pvpNativeLanOutput',('"'+$output+'"'),'-logFile',('"'+$log+'"'))
 # LAN v17 vehicle looks: host pins the default taxi; the guest owns and selects the wide LPG look.
 if($Role -eq 'host'){$arguments+=@('-pvpVehicleLook','starter:0:0')}else{$unlocks=Join-Path $output 'client-vehicle-unlocks.json';'{"version":1,"models":["lpg"],"colors":["lpg:1"],"ornament":true}' | Set-Content -LiteralPath $unlocks -Encoding utf8NoBOM;$arguments+=@('-vehicleUnlocks',('"'+$unlocks+'"'),'-pvpVehicleLook','lpg:1:1')}
 $contentProfile=if($Role -eq 'host'){$ContentHostProfile}else{$ContentClientProfile}
 if($contentProfile){$contentProfile=(Resolve-Path -LiteralPath $contentProfile).Path;$arguments+=@('-contentTest','-contentProfile',('"'+$contentProfile+'"'))}
 if($ExpectedContentHash){$arguments+=@('-contentExpectedHash',$ExpectedContentHash)}
 if($Rendered){$arguments=@($arguments | Where-Object {$_ -ne '-batchmode' -and $_ -ne '-nographics'});$arguments+=@('-noaudio','-pvpNativeRenderCheck','-screen-fullscreen','0','-screen-width',"$RenderWidth",'-screen-height',"$RenderHeight")}
 if($Relay){$arguments+='-pvpNativeRelayCheck'}
 if($MotionOnly){$arguments+='-pvpNativeMotionOnly'}
 if($MotionSeconds -gt 0){$arguments+=@('-pvpNativeMotionSeconds',"$MotionSeconds")}
 if($LegacyRouting){$arguments+='-pvpNativeLegacyRouting'}
 if($Reconnect){$arguments+='-pvpNativeLanReconnect'}
 if($ChoiceEffects){$arguments+='-choiceEffectsLanCheck'}
 $style=if($Rendered -and ($Role -eq "client" -or $RenderBoth)){"Normal"}else{"Hidden"}
 $process=Start-Process -FilePath $Executable -ArgumentList $arguments -WindowStyle $style -PassThru
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
  if($MotionOnly -and $run.Role -eq 'client'){if($metrics.maxFreezeMs -gt 100 -or $metrics.frozenFrames -gt $metrics.movingFrames*.01){throw ('Moving pose pacing failed: frozen='+$metrics.frozenFrames+'/'+$metrics.movingFrames+' longest='+$metrics.maxFreezeMs+'ms; '+$run.Metrics)}; Write-Output ('MOVING_POSE_PACING_OK movingSeconds='+$metrics.movingSeconds+' metres='+$metrics.movingMetres+' maxSpeed='+$metrics.movingMaxSpeed+' longestFreezeMs='+$metrics.maxFreezeMs)}
  if(-not (Select-String -LiteralPath $run.Log -SimpleMatch ('PVP_NATIVE_LAN_OK '+$run.Role) -Quiet)){throw ($run.Role+' missing completion marker')}
  Write-Output ($run.Role+' PASS checks='+$metrics.checks+' native-map='+$metrics.map+' guest-metres='+$metrics.metres);if($metrics.performance){$metrics.performance | Format-Table name,count,averageMs,p95Ms,maxMs | Out-String | Write-Output}
 }
 Write-Output ('PVP_NATIVE_LAN_TWO_PROCESS_OK reconnect='+[bool]$Reconnect+' evidence='+$output)
 if($Relay){Write-Output 'Scope: two local player processes using the configured relay, native input and WheelCollider authority; cross-network play requires separate verification.'}
 else{Write-Output 'Scope: two loopback player processes using native input and WheelCollider authority; remote-PC LAN behavior requires separate verification.'}
} finally {
 foreach($process in $owned){if(-not $process.HasExited){Stop-Process -Id $process.Id -ErrorAction SilentlyContinue}}
}
