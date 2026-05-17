@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-production-safety-gates.ps1" %*
endlocal
