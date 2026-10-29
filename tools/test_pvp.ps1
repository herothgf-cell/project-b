$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
# .NET SDK path: cross-platform runner also launches two real client processes.
if (Get-Command dotnet -ErrorAction SilentlyContinue) {
    $taskPython = Get-Command python -ErrorAction SilentlyContinue
    if (-not $taskPython) { $taskPython = Get-Command python3 -ErrorAction SilentlyContinue }
    if ($taskPython) {
        & $taskPython.Source (Join-Path $PSScriptRoot 'test_pvp.py') --dotnet (Get-Command dotnet).Source
        if ($LASTEXITCODE -ne 0) { throw 'PVP tests failed' }
        exit 0
    }
}
# Existing repository convention: Windows .NET Framework compiler, no new package.
$taskCompiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (-not (Test-Path $taskCompiler)) { throw 'Use .NET SDK 8+ with Python, or the Windows .NET Framework C# compiler.' }
$taskBase = Join-Path $taskRoot 'unity/EungyeTaxi/Assets/Pvp'
$taskSources = @('Runtime/Core/PvpSessionConfig.cs', 'Runtime/Core/PvpBuildTransaction.cs', 'Runtime/Persistence/PvpLocalStore.cs', 'Tests/EditMode/PvpIsolationTests.cs') | ForEach-Object { Join-Path $taskBase $_ }
$taskOutput = Join-Path $taskRoot 'artifacts/pvp-tests'
New-Item -ItemType Directory -Force -Path $taskOutput | Out-Null
$taskExe = Join-Path $taskOutput 'PvpIsolationTests.exe'
& $taskCompiler /nologo "/out:$taskExe" (Join-Path $taskRoot 'tests/PvpIsolationTests.cs') @taskSources
if ($LASTEXITCODE -ne 0) { throw 'PVP test compilation failed' }
& $taskExe
if ($LASTEXITCODE -ne 0) { throw 'PVP isolation tests failed' }
$taskTemp = Join-Path ([IO.Path]::GetTempPath()) ('pvp-two-client-' + [Guid]::NewGuid().ToString('N'))
try {
    New-Item -ItemType Directory -Force -Path $taskTemp | Out-Null
    $taskLogs = @(1, 2 | ForEach-Object { Join-Path $taskTemp "client-$_.txt" })
    $taskProcesses = @($taskLogs | ForEach-Object { Start-Process -FilePath $taskExe -ArgumentList @('--diagnostic', ('"' + $taskTemp + '"')) -RedirectStandardOutput $_ -NoNewWindow -PassThru })
    foreach ($taskProcess in $taskProcesses) { $taskProcess.WaitForExit(); if ($taskProcess.ExitCode -ne 0) { throw 'Diagnostic child failed' } }
    $taskPaths = @($taskLogs | ForEach-Object { (Get-Content $_ -Raw).Trim() })
    if ($taskPaths[0] -eq $taskPaths[1]) { throw 'Diagnostic filenames collided' }
    $taskContents = @($taskPaths | ForEach-Object { Get-Content $_ -Raw })
    if ($taskContents[0] -eq $taskContents[1]) { throw 'Diagnostic client contents collided' }
    Write-Output 'PVP_TWO_PROCESS_DIAGNOSTICS_OK clients=2 distinct=2'
} finally { if (Test-Path $taskTemp) { Remove-Item -Recurse -Force $taskTemp } }
