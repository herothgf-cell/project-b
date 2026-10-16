$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskCompiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$taskSources = @('TrafficRules', 'PrototypeState', 'CampaignAccount', 'EconomyLedger', 'PassengerRequest', 'RightPracticeState') | ForEach-Object { Join-Path $taskRoot "unity/YeongtongTaxi/Assets/Scripts/$_.cs" }
$taskOutput = Join-Path $taskRoot 'artifacts'
New-Item -ItemType Directory -Force -Path $taskOutput | Out-Null
$taskResults = @()
foreach ($taskName in @('PracticeConditionsTests', 'PrototypeStateTests', 'PrototypeCompletionTests', 'CampaignAccountTests', 'CampaignLifecycleTests', 'PassengerRequestTests', 'EconomyTests')) {
    $taskExe = Join-Path $taskOutput "$taskName.exe"
    & $taskCompiler /nologo "/out:$taskExe" (Join-Path $taskRoot "tests/$taskName.cs") @taskSources
    if ($LASTEXITCODE -ne 0) { throw "$taskName compilation failed" }
    $taskResult = & $taskExe
    if ($LASTEXITCODE -ne 0) { throw "$taskName failed: $taskResult" }
    $taskResults += $taskResult
    Write-Output $taskResult
}
$taskResults | Set-Content -Encoding utf8 (Join-Path $taskOutput 'prototype-model-tests.txt')
