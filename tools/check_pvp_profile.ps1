param([Parameter(Mandatory)][string]$Before,[Parameter(Mandatory)][string]$After)
$ErrorActionPreference='Stop'
function Read-Profile([string]$Path){
 $lines=Get-Content -LiteralPath $Path
 if($lines[0] -notmatch '^frames=(\d+)$'){throw 'Missing profiler frame count'}
 $frames=[int]$Matches[1]
 if($frames -lt 100){throw 'Need at least 100 measured frames'}
 return @{Frames=$frames;Rows=@($lines | Select-Object -Skip 1 | ConvertFrom-Csv)}
}
$old=Read-Profile $Before
$new=Read-Profile $After
$culture=[Globalization.CultureInfo]::InvariantCulture
function Number($Value){return [double]::Parse([string]$Value,$culture)}
$marker='Assembly-CSharp.dll!::DrivingLaw.FixedUpdate() [Invoke]'
$a=$old.Rows | Where-Object name -eq $marker
$b=$new.Rows | Where-Object name -eq $marker
$gcA=$old.Rows | Where-Object name -eq 'GC.Alloc'
$gcB=$new.Rows | Where-Object name -eq 'GC.Alloc'
if(!$a -or !$b -or !$gcA -or !$gcB){throw 'Required timing/allocation markers missing'}
$lawBefore=(Number $a.totalMs)/(Number $a.count)
$lawAfter=(Number $b.totalMs)/(Number $b.count)
$gcBefore=(Number $gcA.allocatedBytes)/$old.Frames
$gcAfter=(Number $gcB.allocatedBytes)/$new.Frames
Write-Output "Law average ms: $lawBefore -> $lawAfter; max ms: $($b.maxMs)"
Write-Output "GC bytes/frame: $gcBefore -> $gcAfter"
if($lawAfter -ge $lawBefore*.5 -or (Number $b.maxMs) -gt 5 -or $gcAfter -ge $gcBefore*.3){throw 'PvP CPU/allocation regression budget exceeded'}
Write-Output 'PVP_PROFILE_IMPROVEMENT_OK (CPU profile only; not a GPU/remote-PC acceptance test)'
