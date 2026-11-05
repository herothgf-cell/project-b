$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskCompiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$taskSources=@((Join-Path $taskRoot 'tests/SlowRightTurnTests.cs'))
$taskPolicy=Join-Path $taskRoot 'unity/EungyeTaxi/Assets/Scripts/SlowRightTurnState.cs'
if(Test-Path -LiteralPath $taskPolicy){$taskSources+=$taskPolicy}
$taskExe=Join-Path $taskRoot 'artifacts/SlowRightTurnTests.exe'
$taskCoreSources=@('ContentJson','ContentTuning') | ForEach-Object {Join-Path $taskRoot "unity/EungyeTaxi/Assets/Scripts/$_.cs"}
& $taskCompiler /nologo "/out:$taskExe" @taskSources @taskCoreSources
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
& $taskExe
exit $LASTEXITCODE
