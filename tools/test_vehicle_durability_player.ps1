param([switch]$Capture, [switch]$Visible, [int]$TimeoutSeconds=300)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskExe=Join-Path $taskRoot 'builds/EungyeTaxi/EungyeTaxi.exe'
$taskArtifacts=Join-Path $taskRoot 'artifacts'
New-Item -ItemType Directory -Force -Path $taskArtifacts | Out-Null
if (!(Test-Path -LiteralPath $taskExe)) {throw 'Build the default Eungye player first'}
$taskProject=(Join-Path $taskRoot 'unity/EungyeTaxi').Replace('\','/')
$taskProjectEditors=Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object {$_.CommandLine -and $_.CommandLine.Replace('\','/').Contains($taskProject)}
if ($taskProjectEditors) {throw 'Wait for this project Unity build to finish before launching this player check'}
foreach ($taskCase in @('vehicleDurabilityTest','vehicleDurabilityReloadTest','vehicleDurabilitySummaryReloadTest','vehicleDurabilityPreparationReloadTest','vehicleDurabilityPendingReloadTest')) {
    $taskLog=Join-Path $taskArtifacts "$taskCase.log"
    $taskReport=Join-Path $taskArtifacts (@{vehicleDurabilityTest='vehicle-durability-player.txt';vehicleDurabilityReloadTest='vehicle-durability-reload.txt';vehicleDurabilitySummaryReloadTest='vehicle-durability-summary-reload.txt';vehicleDurabilityPreparationReloadTest='vehicle-durability-preparation-reload.txt';vehicleDurabilityPendingReloadTest='vehicle-durability-pending-live-reload.txt'}[$taskCase])
    $taskArgs=@('-oneDayMode','-hybridV2',"-$taskCase",'-logFile',('"'+$taskLog+'"'))
    $taskCaptureThisCase=$Capture -and $taskCase -eq 'vehicleDurabilityTest'
    if ($taskCaptureThisCase) {$taskArgs+=@('-vehicleDurabilityCapture','-screen-width','1280','-screen-height','720','-screen-fullscreen','0')}
    else {$taskArgs+=@('-batchmode','-nographics')}
    $taskStarted=[DateTime]::UtcNow
    # Visible is explicit opt-in for an authorized interactive UI check.
    $taskWindowStyle=if($Visible -and $taskCaptureThisCase){'Normal'}else{'Hidden'}
    $taskProcess=Start-Process -FilePath $taskExe -ArgumentList $taskArgs -WorkingDirectory $taskRoot -WindowStyle $taskWindowStyle -PassThru
    $taskWatch=[Diagnostics.Stopwatch]::StartNew()
    while (!$taskProcess.WaitForExit(1000)) {
        if ($taskWatch.Elapsed.TotalSeconds -gt $TimeoutSeconds) {
            $taskProcess.Kill()
            throw "$taskCase timed out; stopped only the test process. See $taskLog"
        }
    }
    if ($taskProcess.ExitCode -ne 0) {throw "$taskCase exit $($taskProcess.ExitCode); see $taskLog"}
    if (!(Test-Path -LiteralPath $taskReport) -or (Get-Item -LiteralPath $taskReport).LastWriteTimeUtc -lt $taskStarted) {throw "$taskCase did not produce a fresh report"}
    if ((Get-Content -LiteralPath $taskReport -Raw) -notmatch '^PASS') {throw "$taskCase report is not PASS: $taskReport"}
    if ($Capture -and $taskCase -eq 'vehicleDurabilityTest') {
        Add-Type -AssemblyName System.Drawing
        foreach ($taskFrame in @('disabled','repair','summary')) {
            $taskImagePath=Join-Path $taskArtifacts "vehicle-durability-$taskFrame.png"
            if (!(Test-Path -LiteralPath $taskImagePath) -or (Get-Item -LiteralPath $taskImagePath).LastWriteTimeUtc -lt $taskStarted) {throw "Missing fresh capture: $taskFrame"}
            $taskImage=[Drawing.Bitmap]::new($taskImagePath)
            try {
                $taskColors=[Collections.Generic.HashSet[int]]::new()
                for($taskY=0;$taskY -lt $taskImage.Height;$taskY+=16) {
                    for($taskX=0;$taskX -lt $taskImage.Width;$taskX+=16) {$null=$taskColors.Add($taskImage.GetPixel($taskX,$taskY).ToArgb())}
                }
                if($taskColors.Count -lt 4){throw "Blank capture: $taskFrame. Functional PASS is not a visual PASS; rerun with explicit -Visible when permitted."}
            } finally {$taskImage.Dispose()}
        }
    }
    Write-Output "$taskCase exit 0 ($([Math]::Round($taskWatch.Elapsed.TotalSeconds,1)) seconds)"
}
