@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-cloud-operation-audit.ps1" %*
endlocal
