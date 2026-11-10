$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskCompiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$taskSources=@('ContentJson','ContentTuning','ChoiceEffects','OneDayOfferPolicy','SlowRightTurnState','PassengerRequestSchedule','PassengerSituations','VehicleCare','PassengerBehavior','PassengerRequest','PrototypeState','CampaignAccount','VehicleCatalog','EconomyLedger','OperationMetrics','DriverCondition','PvpConditionRules','TrafficRules','MannerLedger','FareTrip') | ForEach-Object {Join-Path $taskRoot "unity/EungyeTaxi/Assets/Scripts/$_.cs"}
New-Item -ItemType Directory -Force (Join-Path $taskRoot 'artifacts/passenger-roster-v1') | Out-Null
$taskExe=Join-Path $taskRoot 'artifacts/passenger-roster-v1/PassengerRosterTests.exe'
& $taskCompiler /nologo "/out:$taskExe" (Join-Path $taskRoot 'tests/PassengerRosterTests.cs') @taskSources
if($LASTEXITCODE -ne 0){throw 'PassengerRosterTests compile failed'}
& $taskExe (Join-Path $taskRoot 'config/content/catalog.json') (Join-Path $taskRoot 'config/content/passenger-roster-v2.json') (Join-Path $taskRoot 'config/content/default-profile.json') (Join-Path $taskRoot 'tests/content-editor/fixtures/passenger-legacy-twelve.json')
if($LASTEXITCODE -ne 0){throw 'PassengerRosterTests failed'}
