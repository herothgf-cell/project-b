$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskCompiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$taskSources=@('ContentJson','ContentTuning','OneDayOfferPolicy','SlowRightTurnState','PassengerRequestSchedule','PassengerSituations','VehicleCare','PassengerBehavior','PassengerRequest','PrototypeState','CampaignAccount','EconomyLedger','OperationMetrics','DriverCondition','PvpConditionRules','TrafficRules','MannerLedger','FareTrip') | ForEach-Object {Join-Path $taskRoot "unity/EungyeTaxi/Assets/Scripts/$_.cs"}
New-Item -ItemType Directory -Force (Join-Path $taskRoot 'artifacts/content-tuning') | Out-Null
$taskExe=Join-Path $taskRoot 'artifacts/content-tuning/ContentTuningTests.exe'
& $taskCompiler /nologo "/out:$taskExe" (Join-Path $taskRoot 'tests/ContentTuningTests.cs') @taskSources
if($LASTEXITCODE -ne 0){throw 'ContentTuningTests compile failed'}
& $taskExe (Join-Path $taskRoot 'config/content/catalog.json') (Join-Path $taskRoot 'artifacts/content-tuning/unicode-profile.json')
if($LASTEXITCODE -ne 0){throw 'ContentTuningTests failed'}

& node (Join-Path $taskRoot 'tests/ContentTuningHash.mjs') (Join-Path $taskRoot 'artifacts/content-tuning/unicode-profile.json')
if($LASTEXITCODE -ne 0){throw 'CSharp/Node UTF8 hash comparison failed'}
