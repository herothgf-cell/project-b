@echo off
if not exist "%~dp0builds\Route42Taxi\Route42Taxi.exe" (
  echo Build PrototypeBuild.BuildHd first.
  pause
  exit /b 1
)
start "" "%~dp0builds\Route42Taxi\Route42Taxi.exe" -route42
