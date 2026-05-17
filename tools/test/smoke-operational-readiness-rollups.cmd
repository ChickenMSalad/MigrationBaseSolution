@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-operational-readiness-rollups.ps1" %*
endlocal
