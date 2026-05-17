@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0validate-auth-operations-stack.ps1" %*
endlocal
