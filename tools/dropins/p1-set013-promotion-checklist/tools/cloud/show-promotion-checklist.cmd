@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0show-promotion-checklist.ps1" %*
endlocal
