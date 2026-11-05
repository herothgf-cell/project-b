param([int]$Port = 0)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$node = Get-Command node -ErrorAction SilentlyContinue
if (-not $node) { Write-Host 'Node.js가 필요합니다. Node.js를 설치한 후 다시 실행하세요.' -ForegroundColor Yellow; Read-Host 'Enter 키를 누르면 닫습니다'; exit 1 }
$logDir = Join-Path $root 'artifacts/content-editor'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
$tag = [guid]::NewGuid().ToString('N')
$out = Join-Path $logDir "server-$tag.out.log"
$err = Join-Path $logDir "server-$tag.err.log"
$script = Join-Path $root 'tools/content-editor/server.mjs'
$process = Start-Process -FilePath $node.Source -ArgumentList @('"' + $script + '"','--root','"' + $root + '"','--port',"$Port") -WindowStyle Hidden -RedirectStandardOutput $out -RedirectStandardError $err -PassThru
for ($i=0; $i -lt 100; $i++) {
 Start-Sleep -Milliseconds 100
 if (Test-Path $out) { $line = Get-Content $out | Where-Object { $_ -like 'CONTENT_EDITOR_URL=*' } | Select-Object -First 1; if ($line) { $url = $line.Substring('CONTENT_EDITOR_URL='.Length); Write-Host "관리창 주소: $url"; Start-Process $url; exit 0 } }
 if ($process.HasExited) { break }
}
Write-Host '관리 서버를 시작하지 못했습니다.' -ForegroundColor Red
if (Test-Path $err) { Get-Content $err }
Read-Host 'Enter 키를 누르면 닫습니다'
exit 1
