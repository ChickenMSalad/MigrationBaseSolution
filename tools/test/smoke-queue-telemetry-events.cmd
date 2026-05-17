@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-queue-telemetry-events.ps1" %*
endlocal
