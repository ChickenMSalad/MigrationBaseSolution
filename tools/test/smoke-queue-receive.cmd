@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-queue-receive.ps1" %*
endlocal
