@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-cloud-credential-provider.ps1" %*
endlocal
