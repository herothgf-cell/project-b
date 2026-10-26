$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot '../unity/EungyeTaxi/Assets/Scripts/MannerLedger.cs'
Add-Type -Path $source
$ledger = [MannerLedger]::new()
$now = 1.0
function Check([bool]$condition, [string]$name) {
    if (-not $condition) { throw "FAIL $name" }
    Write-Output "PASS $name"
}
function Recover([MannerLedger]$value, [int]$amount) {
    $method = [MannerLedger].GetMethod('RecoverStable')
    if ($null -eq $method) { throw 'FAIL stable recovery API exists' }
    return [int]$method.Invoke($value, @($amount))
}
$ledger.Penalize('red', 12, $now, 0) | Out-Null
Check ((Recover $ledger 2) -eq 0 -and $ledger.Score -eq 88) 'law loss cannot recover'
$ledger.Reset()
$ledger.Penalize('brake', 2, $now, 0) | Out-Null
Check ((Recover $ledger 2) -eq 2 -and $ledger.Score -eq 100) 'braking loss recovers to 100'
$ledger.Reset()
$ledger.Penalize('accel', 4, $now, 0) | Out-Null
$ledger.Penalize('brake', 4, $now, 0) | Out-Null
Recover $ledger 2 | Out-Null
Recover $ledger 2 | Out-Null
Recover $ledger 2 | Out-Null
Check ((Recover $ledger 2) -eq 0 -and $ledger.Score -eq 98) 'trip recovery capped at six'
$ledger.Reset()
$ledger.Penalize('accel', 4, $now, 0) | Out-Null
$ledger.Penalize('red', 12, $now, 0) | Out-Null
Check ((Recover $ledger 4) -eq 4 -and $ledger.Score -eq 88) 'law loss remains after motion recovery'
$progressType = 'StableDrivingProgress' -as [type]
Check ($null -ne $progressType) 'stable distance and time progress API exists'
$progress = [Activator]::CreateInstance($progressType)
$progress.Observe(0, 0, 1, 1, $true) | Out-Null
$earned = $false
for ($x = 1; $x -le 60; $x++) { $earned = $progress.Observe($x, 0, 1, 1, $true) -or $earned }
Check ($earned) 'sixty seconds of new moving path earns a segment'
$snapshot = $progress.Capture()
$restored = [Activator]::CreateInstance($progressType)
$restored.Restore($snapshot)
$restored.Rebase(60, 0)
$farmed = $false
for ($x = 59; $x -ge 0; $x--) { $farmed = $restored.Observe($x, 0, 1, 1, $true) -or $farmed }
Check (-not $farmed) 'reverse traversal of saved path earns nothing'
for ($i = 0; $i -lt 100; $i++) { $farmed = $restored.Observe(0, 0, 0, 1, $true) -or $farmed }
Check (-not $farmed) 'idle time earns nothing'
$partial = [Activator]::CreateInstance($progressType)
$partial.Observe(0, 0, 1, 1, $true) | Out-Null
for ($x = 1; $x -le 30; $x++) { $partial.Observe($x, 0, 1, 1, $true) | Out-Null }
$resumed = [Activator]::CreateInstance($progressType)
$resumed.Restore($partial.Capture())
$resumed.Rebase(30, 0)
$earned = $false
for ($x = 31; $x -le 60; $x++) { $earned = $resumed.Observe($x, 0, 1, 1, $true) -or $earned }
Check ($earned) 'saved partial moving time completes on a new path'
$slow = [Activator]::CreateInstance($progressType)
$slow.Observe(0, 0, 0.5, 1, $true) | Out-Null
$earned = $false
for ($step = 1; $step -le 60; $step++) { $earned = $slow.Observe($step * 0.5, 0, 0.5, 1, $true) -or $earned }
Check ($earned) 'sixty moving seconds also works at the minimum accepted speed'
$broken = [Activator]::CreateInstance($progressType)
$broken.Observe(0, 0, 1, 1, $true) | Out-Null
for ($x = 1; $x -le 30; $x++) { $broken.Observe($x, 0, 1, 1, $true) | Out-Null }
$broken.Observe(30, 0, 1, 1, $false) | Out-Null
$earned = $false
for ($x = 31; $x -le 60; $x++) { $earned = $broken.Observe($x, 0, 1, 1, $true) -or $earned }
Check (-not $earned) 'harsh motion breaks the stable segment'
$distance = [Activator]::CreateInstance($progressType)
$distance.Observe(0, 0, 10, 0.1, $true) | Out-Null
$earned = $false
for ($x = 1; $x -le 500; $x++) { $earned = $distance.Observe($x, 0, 10, 0.1, $true) -or $earned }
Check ($earned) 'five hundred metres of new moving path earns a segment'
Write-Output 'MANNER_RECOVERY_MODEL_OK'
