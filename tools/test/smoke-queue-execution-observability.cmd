@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-queue-execution-observability.ps1" %*
endlocal
