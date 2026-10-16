@echo off
if not exist "%~dp0builds\EungyeTaxi\EungyeTaxi.exe" (
  echo Build the prototype using PrototypeBuild.BuildHybrid first.
  pause
  exit /b 1
)
start "" "%~dp0builds\EungyeTaxi\EungyeTaxi.exe" -eungye -screen-width 1600 -screen-height 900 -screen-fullscreen 0
