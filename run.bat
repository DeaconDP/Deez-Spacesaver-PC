@echo off
cd /d "%~dp0"

where dotnet >nul 2>&1 || (
  echo .NET 8 SDK required. Install from https://dotnet.microsoft.com/download/dotnet/8.0
  pause
  exit /b 1
)

echo Restoring packages...
dotnet restore Spacesaver.sln || (pause & exit /b 1)

echo Publishing PC-Spacesaver.exe...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0publish.ps1" || (pause & exit /b 1)

if not exist "%~dp0PC-Spacesaver.exe" (
  echo Publish finished but PC-Spacesaver.exe was not found.
  pause
  exit /b 1
)

echo Starting PC-Spacesaver.exe...
start "" "%~dp0PC-Spacesaver.exe"
