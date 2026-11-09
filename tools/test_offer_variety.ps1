$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskCompiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$taskOutput=Join-Path $taskRoot 'artifacts/pickup-variety'
New-Item -ItemType Directory -Force -Path $taskOutput | Out-Null
$taskMap=Get-Content (Join-Path $taskRoot 'config/maps/project/eungye.project.json') -Raw | ConvertFrom-Json
$taskCatalog=Get-Content (Join-Path $taskRoot 'config/maps/eungye-passenger-stops.json') -Raw | ConvertFrom-Json
$taskRows=@();$taskIndex=0
foreach($taskStop in @($taskMap.baseline.stops)+@($taskCatalog.candidates)){
 $taskId=if($taskIndex -lt $taskMap.baseline.stops.Count){'S'+($taskIndex+1).ToString('00')}else{$taskStop.stopId}
 $taskCol=[Math]::Max(0,[Math]::Min(2,[int][Math]::Floor($taskStop.x/$taskMap.baseline.width*3)))
 $taskRow=[Math]::Max(0,[Math]::Min(2,[int][Math]::Floor($taskStop.z/$taskMap.baseline.depth*3)))
 $taskRows+="$taskId`t$($taskRow*3+$taskCol)`t$($taskStop.x.ToString([Globalization.CultureInfo]::InvariantCulture))`t$($taskStop.z.ToString([Globalization.CultureInfo]::InvariantCulture))"
 $taskIndex++
}
$taskFixture=Join-Path $taskOutput 'catalog.tsv'
$taskRows | Set-Content -Encoding utf8 $taskFixture
$taskSources=@('OneDayOfferPolicy','PassengerDemandPolicy','ContentJson','ContentTuning','ChoiceEffects') | ForEach-Object {Join-Path $taskRoot "unity/EungyeTaxi/Assets/Scripts/$_.cs"}
$taskExe=Join-Path $taskOutput 'OfferVarietyTests.exe'
& $taskCompiler /nologo /langversion:5 "/out:$taskExe" (Join-Path $taskRoot 'tests/OfferVarietyTests.cs') @taskSources
if($LASTEXITCODE -ne 0){throw 'OfferVarietyTests compilation failed'}
& $taskExe $taskFixture
if($LASTEXITCODE -ne 0){throw 'OfferVarietyTests failed'}
