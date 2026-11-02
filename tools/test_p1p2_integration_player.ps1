param([int]$TimeoutSeconds=180)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskExe=Join-Path $taskRoot 'builds/EungyeTaxi/EungyeTaxi.exe'
$taskLog=Join-Path $taskRoot 'artifacts/p1p2-native-release.log'
$taskReport=Join-Path $taskRoot 'artifacts/p1p2-native-integration.txt'
New-Item -ItemType Directory -Path (Join-Path $taskRoot 'artifacts') -Force | Out-Null
$taskStarted=[DateTime]::UtcNow
$taskProcess=Start-Process -FilePath $taskExe -ArgumentList @('-batchmode','-nographics','-p1p2IntegrationTest','-hybridV2','-logFile',('"'+$taskLog+'"')) -WorkingDirectory $taskRoot -WindowStyle Hidden -PassThru
try {
 if(!$taskProcess.WaitForExit($TimeoutSeconds*1000)){throw 'P1P2 native integration timeout'}
 if($taskProcess.ExitCode -ne 0){throw "P1P2 native integration exit $($taskProcess.ExitCode); see $taskLog"}
 if(!(Test-Path -LiteralPath $taskReport) -or (Get-Item -LiteralPath $taskReport).LastWriteTimeUtc -lt $taskStarted -or (Get-Content -LiteralPath $taskReport -Raw) -notmatch '^PASS'){throw 'Missing fresh native integration PASS report'}
 if(!(Select-String -LiteralPath $taskLog -SimpleMatch 'P1P2_NATIVE_INTEGRATION_OK' -Quiet)){throw 'Missing native integration completion marker'}
 if(Select-String -LiteralPath $taskLog -Pattern 'Exception:|Assertion failed|NullReferenceException' -Quiet){throw 'Native integration log contains an error'}
 Write-Output 'P1P2_NATIVE_RUNNER_OK position-controlled adapters; not physical driving/manual play'
} finally {if(!$taskProcess.HasExited){Stop-Process -Id $taskProcess.Id -ErrorAction SilentlyContinue}}
