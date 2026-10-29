$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskCompiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$taskSources=@(@('VehicleCare') | ForEach-Object {Join-Path $taskRoot "unity/EungyeTaxi/Assets/Scripts/$_.cs"})
$taskBehavior=Join-Path $taskRoot 'unity/EungyeTaxi/Assets/Scripts/PassengerBehavior.cs'
if(Test-Path $taskBehavior){$taskSources+= $taskBehavior}
$taskExe=Join-Path $taskRoot 'artifacts/PassengerBehaviorTests.exe'
& $taskCompiler /nologo "/out:$taskExe" (Join-Path $taskRoot 'tests/PassengerBehaviorTests.cs') @taskSources
if($LASTEXITCODE -ne 0){throw 'PassengerBehaviorTests compile failed'}
& $taskExe
if($LASTEXITCODE -ne 0){throw 'PassengerBehaviorTests failed'}
