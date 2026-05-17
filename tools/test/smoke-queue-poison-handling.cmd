@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-queue-poison-handling.ps1" %*
endlocal
