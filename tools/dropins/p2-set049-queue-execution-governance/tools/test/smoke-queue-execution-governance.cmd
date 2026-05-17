@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-queue-execution-governance.ps1" %*
endlocal
