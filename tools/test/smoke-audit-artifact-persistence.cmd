@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-audit-artifact-persistence.ps1" %*
endlocal
