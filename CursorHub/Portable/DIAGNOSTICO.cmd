@echo off
cd /d "%~dp0"
start "" /wait "%~dp0CursorHub.exe" --verify-overlay "%~dp0diagnostico-overlay"
echo.
if exist "%~dp0diagnostico-overlay\overlay-results.json" (
  type "%~dp0diagnostico-overlay\overlay-results.json"
) else (
  echo O diagnostico nao gerou um resultado. Verifique se toda a pasta foi extraida.
)
echo.
pause
