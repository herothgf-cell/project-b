$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskCompiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$taskSources=@('VehicleCare','PassengerSituations','PassengerBehavior','ContentJson','ContentTuning','ChoiceEffects') | ForEach-Object {Join-Path $taskRoot "unity/EungyeTaxi/Assets/Scripts/$_.cs"}
New-Item -ItemType Directory -Force (Join-Path $taskRoot 'artifacts/passenger-traits') | Out-Null
$taskExe=Join-Path $taskRoot 'artifacts/passenger-traits/PassengerTraitTests.exe'
& $taskCompiler /nologo "/out:$taskExe" (Join-Path $taskRoot 'tests/PassengerTraitTests.cs') @taskSources
if($LASTEXITCODE -ne 0){throw 'PassengerTraitTests compile failed'}
& $taskExe (Join-Path $taskRoot "config/content/catalog.json")
if($LASTEXITCODE -ne 0){throw 'PassengerTraitTests failed'}
