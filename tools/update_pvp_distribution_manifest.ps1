param([string]$SourceCommit,[string]$ValidatedOn,[switch]$VerifyOnly)
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
$taskOutput=[ordered]@{formatVersion=1;sourceCommit=$SourceCommit;branchBaseCommit=(& git -C $taskRoot rev-parse HEAD).Trim();unity='2022.3.62f2';platform='Windows64';entrypoint='builds/PvpPrototype/PvpPrototype.exe';validatedOn=$ValidatedOn;totalBytes=($taskEntries|Measure-Object bytes -Sum).Sum;validation=[ordered]@{existingVerifiedBuild=$true;sourceRebuiltForPackaging=$false;copiedRuntimeFolderHashVerified=$true;copiedRuntimeIsolation='exit 0 / PVP_RUNTIME_ISOLATION_OK';singleDistribution='130 files / 112193174 bytes / SHA256 verified';differentPhysicalPC=$false;notes='Existing competition release build; concurrent uncommitted NPC/driving source is excluded. Runtime copy launched outside Unity project/cache. Headless smoke is not visual or cross-PC LAN acceptance.'};files=$taskEntries}
$taskOutput|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $taskManifest -Encoding utf8
Write-Output "PVP_DISTRIBUTION_MANIFEST_WRITTEN files=$($taskEntries.Count)"
