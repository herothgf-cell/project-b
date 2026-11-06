$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskCompiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$taskExe=Join-Path $taskRoot 'artifacts/PassengerSituationTests.exe'
New-Item -ItemType Directory -Force (Split-Path $taskExe) | Out-Null
$taskCoreSources=@('ContentJson','ContentTuning','ChoiceEffects') | ForEach-Object {Join-Path $taskRoot "unity/EungyeTaxi/Assets/Scripts/$_.cs"}
& $taskCompiler /nologo "/out:$taskExe" (Join-Path $taskRoot 'tests/PassengerSituationTests.cs') (Join-Path $taskRoot 'unity/EungyeTaxi/Assets/Scripts/PassengerSituations.cs') (Join-Path $taskRoot 'unity/EungyeTaxi/Assets/Scripts/PassengerBehavior.cs') (Join-Path $taskRoot 'unity/EungyeTaxi/Assets/Scripts/VehicleCare.cs') @taskCoreSources
if($LASTEXITCODE -ne 0){throw 'PassengerSituationTests compile failed'}
& $taskExe
if($LASTEXITCODE -ne 0){throw 'PassengerSituationTests failed'}
