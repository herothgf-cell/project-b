$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskCompiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$taskSources=@('ContentJson','ContentTuning','ChoiceEffects','OneDayOfferPolicy','SlowRightTurnState','PassengerRequestSchedule','PassengerSituations','VehicleCare','PassengerBehavior','PassengerRequest','PrototypeState','CampaignAccount','VehicleCatalog','EconomyLedger','OperationMetrics','DriverCondition','PvpConditionRules','TrafficRules','MannerLedger','FareTrip') | ForEach-Object {Join-Path $taskRoot "unity/EungyeTaxi/Assets/Scripts/$_.cs"}
New-Item -ItemType Directory -Force (Join-Path $taskRoot 'artifacts/passenger-overrides') | Out-Null
$taskExe=Join-Path $taskRoot 'artifacts/passenger-overrides/PassengerOverridesTests.exe'
& $taskCompiler /nologo "/out:$taskExe" (Join-Path $taskRoot 'tests/PassengerOverridesTests.cs') @taskSources
if($LASTEXITCODE -ne 0){throw 'PassengerOverridesTests compile failed'}
& $taskExe (Join-Path $taskRoot 'config/content/catalog.json') (Join-Path $taskRoot 'tests/content-editor/fixtures/passenger-overrides.json')
if($LASTEXITCODE -ne 0){throw 'PassengerOverridesTests failed'}
