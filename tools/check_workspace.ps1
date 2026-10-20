param([string]$Root = (Split-Path $PSScriptRoot -Parent), [switch]$Strict)
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
foreach ($taskInput in $taskManifest.inputs) {
    $taskPath = Join-Path $taskRoot $taskInput.path
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
