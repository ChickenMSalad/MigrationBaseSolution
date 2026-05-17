@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-queue-idempotency.ps1" %*
endlocal
