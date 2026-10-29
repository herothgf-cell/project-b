param([ValidateRange(1024,65000)][int]$Port=18001,[ValidateSet('all','baseline','reconnect','leave')][string]$Case='all')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$logs=Join-Path $root 'artifacts/pvp'
$exe=Join-Path $root 'builds/PvpPrototype/PvpPrototype.exe'
$owned=[System.Collections.Generic.List[System.Diagnostics.Process]]::new()
function Start-Player($role,$testPort,$seconds,$label,[string[]]$extra=@()) {
 $log=Join-Path $logs ($label+'.log')
 $p=Start-Process $exe -ArgumentList (@('-batchmode','-nographics','-pvpNetworkRole',$role,'-pvpPort',"$testPort",'-pvpProbeSeconds',"$seconds",'-pvpMatchCheck','-logFile',('"'+$log+'"'))+$extra) -WindowStyle Hidden -PassThru
 $owned.Add($p)
 return [pscustomobject]@{Process=$p;Log=$log;Label=$label}
}
function Assert-Player($run) {
 if(-not $run.Process.WaitForExit(45000)){throw ($run.Label+' timeout')}
 if($run.Process.ExitCode -ne 0){throw ($run.Label+' exit='+$run.Process.ExitCode+' '+$run.Log)}
 if(-not (Select-String -LiteralPath $run.Log -SimpleMatch 'PVP_NETWORK_PLAYER_OK' -Quiet)){throw ($run.Label+' missing success')}
 Select-String -LiteralPath $run.Log -Pattern 'PVP_MATCH_RESULT','PVP_NETWORK_PLAYER_OK','PVP_MATCH_TRIP_RESTORED' | ForEach-Object {Write-Output ($run.Label+': '+$_.Line)}
}
try {
 $index=0
 foreach($scenario in @('baseline','reconnect','leave')) {
  if($Case -ne 'all' -and $scenario -ne $Case){continue}
  $testPort=$Port+2*$index;$index++;$clientPort=$testPort;$extra=@();$proxy=$null
  if($scenario -eq 'reconnect') {
   $clientPort=$testPort+1;$extra=@('-pvpReconnectCheck')
   $proxyLog=Join-Path $logs 'competition-proxy.log'
   $proxy=Start-Process python -ArgumentList @('-u',('"'+(Join-Path $root 'tools/pvp_udp_test_proxy.py')+'"'),'--listen',"$clientPort",'--server',"$testPort",'--rtt-ms','150','--jitter-ms','15','--loss','0.03','--allow-rebind','--seconds','31') -WindowStyle Hidden -RedirectStandardOutput $proxyLog -RedirectStandardError (Join-Path $logs 'competition-proxy.err') -PassThru
   $owned.Add($proxy)
  }
  if($scenario -eq 'leave'){$extra=@('-pvpLeaveCheck')}
  $hostExtra=if($scenario -eq 'leave'){@('-pvpLeaveCheck')}else{@()}
  $hostRun=Start-Player 'host' $testPort 28 ('competition-'+$scenario+'-host') $hostExtra
  Start-Sleep -Seconds 2
  $clientRun=Start-Player 'client' $clientPort 24 ('competition-'+$scenario+'-client') $extra
  Assert-Player $clientRun
  if(-not (Select-String -LiteralPath $clientRun.Log -SimpleMatch 'PVP_LOCAL_MENU_CHECK timescale=0' -Quiet)){throw 'Local menu clock check not exercised'}
  Assert-Player $hostRun
  if(-not (Select-String -LiteralPath $hostRun.Log -SimpleMatch 'PVP_SHARED_OBSTACLE_BLOCKED' -Quiet)){throw 'Authority obstacle block not exercised'}
  if($scenario -ne 'leave') {
   if(@(Select-String -LiteralPath $hostRun.Log -SimpleMatch 'PVP_MATCH_COMPLETION_REPLAY_RECEIVED player=1').Count -lt 2){throw 'Duplicate completion RPCs not received'}
   $h=(Select-String -LiteralPath $hostRun.Log -Pattern '^PVP_MATCH_RESULT ' | Select-Object -Last 1).Line
   $c=(Select-String -LiteralPath $clientRun.Log -Pattern '^PVP_MATCH_RESULT ' | Select-Object -Last 1).Line
   if(-not $h -or $h -ne $c){throw 'Authority/client final result mismatch'}
   $hostSamples=@(Select-String -LiteralPath $hostRun.Log -Pattern '^PVP_SHARED_SAMPLE ' | ForEach-Object {$_.Line})
   $clientSamples=@(Select-String -LiteralPath $clientRun.Log -Pattern '^PVP_SHARED_SAMPLE ' | ForEach-Object {$_.Line})
   $common=@($clientSamples | Where-Object {$_ -in $hostSamples})
   if($common.Count -lt 10){throw 'Insufficient identical authority/client shared-state samples'}
   Write-Output ('Shared state exact matches='+$common.Count)
   if(-not (Select-String -LiteralPath $hostRun.Log -SimpleMatch 'PVP_MATCH_SAME_TICK_REQUESTS queued=2' -Quiet)){throw 'Same-tick race not exercised'}
  }
  if($scenario -eq 'reconnect') {
   if(-not (Select-String -LiteralPath $clientRun.Log -SimpleMatch 'PVP_MATCH_TRIP_RESTORED' -Quiet)){throw 'Onboard ownership not restored'}
   if(-not $proxy.WaitForExit(45000) -or $proxy.ExitCode -ne 0){throw 'Proxy failure'}
   $metrics=Get-Content $proxyLog | Select-Object -Last 1 | ConvertFrom-Json
   if($metrics.dropped -le 0 -or $metrics.rebinds -le 0){throw 'Loss/reconnect not exercised'}
   Write-Output ($metrics | ConvertTo-Json -Compress)
  }
 }
 Write-Output 'PVP_COMPETITION_PLAYERS_OK server-position-controlled; not full-course physical/human driving'
} finally {
 foreach($process in $owned){if(-not $process.HasExited){Stop-Process -Id $process.Id -ErrorAction SilentlyContinue}}
}