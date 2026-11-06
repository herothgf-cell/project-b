param([int]$TimeoutSeconds=300)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskDir=Join-Path $taskRoot 'artifacts/rain'
New-Item -ItemType Directory -Force $taskDir | Out-Null
Push-Location $taskRoot
try {
 & node --input-type=module -e "import fs from 'node:fs'; import {hashProfile} from './tools/content-editor/profile.mjs'; const p=JSON.parse(fs.readFileSync('config/content/default-profile.json','utf8'));p.values.find(v=>v.id==='weather.rain.enabled').value='true';p.contentHash=hashProfile(p);fs.writeFileSync('artifacts/rain/rain-profile.json',JSON.stringify(p));"
 if($LASTEXITCODE -ne 0){throw 'Rain test profile generation failed'}
 $taskLog=Join-Path $taskDir 'player.log'
 $taskProc=Start-Process -FilePath (Join-Path $taskRoot 'builds/EungyeTaxi/EungyeTaxi.exe') -ArgumentList @('-oneDayMode','-hybridV2','-rainTest','-contentTest','-contentProfile',('"'+(Join-Path $taskDir 'rain-profile.json')+'"'),'-batchmode','-logFile',('"'+$taskLog+'"'),'-screen-width','1280','-screen-height','720','-screen-fullscreen','0') -WindowStyle Hidden -PassThru
 if(!$taskProc.WaitForExit($TimeoutSeconds*1000)){$taskProc.Kill();throw 'Rain test timeout; only owned test player stopped'}
 if($taskProc.ExitCode -ne 0){throw ('Rain test failed: '+$taskLog)}
 if(!(Select-String -LiteralPath $taskLog -SimpleMatch 'RAIN_PLAYER_OK' -Quiet)){throw 'Rain completion marker missing'}
 Select-String -LiteralPath $taskLog -Pattern 'RAIN PASS 20kmh|RAIN_PLAYER_OK' | ForEach-Object {$_.Line}
} finally {Pop-Location}
