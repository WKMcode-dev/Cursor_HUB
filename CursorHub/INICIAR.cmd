@echo off
cd /d "%~dp0"
dotnet run --project CursorHub.csproj
if errorlevel 1 (
  echo.
  echo Nao foi possivel iniciar o Cursor Hub. Copie o erro acima.
  pause
)
