param([ValidateRange(1024,65000)][int]$Port=18301,[ValidateSet('all','baseline','lag-swap','violation')][string]$Case='all',[string]$Executable)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$logs=Join-Path $root 'artifacts/pvp'
$exe=if($Executable){[IO.Path]::GetFullPath($Executable)}else{Join-Path $root 'builds/PvpPrototype/PvpPrototype.exe'}
$owned=[System.Collections.Generic.List[System.Diagnostics.Process]]::new()
function Start-Drive($role,$testPort,$seconds,$label,[string[]]$extra=@()) {
 $log=Join-Path $logs ($label+'.log')
 $p=Start-Process $exe -ArgumentList (@('-batchmode','-nographics','-pvpNetworkRole',$role,'-pvpPort',"$testPort",'-pvpProbeSeconds',"$seconds",'-pvpDriveCheck','-logFile',('"'+$log+'"'))+$extra) -WindowStyle Hidden -PassThru
 $owned.Add($p);return [pscustomobject]@{Process=$p;Log=$log;Label=$label}
}
function Assert-Drive($run) {
 if(-not $run.Process.WaitForExit(190000)){throw ($run.Label+' timeout')}
 if($run.Process.ExitCode -ne 0){throw ($run.Label+' exit='+$run.Process.ExitCode+' '+$run.Log)}
 foreach($marker in @('PVP_NETWORK_PLAYER_OK','PVP_DRIVING_PROOF','PVP_MATCH_RESULT')){if(-not (Select-String -LiteralPath $run.Log -SimpleMatch $marker -Quiet)){throw ($run.Label+' missing '+$marker)}}
 Select-String -LiteralPath $run.Log -Pattern '^PVP_DRIVING_PROOF','^PVP_MATCH_RESULT' | ForEach-Object {Write-Output ($run.Label+': '+$_.Line)}
}
try {
 $index=0
 foreach($scenario in @('baseline','lag-swap','violation')) {
  if($Case -ne 'all' -and $Case -ne $scenario){continue}
  $testPort=$Port+$index*2;$index++;$clientPort=$testPort;$extra=@();$proxy=$null
  if($scenario -eq 'lag-swap'){
   $clientPort=$testPort+1;$extra=@('-pvpDriveSwap');$proxyLog=Join-Path $logs 'driving-proxy.log'
   $proxy=Start-Process python -ArgumentList @('-u',('"'+(Join-Path $root 'tools/pvp_udp_test_proxy.py')+'"'),'--listen',"$clientPort",'--server',"$testPort",'--rtt-ms','150','--jitter-ms','15','--loss','0.03','--seconds','155') -WindowStyle Hidden -RedirectStandardOutput $proxyLog -RedirectStandardError (Join-Path $logs 'driving-proxy.err') -PassThru;$owned.Add($proxy)
  }
  if($scenario -eq 'violation'){$extra=@('-pvpDriveViolation')}
  $hostRun=Start-Drive 'host' $testPort 150 ('driving-'+$scenario+'-host') $extra
  Start-Sleep -Seconds 2
  $clientRun=Start-Drive 'client' $clientPort 145 ('driving-'+$scenario+'-client') $extra
  Assert-Drive $clientRun
  Assert-Drive $hostRun
  $hostResult=(Select-String -LiteralPath $hostRun.Log -Pattern '^PVP_MATCH_RESULT ' | Select-Object -Last 1).Line
  $clientResult=(Select-String -LiteralPath $clientRun.Log -Pattern '^PVP_MATCH_RESULT ' | Select-Object -Last 1).Line
  if($hostResult -ne $clientResult){throw 'Driving result mismatch'}
  $h=@(Select-String -LiteralPath $hostRun.Log -Pattern '^PVP_SHARED_SAMPLE ' | ForEach-Object {$_.Line})
  $c=@(Select-String -LiteralPath $clientRun.Log -Pattern '^PVP_SHARED_SAMPLE ' | ForEach-Object {$_.Line})
  $common=@($c | Where-Object {$_ -in $h -and $_ -match '\|N.*\|P'})
  if($common.Count -lt 30){throw 'Insufficient exact shared NPC/pedestrian samples'}
  if(Select-String -LiteralPath $hostRun.Log -SimpleMatch -Pattern 'PVP_MATCH_SAME_TICK_REQUESTS' -Quiet){throw 'Driving test used position/race fixture'}
  if($scenario -eq 'violation' -and -not (Select-String -LiteralPath $hostRun.Log -SimpleMatch -Pattern 'PVP_RULE_CONFIRMED player=0' -Quiet)){throw 'No real driving violation confirmed'}
  Write-Output ('Shared NPC/pedestrian exact samples='+$common.Count)
  if($proxy){if(-not $proxy.WaitForExit(20000) -or $proxy.ExitCode -ne 0){throw 'Proxy failed'};Get-Content $proxyLog | Select-Object -Last 1}
 }
 Write-Output 'PVP_INPUT_DRIVING_PLAYERS_OK no runtime authority position fixture; planar motor inputs, not human/WheelCollider acceptance'
} finally {
 foreach($process in $owned){if(-not $process.HasExited){Stop-Process -Id $process.Id -ErrorAction SilentlyContinue}}
}
