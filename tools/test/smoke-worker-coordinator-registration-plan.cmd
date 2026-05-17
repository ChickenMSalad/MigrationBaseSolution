@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-worker-coordinator-registration-plan.ps1" %*
endlocal
