param([string]$SourceCommit,[string]$ValidatedOn,[switch]$VerifyOnly,[switch]$Rebuilt,[string]$ValidationReport)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskBuild=Join-Path $taskRoot 'builds/PvpPrototype'
$taskManifest=Join-Path $taskRoot 'distribution/PvpPrototype-build-manifest.json'
$taskFiles=@(Get-ChildItem -LiteralPath $taskBuild -File -Recurse)
$taskRequired=@('PvpPrototype.exe','UnityPlayer.dll','MonoBleedingEdge/EmbedRuntime/mono-2.0-bdwgc.dll','PvpPrototype_Data/globalgamemanagers','PvpPrototype_Data/ScriptingAssemblies.json','PvpPrototype_Data/Managed/Assembly-CSharp.dll','PvpPrototype_Data/Managed/Unity.Netcode.Runtime.dll','PvpPrototype_Data/Managed/Unity.Networking.Transport.dll','ThirdPartyNotices/NotoSansKR-OFL.txt','ThirdPartyNotices/mchrbn-unity-traffic-simulation-MIT.txt','ThirdPartyNotices/mchrbn-unity-traffic-simulation-NOTICE.txt')
foreach($taskRequiredFile in $taskRequired){if(!(Test-Path -LiteralPath (Join-Path $taskBuild $taskRequiredFile))){throw "Missing runtime file: $taskRequiredFile"}}
if($VerifyOnly){
    $taskData=Get-Content -LiteralPath $taskManifest -Raw -Encoding UTF8 | ConvertFrom-Json
    if($taskFiles.Count -ne $taskData.files.Count){throw 'PvP distribution file count mismatch'}
    [long]$taskBytes=0
    $taskSeen=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach($taskEntry in $taskData.files){
        if(!$taskEntry.path.StartsWith('builds/PvpPrototype/') -or !$taskSeen.Add($taskEntry.path)){throw 'Invalid or duplicate PvP manifest path'}
        $taskPath=[IO.Path]::GetFullPath((Join-Path $taskRoot $taskEntry.path))
        if(!$taskPath.StartsWith([IO.Path]::GetFullPath($taskBuild)+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'PvP manifest escapes runtime directory'}
        $taskFile=Get-Item -LiteralPath $taskPath
        if($taskFile.Length -ne $taskEntry.bytes -or (Get-FileHash -LiteralPath $taskPath -Algorithm SHA256).Hash -ne $taskEntry.sha256){throw "PvP hash mismatch: $($taskEntry.path)"}
        $taskBytes+=$taskFile.Length
    }
    if($taskBytes -ne $taskData.totalBytes){throw 'PvP total bytes mismatch'}
    Write-Output "PVP_DISTRIBUTION_OK files=$($taskFiles.Count) bytes=$taskBytes"
    exit 0
}
if($SourceCommit -notmatch '^[a-fA-F0-9]{40}$' -or $ValidatedOn -notmatch '^\d{4}-\d{2}-\d{2}$'){throw 'Pass the actual build source commit and explicit validation date'}
& git -C $taskRoot cat-file -e "$SourceCommit`^{commit}"
if($LASTEXITCODE -ne 0){throw 'Build source commit does not exist'}
$taskEntries=@($taskFiles | Sort-Object FullName | ForEach-Object {[pscustomobject][ordered]@{path=[IO.Path]::GetRelativePath($taskRoot,$_.FullName).Replace('\','/');bytes=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}})
$taskSingle=Get-Content -LiteralPath (Join-Path $taskRoot 'distribution/EungyeTaxi-build-manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$taskValidation=[ordered]@{existingVerifiedBuild=(!$Rebuilt);sourceRebuiltForPackaging=[bool]$Rebuilt;copiedRuntimeFolderHashVerified=$false;copiedRuntimeIsolation='not supplied';singleDistributionFiles=$taskSingle.files.Count;singleDistributionBytes=$taskSingle.totalBytes;differentPhysicalPC=$false;notes='Hash inventory; execution evidence is supplied separately.'}
if($ValidationReport){
    $taskReport=Get-Content -LiteralPath (Join-Path $taskRoot $ValidationReport) -Raw -Encoding UTF8 | ConvertFrom-Json
    if($taskReport.sourceCommit -ne $SourceCommit){throw 'Validation report source commit mismatch'}
    $taskValidation.copiedRuntimeFolderHashVerified=($taskReport.copyHashEqual -eq $true)
    $taskValidation.copiedRuntimeIsolation=if($taskReport.copyExit -eq 0 -and $taskReport.copyMarker -eq 'PVP_RUNTIME_ISOLATION_OK'){'exit 0 / PVP_RUNTIME_ISOLATION_OK'}else{'not verified'}
    $taskValidation['report']=$taskReport
}
if($Rebuilt -and (!$ValidationReport -or !$taskValidation.copiedRuntimeFolderHashVerified -or $taskValidation.copiedRuntimeIsolation -ne 'exit 0 / PVP_RUNTIME_ISOLATION_OK' -or $taskReport.buildExit -ne 0 -or $taskReport.driveBaselineExit -ne 0)){throw 'Rebuilt release requires matching build, driving and copied-runtime validation evidence'}
$taskOutput=[ordered]@{formatVersion=1;sourceCommit=$SourceCommit;branchBaseCommit=(& git -C $taskRoot rev-parse HEAD).Trim();unity='2022.3.62f2';platform='Windows64';entrypoint='builds/PvpPrototype/PvpPrototype.exe';validatedOn=$ValidatedOn;totalBytes=($taskEntries|Measure-Object bytes -Sum).Sum;validation=$taskValidation;files=$taskEntries}
$taskOutput|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $taskManifest -Encoding utf8
Write-Output "PVP_DISTRIBUTION_MANIFEST_WRITTEN files=$($taskEntries.Count)"
