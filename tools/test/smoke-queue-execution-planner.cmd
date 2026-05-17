@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-queue-execution-planner.ps1" %*
endlocal
