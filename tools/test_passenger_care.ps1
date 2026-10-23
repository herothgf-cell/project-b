$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskCompiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$taskOutput = Join-Path $taskRoot 'artifacts'
New-Item -ItemType Directory -Force -Path $taskOutput | Out-Null
$taskSources = @('DriverCondition','PrototypeState','CampaignAccount','EconomyLedger','PassengerRequest','MannerLedger','FareTrip','VehicleCare','TrafficRules') | ForEach-Object { Join-Path $taskRoot "unity/YeongtongTaxi/Assets/Scripts/$_.cs" }
$taskExe=Join-Path $taskOutput 'PassengerCareTests.exe'
& $taskCompiler /nologo "/out:$taskExe" (Join-Path $taskRoot 'tests/PassengerCareTests.cs') @taskSources
if($LASTEXITCODE -ne 0){throw 'PassengerCareTests compile failed'}
& $taskExe
if($LASTEXITCODE -ne 0){throw 'PassengerCareTests failed'}
