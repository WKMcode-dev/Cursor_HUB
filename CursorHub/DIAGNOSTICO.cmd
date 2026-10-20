@echo off
cd /d "%~dp0"
dotnet build CursorHub.csproj
if errorlevel 1 goto done
dotnet "bin\Debug\net10.0-windows\CursorHub.dll" --verify-overlay "%~dp0diagnostico-overlay"
echo.
type "%~dp0diagnostico-overlay\overlay-results.json"
:done
echo.
pause
