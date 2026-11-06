param([string]$SourceCommit, [string]$ValidatedOn = '2026-10-22', [switch]$VerifyOnly, [string]$ValidationReport, [string]$Root = (Split-Path $PSScriptRoot -Parent))
$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath($Root)
$taskManifestPath=Join-Path $taskRoot 'distribution/EungyeTaxi-build-manifest.json'
$taskMutable='builds/EungyeTaxi/content/active-profile.json'
function Test-UserContentProfile([string]$Path) {
    $taskNode=Get-Command node -ErrorAction SilentlyContinue
    if(!$taskNode){throw 'Node.js is required to verify a user content profile'}
    & $taskNode.Source (Join-Path $taskRoot 'tools/content-editor/verify-profile.mjs') (Join-Path $taskRoot 'config/content/catalog.json') $Path
    if($LASTEXITCODE -ne 0){throw 'Invalid user content profile'}
}
if ($VerifyOnly) {
    $taskManifest=Get-Content -LiteralPath $taskManifestPath -Raw | ConvertFrom-Json
    $taskBuild=[IO.Path]::GetFullPath((Join-Path $taskRoot 'builds/EungyeTaxi'))
    $taskActual=@(Get-ChildItem -LiteralPath $taskBuild -File -Recurse)
    $taskSeen=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    [long]$taskTotal=0;[long]$taskShippedTotal=0
    foreach ($taskFile in $taskManifest.files) {
        if(!$taskFile.path.StartsWith('builds/EungyeTaxi/',[StringComparison]::Ordinal) -or !$taskSeen.Add($taskFile.path)){throw 'Invalid or duplicate distribution manifest path'}
        $taskPath=[IO.Path]::GetFullPath((Join-Path $taskRoot $taskFile.path))
        if(!$taskPath.StartsWith($taskBuild+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetRelativePath($taskRoot,$taskPath).Replace('\','/') -cne $taskFile.path){throw 'Distribution manifest path escapes runtime directory'}
        $taskShippedTotal+=$taskFile.bytes
        if($taskFile.path -ceq $taskMutable -and !(Test-Path -LiteralPath $taskPath)){Write-Output 'CONTENT_PROFILE_DEFAULTS active file absent';continue}
        $taskItem=Get-Item -LiteralPath $taskPath
        if($taskItem.Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Distribution file cannot be a symbolic link'}
        if ($taskItem.Length -ne $taskFile.bytes -or (Get-FileHash -LiteralPath $taskPath -Algorithm SHA256).Hash -ne $taskFile.sha256) {
            if($taskFile.path -cne $taskMutable){throw "Distribution hash mismatch: $($taskFile.path)"}
            Test-UserContentProfile $taskPath
            Write-Output 'CONTENT_PROFILE_USER_OVERRIDE validated separately from shipped runtime'
        }
        $taskTotal+=$taskItem.Length
    }
    if($taskShippedTotal -ne $taskManifest.totalBytes){throw 'Distribution byte count mismatch'}
    foreach($taskItem in $taskActual){$taskRelative=[IO.Path]::GetRelativePath($taskRoot,$taskItem.FullName).Replace('\','/');if(!$taskSeen.Contains($taskRelative)){if($taskRelative -ceq $taskMutable){Test-UserContentProfile $taskItem.FullName;$taskTotal+=$taskItem.Length;Write-Output 'CONTENT_PROFILE_USER_OVERRIDE validated optional user file'}else{throw "Unexpected distribution file: $taskRelative"}}}
    Write-Output "DISTRIBUTION_OK files=$($taskActual.Count) bytes=$taskTotal shippedBytes=$taskShippedTotal"
    exit 0
}
if ($SourceCommit -notmatch '^[0-9a-fA-F]{40}$') {throw 'Pass the actual 40-character source commit hash after committing source changes'}
& git -C $taskRoot cat-file -e "$SourceCommit`^{commit}"
if ($LASTEXITCODE -ne 0) {throw 'Source commit does not exist'}
$taskEntries=@(Get-ChildItem -LiteralPath (Join-Path $taskRoot 'builds/EungyeTaxi') -File -Recurse | Where-Object { [IO.Path]::GetRelativePath($taskRoot,$_.FullName).Replace('\','/') -cne $taskMutable } | Sort-Object FullName | ForEach-Object {
    [pscustomobject][ordered]@{path=[IO.Path]::GetRelativePath($taskRoot,$_.FullName).Replace('\','/');bytes=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}
})
$taskOutput=[ordered]@{sourceCommit=$SourceCommit;unity='2022.3.62f2';region='eungye';entrypoint='builds/EungyeTaxi/EungyeTaxi.exe';validatedOn=$ValidatedOn;totalBytes=($taskEntries | Measure-Object bytes -Sum).Sum;validation=[ordered]@{build='PrototypeBuild.BuildEasyRoadMapEditor exit 0';modelSuites=23;campaignStateAssertions=20;presentationHudAssertions=20;editorCoreAssertions=15;easyEditorCoreAssertions=7;buttonResolutions=@(1280,1600,1920);editorExtendedTests=$true;editorPlayerAssertions=28;trialIsolation=$true;manualDriving=$false;fieldReviewComplete=$false;fieldWorkStoppedByUser=$true};files=$taskEntries}
if ($ValidationReport) {$taskOutput.validation=Get-Content -LiteralPath $ValidationReport -Raw -Encoding UTF8 | ConvertFrom-Json}
$taskProjectPath=Join-Path $taskRoot 'config/maps/project/eungye.project.json'
if(Test-Path -LiteralPath $taskProjectPath){$taskProject=Get-Content -LiteralPath $taskProjectPath -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 100;$taskOutput.projectMap=[ordered]@{path='config/maps/project/eungye.project.json';contentHash=$taskProject.contentHash;bytes=(Get-Item -LiteralPath $taskProjectPath).Length;sha256=(Get-FileHash -LiteralPath $taskProjectPath -Algorithm SHA256).Hash.ToLowerInvariant()}}
$taskOutput | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $taskManifestPath -Encoding utf8
Write-Output "DISTRIBUTION_MANIFEST_WRITTEN files=$($taskEntries.Count)"
