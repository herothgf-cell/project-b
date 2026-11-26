@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\package_build_zip.ps1" -Force
if errorlevel 1 (
  echo.
  echo [FAILED] Zip was not created. See the message above.
  pause
  exit /b 1
)
echo.
echo [OK] Zip created in the builds folder.
start "" explorer.exe "%~dp0builds"
pause
