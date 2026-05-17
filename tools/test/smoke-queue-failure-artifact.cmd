@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-queue-failure-artifact.ps1" %*
endlocal
