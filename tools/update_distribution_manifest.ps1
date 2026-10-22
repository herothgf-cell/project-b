param([string]$SourceCommit, [string]$ValidatedOn = '2026-10-22', [switch]$VerifyOnly)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskManifestPath=Join-Path $taskRoot 'distribution/EungyeTaxi-build-manifest.json'
if ($VerifyOnly) {
    $taskManifest=Get-Content -LiteralPath $taskManifestPath -Raw | ConvertFrom-Json
    $taskActual=@(Get-ChildItem -LiteralPath (Join-Path $taskRoot 'builds/EungyeTaxi') -File -Recurse)
    if ($taskActual.Count -ne $taskManifest.files.Count) {throw 'Distribution file count mismatch'}
    [long]$taskTotal=0
    foreach ($taskFile in $taskManifest.files) {
        $taskPath=Join-Path $taskRoot $taskFile.path
        $taskItem=Get-Item -LiteralPath $taskPath
        if ($taskItem.Length -ne $taskFile.bytes -or (Get-FileHash -LiteralPath $taskPath -Algorithm SHA256).Hash -ne $taskFile.sha256) {throw "Distribution hash mismatch: $($taskFile.path)"}
        $taskTotal+=$taskItem.Length
    }
    if ($taskTotal -ne $taskManifest.totalBytes) {throw 'Distribution byte count mismatch'}
    Write-Output "DISTRIBUTION_OK files=$($taskActual.Count) bytes=$taskTotal"
    exit 0
}
if ($SourceCommit -notmatch '^[0-9a-fA-F]{40}$') {throw 'Pass the actual 40-character source commit hash after committing source changes'}
& git -C $taskRoot cat-file -e "$SourceCommit`^{commit}"
if ($LASTEXITCODE -ne 0) {throw 'Source commit does not exist'}
$taskEntries=@(Get-ChildItem -LiteralPath (Join-Path $taskRoot 'builds/EungyeTaxi') -File -Recurse | Sort-Object FullName | ForEach-Object {
    [pscustomobject][ordered]@{path=[IO.Path]::GetRelativePath($taskRoot,$_.FullName).Replace('\','/');bytes=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}
})
$taskOutput=[ordered]@{sourceCommit=$SourceCommit;unity='2022.3.62f2';region='eungye';entrypoint='builds/EungyeTaxi/EungyeTaxi.exe';validatedOn=$ValidatedOn;totalBytes=($taskEntries | Measure-Object bytes -Sum).Sum;validation=[ordered]@{build='PrototypeBuild.BuildEasyRoadMapEditor exit 0';modelSuites=23;campaignStateAssertions=20;presentationHudAssertions=20;editorCoreAssertions=15;easyEditorCoreAssertions=7;buttonResolutions=@(1280,1600,1920);editorExtendedTests=$true;editorPlayerAssertions=28;trialIsolation=$true;manualDriving=$false;fieldReviewComplete=$false;fieldWorkStoppedByUser=$true};files=$taskEntries}
$taskOutput | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $taskManifestPath -Encoding utf8
Write-Output "DISTRIBUTION_MANIFEST_WRITTEN files=$($taskEntries.Count)"
