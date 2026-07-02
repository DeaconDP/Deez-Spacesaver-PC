@echo off
cd /d "%~dp0"
dotnet run --project "%~dp0src\Spacesaver\Spacesaver.csproj" -c Debug
if errorlevel 1 pause
