@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-endpoint-policy-inventory.ps1" %*
endlocal
