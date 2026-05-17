@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0validate-queue-execution-stack.ps1" %*
endlocal
