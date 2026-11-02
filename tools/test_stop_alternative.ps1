$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskCompiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$taskExe=Join-Path $taskRoot 'artifacts/StopAlternativeTests.exe'
& $taskCompiler /nologo "/out:$taskExe" (Join-Path $taskRoot 'tests/StopAlternativeTests.cs') (Join-Path $taskRoot 'unity/EungyeTaxi/Assets/Scripts/StopOccupancyPolicy.cs')
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
& $taskExe
exit $LASTEXITCODE
