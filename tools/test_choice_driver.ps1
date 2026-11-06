$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskDir=Join-Path $taskRoot 'artifacts/choice-driver'
New-Item -ItemType Directory -Force $taskDir | Out-Null
$taskExe=Join-Path $taskDir 'ChoiceDriverTests.exe'
$taskCompiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
& $taskCompiler /nologo "/out:$taskExe" (Join-Path $taskRoot 'tests/ChoiceDriverTests.cs') (Join-Path $taskRoot 'unity/EungyeTaxi/Assets/Scripts/DriverCondition.cs')
if($LASTEXITCODE -ne 0){throw 'Choice driver compile failed'}
& $taskExe
if($LASTEXITCODE -ne 0){throw 'Choice driver tests failed'}
