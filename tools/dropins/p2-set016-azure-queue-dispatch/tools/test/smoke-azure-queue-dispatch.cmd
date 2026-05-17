@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-azure-queue-dispatch.ps1" %*
endlocal
