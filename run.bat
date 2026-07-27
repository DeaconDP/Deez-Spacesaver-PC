@echo off
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -Command "Start-Process -FilePath powershell.exe -WorkingDirectory '%CD%' -WindowStyle Hidden -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-STA','-File','%~dp0run.ps1'"
