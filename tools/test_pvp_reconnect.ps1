param([ValidateRange(1024,65000)][int]$Port=17901,[ValidateSet('all','resume','forged','expiry','leave','blackout','host-loss')][string]$Case='all')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$exe=Join-Path $root 'builds/PvpPrototype/PvpPrototype.exe'
$logs=Join-Path $root 'artifacts/pvp'
New-Item -ItemType Directory -Force $logs | Out-Null
$owned=[System.Collections.Generic.List[System.Diagnostics.Process]]::new()
function Start-Pvp([string]$role,[int]$testPort,[int]$seconds,[string]$label,[string[]]$extra=@()) {
 $log=Join-Path $logs ($label+'.log')
 $arguments=@('-batchmode','-nographics','-pvpNetworkRole',$role,'-pvpPort',"$testPort",'-pvpProbeSeconds',"$seconds",'-logFile',('"'+$log+'"'))+$extra
 $process=Start-Process $exe -ArgumentList $arguments -WindowStyle Hidden -PassThru
 $owned.Add($process)
 return [pscustomobject]@{Process=$process;Log=$log;Label=$label}
}
function Assert-Pvp($run,[int]$expected=0,[string]$marker='PVP_NETWORK_PLAYER_OK') {
 if(-not $run.Process.WaitForExit(45000)){throw ($run.Label+' timeout')}
 if($run.Process.ExitCode -ne $expected){throw ($run.Label+' exit='+$run.Process.ExitCode+' log='+$run.Log)}
 if(-not (Select-String -LiteralPath $run.Log -SimpleMatch $marker -Quiet)){throw ($run.Label+' missing '+$marker)}
 Select-String -LiteralPath $run.Log -Pattern 'PVP_NETWORK_PLAYER_(OK|FAIL)','PVP_RECONNECT_RESTORED' | ForEach-Object {Write-Output ($run.Label+': '+$_.Line)}
}
try {
 $index=0
 foreach($scenario in @('resume','forged','expiry','leave','blackout','host-loss')) {
  if($Case -ne 'all' -and $Case -ne $scenario){continue}
  $testPort=$Port+$index*2;$index++
  $clientPort=$testPort;$hostSeconds=22;$clientSeconds=15;$hostExtra=@();$clientExtra=@('-pvpReconnectCheck');$expected=0;$marker='PVP_NETWORK_PLAYER_OK';$proxy=$null
  switch($scenario) {
   'forged' {$clientExtra+=@('-pvpTamperTicket','-pvpExpectedRejection','"invalid reconnect ticket"')}
   'expiry' {$hostExtra=@('-pvpReconnectGrace','3');$clientExtra+=@('-pvpReconnectDelay','5');$expected=1;$marker='PVP_NETWORK_PLAYER_FAIL client reconnect timeout'}
   'leave' {$clientExtra+=@('-pvpLeaveCheck')}
   'blackout' {
    $clientPort=$testPort+1;$hostSeconds=27;$clientSeconds=22;$clientExtra+=@('-pvpBlackoutCheck')
    $proxyLog=Join-Path $logs 'reconnect-blackout-proxy.log'
    $proxy=Start-Process python -ArgumentList @('-u',('"'+(Join-Path $root 'tools/pvp_udp_test_proxy.py')+'"'),'--listen',"$clientPort",'--server',"$testPort",'--rtt-ms','150','--jitter-ms','15','--loss','0.03','--seconds','30','--blackout-start','8','--blackout-seconds','4','--allow-rebind') -WindowStyle Hidden -RedirectStandardOutput $proxyLog -RedirectStandardError (Join-Path $logs 'reconnect-blackout-proxy.err') -PassThru
    $owned.Add($proxy)
   }
   'host-loss' {$hostSeconds=7;$hostExtra=@('-pvpReconnectGrace','3');$clientExtra+=@('-pvpBlackoutCheck');$expected=1;$marker='PVP_NETWORK_PLAYER_FAIL client reconnect timeout'}
  }
  $hostRun=Start-Pvp 'host' $testPort $hostSeconds ('reconnect-'+$scenario+'-host') $hostExtra
  Start-Sleep -Seconds 2
  $clientRun=Start-Pvp 'client' $clientPort $clientSeconds ('reconnect-'+$scenario+'-client') $clientExtra
  Assert-Pvp $clientRun $expected $marker
  Assert-Pvp $hostRun
  if($scenario -in @('resume','blackout')) {
   if(-not (Select-String -LiteralPath $clientRun.Log -SimpleMatch 'PVP_RECONNECT_RESTORED' -Quiet)){throw 'No restored state received'}
   if(-not (Select-String -LiteralPath $hostRun.Log -SimpleMatch 'PVP_PEER_RESUMED' -Quiet)){throw 'Host did not resume stable slot'}
  }
  if($scenario -in @('expiry','leave','forged','host-loss')) {
   if(Select-String -LiteralPath $clientRun.Log -SimpleMatch 'PVP_RECONNECT_RESTORED' -Quiet){throw 'Invalid reconnect was accepted'}
  }
  if($proxy) {
   if(-not $proxy.WaitForExit(45000) -or $proxy.ExitCode -ne 0){throw 'Proxy failed'}
   $metrics=Get-Content -LiteralPath $proxyLog | Select-Object -Last 1 | ConvertFrom-Json
   if($metrics.blackout_dropped -le 0 -or $metrics.rebinds -le 0){throw 'Actual packet blackout/rebind not exercised'}
   Write-Output ($metrics | ConvertTo-Json -Compress)
  }
 }
 Write-Output 'PVP_RECONNECT_PLAYERS_OK (same-PC only; expired-server admission separately covered by Editor model)'
} finally {
 foreach($process in $owned){if(-not $process.HasExited){Stop-Process -Id $process.Id -ErrorAction SilentlyContinue}}
}