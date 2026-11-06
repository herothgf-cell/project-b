$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskCompiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$taskOut=Join-Path $taskRoot 'artifacts/choice-effects'
New-Item -ItemType Directory -Force $taskOut | Out-Null
$taskExe=Join-Path $taskOut 'ChoiceEffectsTests.exe'
$taskSources=@('ContentJson','ContentTuning','ChoiceEffects') | ForEach-Object {Join-Path $taskRoot "unity/EungyeTaxi/Assets/Scripts/$_.cs"}
& $taskCompiler /nologo "/out:$taskExe" (Join-Path $taskRoot 'tests/ChoiceEffectsTests.cs') @taskSources
if($LASTEXITCODE -ne 0){throw 'ChoiceEffectsTests compile failed'}
& $taskExe
if($LASTEXITCODE -ne 0){throw 'ChoiceEffectsTests failed'}
