param([string]$Executable,[int]$TimeoutSeconds=205,[switch]$Visible)
$ErrorActionPreference='Stop'
$storyRoot=Split-Path $PSScriptRoot -Parent
if(!$Executable){$Executable=Join-Path $storyRoot 'builds/EungyeTaxi/EungyeTaxi.exe'}
$storyOutput=Join-Path $storyRoot 'artifacts/passenger-story-player'
New-Item -ItemType Directory -Force $storyOutput | Out-Null
foreach($name in @('campaign.json','campaign.json.bak','matrix.json','matrix.json.bak','student-receipt.png')){
    $fixture=Join-Path $storyOutput $name
    if(Test-Path -LiteralPath $fixture){Remove-Item -LiteralPath $fixture}
}
foreach($phase in @('passengerStoryCheck','passengerStoryReloadCheck','passengerStoryMatrixCheck')){
    $storyLog=Join-Path $storyOutput "$phase.log"
    $storyArgs=@('-contentTest',"-$phase",'-screen-width','1600','-screen-height','900','-logFile',('"' + $storyLog + '"'))
    $windowStyle=if($Visible){'Normal'}else{'Hidden'}
    $storyProcess=Start-Process -FilePath $Executable -ArgumentList $storyArgs -WorkingDirectory $storyRoot -WindowStyle $windowStyle -PassThru
    $watch=[Diagnostics.Stopwatch]::StartNew()
    while(!$storyProcess.WaitForExit(1000)){
        if($watch.Elapsed.TotalSeconds -gt $TimeoutSeconds){$storyProcess.Kill();throw "Passenger story player timeout. $storyLog"}
    }
    if($storyProcess.ExitCode -ne 0){throw "Passenger story player exit $($storyProcess.ExitCode). $storyLog"}
    $marker=switch($phase){
        'passengerStoryCheck'{'PASSENGER_STORY_PLAYER_OK'}
        'passengerStoryReloadCheck'{'PASSENGER_STORY_RELOAD_OK'}
        'passengerStoryMatrixCheck'{'PASSENGER_STORY_MATRIX_OK'}
    }
    if((Get-Content -Raw -LiteralPath $storyLog) -notmatch $marker){throw "Missing $marker in $storyLog"}
    Write-Output "Passenger story player PASS $phase. $storyLog"
}
