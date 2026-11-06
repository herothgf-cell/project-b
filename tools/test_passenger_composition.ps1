$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskCompiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$taskSources=@('VehicleCare','PassengerSituations','PassengerBehavior','ContentJson','ContentTuning','ChoiceEffects') | ForEach-Object {Join-Path $taskRoot "unity/EungyeTaxi/Assets/Scripts/$_.cs"}
New-Item -ItemType Directory -Force (Join-Path $taskRoot 'artifacts/passenger-composition') | Out-Null
$taskExe=Join-Path $taskRoot 'artifacts/passenger-composition/PassengerCompositionTests.exe'
& $taskCompiler /nologo "/out:$taskExe" (Join-Path $taskRoot 'tests/PassengerCompositionTests.cs') @taskSources
if($LASTEXITCODE -ne 0){throw 'PassengerCompositionTests compile failed'}
& $taskExe (Join-Path $taskRoot 'config/content/catalog.json') (Join-Path $taskRoot 'tests/content-editor/fixtures/passenger-presets.json')
if($LASTEXITCODE -ne 0){throw 'PassengerCompositionTests failed'}
