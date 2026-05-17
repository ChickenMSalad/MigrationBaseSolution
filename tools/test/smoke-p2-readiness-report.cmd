@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-p2-readiness-report.ps1" %*
endlocal
