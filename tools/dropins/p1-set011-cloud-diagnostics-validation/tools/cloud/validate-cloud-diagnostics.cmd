@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0validate-cloud-diagnostics.ps1" %*
endlocal
