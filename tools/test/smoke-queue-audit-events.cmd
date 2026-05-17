@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-queue-audit-events.ps1" %*
endlocal
