@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-queue-failure-handler.ps1" %*
endlocal
