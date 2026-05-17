@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-queue-executor-coordinator.ps1" %*
endlocal
