@echo off
if not exist "%~dp0builds\AssistedTaxi\AssistedTaxi.exe" (
  echo Build the assisted prototype using PrototypeBuild.BuildAssisted first.
  pause
  exit /b 1
)
start "" "%~dp0builds\AssistedTaxi\AssistedTaxi.exe" -roadRulesTrial
