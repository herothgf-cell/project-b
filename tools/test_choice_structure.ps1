param([string]$Root=(Split-Path $PSScriptRoot -Parent))
$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath($Root)
$taskAudit=Join-Path $taskRoot 'artifacts/choice-structure-audit'
New-Item -ItemType Directory -Force $taskAudit | Out-Null
$taskSource=[IO.File]::ReadAllText((Join-Path $taskRoot 'unity/EungyeTaxi/Assets/Scripts/PassengerRequest.cs'))
# Compile the actual pure request class, excluding the unrelated settlement/UI classes.
$taskBoundary=$taskSource.IndexOf('// Settlement is')
$taskEnum=[regex]::Match($taskSource,'(?m)^public enum PassengerReply[^\r\n]+').Value
if($taskBoundary -lt 1 -or !$taskEnum){throw 'PassengerRequest source boundary changed; update the audit extraction'}
$taskCore=Join-Path $taskAudit 'PassengerRequestCore.cs'
[IO.File]::WriteAllText($taskCore,$taskSource.Substring(0,$taskBoundary)+"`n"+$taskEnum+"`n",[Text.UTF8Encoding]::new($false))
$taskCompiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$taskExe=Join-Path $taskAudit 'ChoiceStructureAudit.exe'
$taskSources=@((Join-Path $taskRoot 'tests/ChoiceStructureAudit.cs'),$taskCore,
 (Join-Path $taskRoot 'unity/EungyeTaxi/Assets/Scripts/ContentJson.cs'),
 (Join-Path $taskRoot 'unity/EungyeTaxi/Assets/Scripts/ContentTuning.cs'))
& $taskCompiler /nologo "/out:$taskExe" @taskSources
if($LASTEXITCODE -ne 0){throw 'Choice structure audit compile failed'}
& $taskExe (Join-Path $taskRoot 'config/content/catalog.json') | Tee-Object -FilePath (Join-Path $taskAudit 'results.log')
if($LASTEXITCODE -ne 0){throw 'Choice structure audit failed'}
