@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-audit-persistence.ps1" %*
endlocal
