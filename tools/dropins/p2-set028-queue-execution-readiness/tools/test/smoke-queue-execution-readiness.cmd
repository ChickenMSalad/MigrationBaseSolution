@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-queue-execution-readiness.ps1" %*
endlocal
