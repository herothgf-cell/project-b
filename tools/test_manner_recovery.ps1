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
$turnback = [Activator]::CreateInstance($progressType)
$turnback.Observe(0, 0, 1, 1, $true) | Out-Null
for ($x = 1; $x -le 50; $x++) { $turnback.Observe($x, 0, 1, 1, $true) | Out-Null }
$earned = $false
for ($x = 49; $x -ge 28; $x--) { $earned = $turnback.Observe($x, 0, 1, 1, $true) -or $earned }
Check (-not $earned) 'a pre-award out-and-back cannot turn fifty unique metres into sixty moving seconds'
$savedTurnback = [Activator]::CreateInstance($progressType)
$savedTurnback.Restore($turnback.Capture())
$savedTurnback.Rebase(28, 0)
$beforeSavedReturn = $savedTurnback.Capture()
for ($x = 29; $x -le 50; $x++) { $savedTurnback.Observe($x, 0, 1, 1, $true) | Out-Null }
Check ($savedTurnback.Capture().seconds -eq $beforeSavedReturn.seconds) 'save and restore retain closed cells on a repeated path'
$within = [Activator]::CreateInstance($progressType)
$within.Observe(0, 0, 1, 1, $true) | Out-Null
for ($x = 1; $x -le 9; $x++) { $within.Observe($x, 0, 1, 1, $true) | Out-Null }
$beforeReturn = $within.Capture()
for ($x = 8; $x -ge 1; $x--) { $within.Observe($x, 0, 1, 1, $true) | Out-Null }
$afterReturn = $within.Capture()
Check ($afterReturn.seconds -eq $beforeReturn.seconds -and $afterReturn.distance -eq $beforeReturn.distance) 'retrace inside the active ten-metre cell earns no distance or moving time'
$savedWithin = [Activator]::CreateInstance($progressType)
$savedWithin.Restore($beforeReturn)
$savedWithin.Rebase(9, 0)
for ($x = 8; $x -ge 1; $x--) { $savedWithin.Observe($x, 0, 1, 1, $true) | Out-Null }
Check ($savedWithin.Capture().seconds -eq $beforeReturn.seconds) 'save and restore retain the active-cell frontier'
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
