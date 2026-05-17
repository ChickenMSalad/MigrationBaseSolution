@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-cloud-operation-telemetry.ps1" %*
endlocal
