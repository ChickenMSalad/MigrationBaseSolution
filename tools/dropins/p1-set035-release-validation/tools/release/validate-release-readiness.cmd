@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0validate-release-readiness.ps1" %*
endlocal
