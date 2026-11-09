$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskCompiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$taskSources=@('ContentJson','ContentTuning','ChoiceEffects','OneDayOfferPolicy','SlowRightTurnState','PassengerRequestSchedule','PassengerSituations','VehicleCare','PassengerBehavior','PassengerRequest','PassengerPresentation','PrototypeState','CampaignAccount','EconomyLedger','OperationMetrics','DriverCondition','PvpConditionRules','TrafficRules','MannerLedger','FareTrip') | ForEach-Object {Join-Path $taskRoot "unity/EungyeTaxi/Assets/Scripts/$_.cs"}
$taskOut=Join-Path $taskRoot 'artifacts/passenger-reactions'
New-Item -ItemType Directory -Force $taskOut | Out-Null
$taskExe=Join-Path $taskOut 'PassengerReactionTests.exe'
& $taskCompiler /nologo "/out:$taskExe" (Join-Path $taskRoot 'tests/PassengerReactionTests.cs') @taskSources
if($LASTEXITCODE -ne 0){throw 'PassengerReactionTests compile failed'}
& $taskExe (Join-Path $taskRoot 'config/content/catalog.json')
if($LASTEXITCODE -ne 0){throw 'PassengerReactionTests failed'}
