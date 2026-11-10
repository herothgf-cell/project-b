$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskOut=Join-Path $taskRoot 'artifacts/pvp-condition-settings'
New-Item -ItemType Directory -Force $taskOut | Out-Null
$taskCompiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$taskSources=@('DriverCondition','PvpConditionRules','PrototypeState','CampaignAccount','VehicleCatalog','EconomyLedger','OperationMetrics','PassengerRequest','MannerLedger','FareTrip','VehicleCare','PassengerBehavior','TrafficRules','PassengerWaitingPolicy','ScoreFeedback','RideReceiptSummary','OperationPlanningPolicy') | ForEach-Object {Join-Path $taskRoot "unity/EungyeTaxi/Assets/Scripts/$_.cs"}
$taskExe=Join-Path $taskOut 'PvpConditionSettingsTests.exe'
$taskCoreSources=@('ContentJson','ContentTuning','ChoiceEffects') | ForEach-Object {Join-Path $taskRoot "unity/EungyeTaxi/Assets/Scripts/$_.cs"}
& $taskCompiler /nologo /langversion:5 "/out:$taskExe" (Join-Path $taskRoot 'tests/PvpConditionSettingsTests.cs') @taskSources @taskCoreSources
if($LASTEXITCODE -ne 0){throw 'PVP condition settings compile failed'}
& $taskExe
if($LASTEXITCODE -ne 0){throw 'PVP condition settings tests failed'}
