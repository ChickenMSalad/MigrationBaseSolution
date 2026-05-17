@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-queue-worker-loop.ps1" %*
endlocal
