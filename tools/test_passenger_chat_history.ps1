$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskOut=Join-Path $taskRoot 'artifacts/compact-nav-chat'
New-Item -ItemType Directory -Force -Path $taskOut | Out-Null
$taskCompiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$taskExe=Join-Path $taskOut 'PassengerChatHistoryTests.exe'
& $taskCompiler /nologo "/out:$taskExe" (Join-Path $taskRoot 'tests/PassengerChatHistoryTests.cs') (Join-Path $taskRoot 'unity/EungyeTaxi/Assets/Scripts/PassengerChatHistory.cs') (Join-Path $taskRoot 'unity/EungyeTaxi/Assets/Scripts/PassengerPresentation.cs')
if($LASTEXITCODE -ne 0){throw 'Passenger chat history compile failed'}
& $taskExe
if($LASTEXITCODE -ne 0){throw 'Passenger chat history checks failed'}
