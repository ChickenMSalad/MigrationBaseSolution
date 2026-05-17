@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-auth-policy-readiness.ps1" %*
endlocal
