@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-queue-worker-loop-diagnostics.ps1" %*
endlocal
