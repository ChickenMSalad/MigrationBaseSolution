@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-auth-enforcement-diagnostics.ps1" %*
endlocal
