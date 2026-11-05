param([string]$Case='all')
$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskOut=Join-Path $taskRoot 'artifacts/operation-planning'
New-Item -ItemType Directory -Force $taskOut | Out-Null
$taskCompiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$taskSources=@('DriverCondition','PvpConditionRules','PrototypeState','CampaignAccount','EconomyLedger','OperationMetrics','PassengerRequest','MannerLedger','FareTrip','VehicleCare','PassengerBehavior','TrafficRules','PassengerWaitingPolicy','ScoreFeedback','RideReceiptSummary','OperationPlanningPolicy','ServiceStopsPlanning','PvpSimulationBotServices') | ForEach-Object {Join-Path $taskRoot "unity/EungyeTaxi/Assets/Scripts/$_.cs"}
$taskExe=Join-Path $taskOut 'OperationPlanningAdapterTests.exe'
$taskCoreSources=@('ContentJson','ContentTuning') | ForEach-Object {Join-Path $taskRoot "unity/EungyeTaxi/Assets/Scripts/$_.cs"}
& $taskCompiler /nologo /langversion:5 "/out:$taskExe" (Join-Path $taskRoot 'tests/OperationPlanningAdapterTests.cs') @taskSources @taskCoreSources
if($LASTEXITCODE -ne 0){throw 'Operation planning adapters compile failed'}
if($Case -eq 'all'){& $taskExe}else{& $taskExe $Case}
if($LASTEXITCODE -ne 0){throw 'Operation planning adapters failed'}
