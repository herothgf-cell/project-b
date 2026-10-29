param([ValidateRange(1024,65000)][int]$Port=17871,[ValidateRange(1,10)][int]$Rounds=2)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$exe=Join-Path $root 'builds/PvpPrototype/PvpPrototype.exe'
$logs=Join-Path $root 'artifacts/pvp'
New-Item -ItemType Directory -Force $logs | Out-Null
$owned=[System.Collections.Generic.List[System.Diagnostics.Process]]::new()
function Start-Pvp([string]$role,[int]$port,[int]$seconds,[string]$label,[string[]]$extra=@()) {
 $log=Join-Path $logs ($label+'.log')
 $arguments=@('-batchmode','-nographics','-pvpNetworkRole',$role,'-pvpPort',"$port",'-pvpProbeSeconds',"$seconds",'-logFile',('"'+$log+'"'))+$extra
 $process=Start-Process -FilePath $exe -ArgumentList $arguments -WindowStyle Hidden -PassThru
 $owned.Add($process)
 return [pscustomobject]@{Process=$process;Log=$log;Label=$label}
}
function Assert-Pvp($run,[int]$expected=0,[string]$marker='PVP_NETWORK_PLAYER_OK') {
 if(-not $run.Process.WaitForExit(45000)){throw ($run.Label+' timeout')}
 if($run.Process.ExitCode -ne $expected){throw ($run.Label+' exit='+$run.Process.ExitCode+' log='+$run.Log)}
 if(-not (Select-String -LiteralPath $run.Log -SimpleMatch $marker -Quiet)){throw ($run.Label+' missing result marker')}
 Write-Output ($run.Label+' exit='+$expected)
 Select-String -LiteralPath $run.Log -Pattern 'PVP_NETWORK_PLAYER_(OK|FAIL)' | ForEach-Object {$_.Line}
}
try {
 for($round=0;$round -lt $Rounds;$round++) {
  $testPort=$Port+$round
  $hostRun=Start-Pvp 'host' $testPort 22 "net-$round-host"
  Start-Sleep -Seconds 2
  $clientRun=Start-Pvp 'client' $testPort 15 "net-$round-client"
  Start-Sleep -Seconds 3
  $rejectRun=Start-Pvp 'client' $testPort 12 "net-$round-rejected" @('-pvpBadMap','-pvpExpectedRejection','"map mismatch"')
  Assert-Pvp $rejectRun
  Assert-Pvp $clientRun
  Assert-Pvp $hostRun
 }
 $hostRun=Start-Pvp 'host' ($Port+$Rounds) 7 'net-host-loss-host'
 Start-Sleep -Seconds 2
 $clientRun=Start-Pvp 'client' ($Port+$Rounds) 18 'net-host-loss-client'
 Assert-Pvp $hostRun
 Assert-Pvp $clientRun 1 'PVP_NETWORK_PLAYER_FAIL'
 if(-not (Select-String -LiteralPath $clientRun.Log -SimpleMatch 'PVP_NETWORK_DISCONNECTED' -Quiet)){throw 'Host-loss did not exercise disconnect callback'}
 Write-Output 'PVP_DIRECT_IP_CHECKS_OK (same-PC processes only; no remote-PC certification)'
} finally {
 foreach($process in $owned){if(-not $process.HasExited){Stop-Process -Id $process.Id -ErrorAction SilentlyContinue}}
}