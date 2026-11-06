$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskCompiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$taskOut=Join-Path $taskRoot 'artifacts/rain/RainPolicyTests.exe'
New-Item -ItemType Directory -Force (Split-Path $taskOut) | Out-Null
& $taskCompiler /nologo "/out:$taskOut" (Join-Path $taskRoot 'tests/RainPolicyTests.cs') (Join-Path $taskRoot 'unity/EungyeTaxi/Assets/Scripts/RainPolicy.cs')
if($LASTEXITCODE -ne 0){throw 'Rain policy compile failed'}
& $taskOut
if($LASTEXITCODE -ne 0){throw 'Rain policy tests failed'}
