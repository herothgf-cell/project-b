$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskOut=Join-Path $taskRoot 'artifacts/operation-planning'
New-Item -ItemType Directory -Force $taskOut | Out-Null
$taskCompiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$taskSources=@('DriverCondition','PvpConditionRules','PrototypeState','CampaignAccount','EconomyLedger','OperationMetrics','PassengerRequest','MannerLedger','FareTrip','VehicleCare','PassengerBehavior','TrafficRules','PassengerWaitingPolicy','ScoreFeedback','RideReceiptSummary','OperationPlanningPolicy') | ForEach-Object {Join-Path $taskRoot "unity/EungyeTaxi/Assets/Scripts/$_.cs"}
$taskExe=Join-Path $taskOut 'OperationPlanningTests.exe'
& $taskCompiler /nologo /langversion:5 "/out:$taskExe" (Join-Path $taskRoot 'tests/OperationPlanningTests.cs') @taskSources
if($LASTEXITCODE -ne 0){throw 'Operation planning compile failed'}
& $taskExe
if($LASTEXITCODE -ne 0){throw 'Operation planning tests failed'}
