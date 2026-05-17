@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-queue-dispatch.ps1" %*
endlocal
