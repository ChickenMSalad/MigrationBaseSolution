@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-audit-event-writer.ps1" %*
endlocal
