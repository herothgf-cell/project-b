$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskCompiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$taskSources=@('ContentJson','ContentTuning','ContentTuningRuntime','ContentTuningProbe','ChoiceEffects','OneDayOfferPolicy','SlowRightTurnState','PassengerRequestSchedule','PassengerSituations','VehicleCare','PassengerBehavior','PassengerRequest','PrototypeState','CampaignAccount','EconomyLedger','OperationMetrics','DriverCondition','PvpConditionRules','TrafficRules','MannerLedger','FareTrip') | ForEach-Object {Join-Path $taskRoot "unity/EungyeTaxi/Assets/Scripts/$_.cs"}
$taskOutput=Join-Path $taskRoot 'artifacts/content-preview'
New-Item -ItemType Directory -Force $taskOutput | Out-Null
$taskExe=Join-Path $taskOutput 'ContentPreviewChecks.exe'
& $taskCompiler /nologo /define:CONTENT_RUNTIME_PREVIEW_CHECKS /r:System.Web.Extensions.dll "/out:$taskExe" (Join-Path $taskRoot 'tests/ContentPreviewStubs.cs') (Join-Path $taskRoot 'unity/EungyeTaxi/Assets/Editor/ContentTuningChecks.cs') @taskSources
if($LASTEXITCODE -ne 0){throw 'ContentPreviewChecks compile failed'}
Push-Location $taskRoot
try {& $taskExe;if($LASTEXITCODE -ne 0){throw 'ContentPreviewChecks failed'}} finally {Pop-Location}
