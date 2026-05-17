@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-storage-provider-stack.ps1" %*
endlocal
