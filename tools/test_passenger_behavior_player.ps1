param([switch]$Capture, [switch]$Visible, [int]$TimeoutSeconds=240)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskExe=Join-Path $taskRoot 'builds/EungyeTaxi/EungyeTaxi.exe'
$taskArtifacts=Join-Path $taskRoot 'artifacts'
New-Item -ItemType Directory -Force -Path $taskArtifacts | Out-Null
if (!(Test-Path -LiteralPath $taskExe)) {throw 'Build the default Eungye player first'}
foreach ($taskCase in @('passengerBehaviorTest','passengerBehaviorReloadTest')) {
    $taskLog=Join-Path $taskArtifacts "$taskCase.log"
    $taskReport=Join-Path $taskArtifacts $(if ($taskCase -eq 'passengerBehaviorTest') {'passenger-behavior-player.txt'} else {'passenger-behavior-reload.txt'})
    $taskArgs=@('-oneDayMode','-hybridV2',"-$taskCase",'-logFile',('"'+$taskLog+'"'))
    if ($Capture) {$taskArgs+=@('-passengerBehaviorCapture','-screen-width','1280','-screen-height','720','-screen-fullscreen','0')}
    else {$taskArgs+=@('-batchmode','-nographics')}
    $taskStarted=[DateTime]::UtcNow
    # Visible is explicit opt-in for an authorized interactive UI check.
    $taskWindowStyle=if($Visible){'Normal'}else{'Hidden'}
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
    if ($Capture -and $taskCase -eq 'passengerBehaviorTest') {
        Add-Type -AssemblyName System.Drawing
        foreach ($taskFrame in @('preparation','receipt','food-choice','sleeping')) {
            $taskImagePath=Join-Path $taskArtifacts "passenger-behavior-$taskFrame.png"
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
