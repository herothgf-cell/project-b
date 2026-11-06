$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskOut=Join-Path $taskRoot 'artifacts/passenger-rest-adapter'
New-Item -ItemType Directory -Force $taskOut | Out-Null
$taskSource=Get-Content (Join-Path $taskRoot 'unity/EungyeTaxi/Assets/Scripts/PassengerJobs.cs') -Raw
function Read-ProductionMember([string]$taskSignature) {
 $taskStart=$taskSource.IndexOf($taskSignature,[StringComparison]::Ordinal)
 if($taskStart -lt 0){throw "Missing production member: $taskSignature"}
 $taskBrace=$taskSource.IndexOf('{',$taskStart)
 $taskDepth=0
 for($taskAt=$taskBrace;$taskAt -lt $taskSource.Length;$taskAt++) {
  if($taskSource[$taskAt] -eq '{'){$taskDepth++}
  elseif($taskSource[$taskAt] -eq '}'){$taskDepth--;if($taskDepth -eq 0){return $taskSource.Substring($taskStart,$taskAt-$taskStart+1)}}
 }
 throw "Unbalanced production member: $taskSignature"
}
# Compile these members verbatim, so the test executes the actual jobs adapter
# and its actual availability gate; no mirrored response-hold expression.
$taskMembers=@(Read-ProductionMember ' public bool BehaviorChoicesOpen';Read-ProductionMember ' public bool BehaviorAsleep';Read-ProductionMember ' public void AdvanceBehavior(')
$taskExtracted=Join-Path $taskOut 'PassengerJobs.RestAdapter.production.cs'
('using UnityEngine;'+[Environment]::NewLine+'public partial class PassengerJobs {'+[Environment]::NewLine+($taskMembers -join [Environment]::NewLine)+[Environment]::NewLine+'}') | Set-Content -LiteralPath $taskExtracted -Encoding utf8
$taskCompiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$taskExe=Join-Path $taskOut 'PassengerRestAdapterTests.exe'
$taskModels=@('PassengerBehavior','PassengerSituations','VehicleCare','FareTrip') | ForEach-Object {Join-Path $taskRoot "unity/EungyeTaxi/Assets/Scripts/$_.cs"}
$taskCoreSources=@('ContentJson','ContentTuning','ChoiceEffects') | ForEach-Object {Join-Path $taskRoot "unity/EungyeTaxi/Assets/Scripts/$_.cs"}
& $taskCompiler /nologo /langversion:5 "/out:$taskExe" (Join-Path $taskRoot 'tests/PassengerRestAdapterTests.cs') $taskExtracted @taskModels @taskCoreSources
if($LASTEXITCODE -ne 0){throw 'Passenger rest adapter compile failed'}
& $taskExe
if($LASTEXITCODE -ne 0){throw 'Passenger rest adapter tests failed'}
