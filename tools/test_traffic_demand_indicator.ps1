$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskCompiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$taskOutput=Join-Path $taskRoot 'artifacts/traffic-demand-indicator'
New-Item -ItemType Directory -Force -Path $taskOutput | Out-Null
$taskSources=@(@('PassengerDemandPolicy','TrafficDriverProfile','TrafficRules','ManualIndicatorPolicy') | ForEach-Object {Join-Path $taskRoot "unity/EungyeTaxi/Assets/Scripts/$_.cs"})
$taskExe=Join-Path $taskOutput 'TrafficDemandIndicatorTests.exe'
& $taskCompiler /nologo /langversion:5 "/out:$taskExe" (Join-Path $taskRoot 'tests/TrafficDemandIndicatorTests.cs') @taskSources
if($LASTEXITCODE -ne 0){throw 'TrafficDemandIndicatorTests compile failed'}
& $taskExe
if($LASTEXITCODE -ne 0){throw 'TrafficDemandIndicatorTests failed'}



