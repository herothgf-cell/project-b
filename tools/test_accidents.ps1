$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskCompiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$taskOutput = Join-Path $taskRoot 'artifacts'
New-Item -ItemType Directory -Force -Path $taskOutput | Out-Null
$taskSources = @('ContentJson','ContentTuning','AccidentSeverityPolicy','AccidentState','PvpConditionRules','DriverCondition','PrototypeState','CampaignAccount','EconomyLedger','OperationMetrics','PassengerRequest','MannerLedger','FareTrip','VehicleCare','PassengerBehavior','TrafficRules','PassengerWaitingPolicy','ScoreFeedback','RideReceiptSummary') | ForEach-Object { Join-Path $taskRoot "unity/EungyeTaxi/Assets/Scripts/$_.cs" }
$taskExe=Join-Path $taskOutput 'AccidentTests.exe'
& $taskCompiler /nologo "/out:$taskExe" (Join-Path $taskRoot 'tests/AccidentTests.cs') @taskSources
if($LASTEXITCODE -ne 0){throw 'AccidentTests compile failed'}
& $taskExe
if($LASTEXITCODE -ne 0){throw 'AccidentTests failed'}
