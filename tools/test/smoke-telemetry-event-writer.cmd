@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-telemetry-event-writer.ps1" %*
endlocal
