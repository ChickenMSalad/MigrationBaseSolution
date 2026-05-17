@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0new-cloud-appsettings-from-outputs.ps1" %*
endlocal
