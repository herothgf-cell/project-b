$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskSources=@('RainSettings','RainPolicy','ContentJson','ContentTuning','ChoiceEffects','OneDayOfferPolicy','SlowRightTurnState','PassengerRequestSchedule','PassengerSituations','VehicleCare','PassengerBehavior','PassengerRequest','PrototypeState','CampaignAccount','EconomyLedger','OperationMetrics','DriverCondition','PvpConditionRules','TrafficRules','MannerLedger','FareTrip') | ForEach-Object {Join-Path $taskRoot "unity/EungyeTaxi/Assets/Scripts/$_.cs"}
New-Item -ItemType Directory -Force (Join-Path $taskRoot 'artifacts/rain-settings') | Out-Null
$taskExe=Join-Path $taskRoot 'artifacts/rain-settings/RainSettingsTests.exe'
& "$env:WINDIR/Microsoft.NET/Framework64/v4.0.30319/csc.exe" /nologo "/out:$taskExe" (Join-Path $taskRoot 'tests/RainSettingsTests.cs') @taskSources
if($LASTEXITCODE -ne 0){throw 'Rain settings compile failed'}
& $taskExe (Join-Path $taskRoot 'config/content/catalog.json')
if($LASTEXITCODE -ne 0){throw 'Rain settings tests failed'}
