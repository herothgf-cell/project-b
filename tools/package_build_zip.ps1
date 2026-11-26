# 공유용 빌드 압축: builds/EungyeTaxi 를 manifest(크기·SHA256)로 확인한 뒤
# 같은 builds/ 폴더에 EungyeTaxi-<날짜>-<커밋>.zip 을 만든다. builds/*.zip 은 Git 제외 대상이다.
# 사용: powershell -ExecutionPolicy Bypass -File tools\package_build_zip.ps1 [-IncludeDebugInfo] [-Force]
# Windows PowerShell 5.1 과 PowerShell 7 모두 동작한다.
param([switch]$IncludeDebugInfo, [switch]$Force, [string]$Root = (Split-Path $PSScriptRoot -Parent))
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem

$repo = [IO.Path]::GetFullPath($Root)
$buildDir = Join-Path $repo 'builds\EungyeTaxi'
$manifestPath = Join-Path $repo 'distribution\EungyeTaxi-build-manifest.json'
$prefix = 'builds/EungyeTaxi/'
$utf8 = New-Object System.Text.UTF8Encoding($false)
$manifest = [IO.File]::ReadAllText($manifestPath, $utf8) | ConvertFrom-Json

# 1) manifest 대조: 빠진 파일·크기·해시가 다르면 중단
$entries = @()
[long]$total = 0
foreach ($f in $manifest.files) {
    if (-not $f.path.StartsWith($prefix, [StringComparison]::Ordinal)) { throw "manifest 경로 오류: $($f.path)" }
    $rel = $f.path.Substring($prefix.Length)
    if ($rel.Contains('..')) { throw "manifest 경로 오류: $($f.path)" }
    $full = Join-Path $buildDir ($rel -replace '/', '\')
    if (-not (Test-Path -LiteralPath $full)) { throw "빌드 파일 없음: $($f.path)" }
    $item = Get-Item -LiteralPath $full
    if ($item.Length -ne $f.bytes -or (Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash -ne $f.sha256) {
        throw "manifest와 다름(빌드 후 manifest 갱신 필요): $($f.path)"
    }
    $total += $item.Length
    if (-not $IncludeDebugInfo -and $rel -like '*_BurstDebugInformation_DoNotShip/*') { continue }
    $entries += [pscustomobject]@{ Rel = $rel; Full = $full }
}
if ($total -ne $manifest.totalBytes) { throw 'manifest 전체 크기 불일치' }
Write-Output "MANIFEST_OK files=$($manifest.files.Count) bytes=$total"

# 2) 압축: zip 안 최상위 폴더는 EungyeTaxi/
$date = Get-Date -Format 'yyyyMMdd'
$commit = (& git -C $repo rev-parse --short HEAD).Trim()
$zipPath = Join-Path $repo "builds\EungyeTaxi-$date-$commit.zip"
if (Test-Path -LiteralPath $zipPath) {
    if (-not $Force) { throw "이미 있음: $zipPath (덮어쓰려면 -Force)" }
    Remove-Item -LiteralPath $zipPath
}
$tmp = "$zipPath.partial"
if (Test-Path -LiteralPath $tmp) { Remove-Item -LiteralPath $tmp }
$zip = [IO.Compression.ZipFile]::Open($tmp, [IO.Compression.ZipArchiveMode]::Create, $utf8)
try {
    foreach ($e in $entries) {
        [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $e.Full, "EungyeTaxi/$($e.Rel)", [IO.Compression.CompressionLevel]::Optimal)
    }
} finally { $zip.Dispose() }

# 3) 압축 결과 재확인: 항목 수와 원본 크기 합
$check = [IO.Compression.ZipFile]::OpenRead($tmp)
try {
    [long]$sum = 0; foreach ($z in $check.Entries) { $sum += $z.Length }
    if ($check.Entries.Count -ne $entries.Count) { throw 'zip 항목 수 불일치' }
} finally { $check.Dispose() }
Move-Item -LiteralPath $tmp -Destination $zipPath
$size = (Get-Item -LiteralPath $zipPath).Length
$hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash
Write-Output "ZIP_OK path=$zipPath entries=$($entries.Count) uncompressed=$sum zipBytes=$size sha256=$hash"
