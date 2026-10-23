param([string]$Root = (Split-Path $PSScriptRoot -Parent), [switch]$Strict, [switch]$RequireRawInputs)
$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path -LiteralPath $Root).Path
$taskManifestPath = Join-Path $taskRoot 'config/maps/local-input-manifest.json'
$taskManifest = Get-Content -LiteralPath $taskManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
$taskIssues = 0
Write-Output ("Workspace: " + $taskRoot)
& git -C $taskRoot log -1 --oneline
$taskVersion = Get-Content -LiteralPath (Join-Path $taskRoot 'unity/YeongtongTaxi/ProjectSettings/ProjectVersion.txt') -Raw
if ($taskVersion -notmatch ('m_EditorVersion: ' + [regex]::Escape($taskManifest.unity) + '(\s|$)')) {
    Write-Output 'MISMATCH Unity project version'; $taskIssues++
} else { Write-Output ("OK Unity project " + $taskManifest.unity + ' (Editor installation is checked separately in Unity Hub)') }
$taskProjectPath=Join-Path $taskRoot 'config/maps/project/eungye.project.json'
$taskProjectReady=$false
if (Test-Path -LiteralPath $taskProjectPath -PathType Leaf) {
    try {
        $taskProject=Get-Content -LiteralPath $taskProjectPath -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 100
        if ($taskProject.schema -ne 1 -or $taskProject.baseline.mapId -ne 'eungye' -or -not $taskProject.document.scene.objects -or $taskProject.contentHash -notmatch '^[0-9a-f]{64}$') {throw 'Invalid project map structure'}
        $taskProjectReady=$true
        Write-Output ('OK Git project map ' + $taskProject.contentHash + ' (full geometry/checksum validation runs in BuildHybrid)')
    } catch {Write-Output ('INVALID project map: ' + $_.Exception.Message);$taskIssues++}
}
foreach ($taskInput in $taskManifest.inputs) {
    $taskPath = Join-Path $taskRoot $taskInput.path
    if ($taskProjectReady -and -not $RequireRawInputs -and -not (Test-Path -LiteralPath $taskPath -PathType Leaf)) {Write-Output ("OPTIONAL import input absent: " + $taskInput.path);continue}
    if (-not (Test-Path -LiteralPath $taskPath -PathType Leaf)) { Write-Output ("MISSING " + $taskInput.path); $taskIssues++; continue }
    $taskHash = (Get-FileHash -LiteralPath $taskPath -Algorithm SHA256).Hash
    if ($taskHash -ne $taskInput.sha256 -or (Get-Item -LiteralPath $taskPath).Length -ne $taskInput.bytes) {
        Write-Output ("MISMATCH " + $taskInput.path); $taskIssues++
    } else { Write-Output ("OK " + $taskInput.path) }
}
Write-Output 'Read docs/work-history/LATEST.md before continuing work.'
Write-Output 'Local changes (preserve these before switching PCs):'
& git -C $taskRoot status --short
Write-Output ("Input issues: " + $taskIssues)
if ($Strict -and $taskIssues -gt 0) { exit 1 }
