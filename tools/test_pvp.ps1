$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
# .NET SDK path: cross-platform runner also launches two real client processes.
$taskDotnet = Get-Command dotnet -ErrorAction SilentlyContinue
$taskHasSdk = $false
if ($taskDotnet) {
    $taskSdks = @(& $taskDotnet.Source --list-sdks 2>$null)
    $taskHasSdk = $LASTEXITCODE -eq 0 -and @($taskSdks | Where-Object { $_ -match '^(\d+)\.\d+\.\d+' -and [int]$Matches[1] -ge 8 }).Count -gt 0
}
if ($taskHasSdk) {
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
} finally {
    $taskTempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
    $taskResolved = [IO.Path]::GetFullPath($taskTemp)
    if (-not $taskResolved.StartsWith($taskTempRoot,[StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected diagnostic cleanup path' }
    if (Test-Path -LiteralPath $taskResolved) { Remove-Item -LiteralPath $taskResolved -Recurse -Force }
}
