@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0validate-operational-diagnostics-stack.ps1" %*
endlocal
