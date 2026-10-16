@echo off
if not exist "%~dp0builds\YeongtongTaxi\YeongtongTaxi.exe" (
  echo Build the prototype using PrototypeBuild.Build first.
  pause
  exit /b 1
)
start "" "%~dp0builds\YeongtongTaxi\YeongtongTaxi.exe"
