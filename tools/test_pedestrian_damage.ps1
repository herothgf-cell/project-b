$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskCompiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$taskNames=@('ContentJson','ContentTuning','AccidentSeverityPolicy','AccidentState','PvpConditionRules','DriverCondition','PrototypeState','CampaignAccount','VehicleCatalog','EconomyLedger','OperationMetrics','PassengerRequest','MannerLedger','FareTrip','VehicleCare','PassengerBehavior','TrafficRules','PassengerWaitingPolicy','ScoreFeedback','RideReceiptSummary')
$taskSources=$taskNames | ForEach-Object {Join-Path $taskRoot "unity/EungyeTaxi/Assets/Scripts/$_.cs"}
$taskExe=Join-Path $taskRoot 'artifacts/PedestrianDamageTests.exe'
& $taskCompiler /nologo "/out:$taskExe" (Join-Path $taskRoot 'tests/PedestrianDamageTests.cs') @taskSources
if($LASTEXITCODE -ne 0){throw 'Pedestrian damage test compile failed'}
& $taskExe
if($LASTEXITCODE -ne 0){throw 'Pedestrian damage test failed'}
