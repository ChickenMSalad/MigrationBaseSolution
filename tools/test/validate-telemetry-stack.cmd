@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0validate-telemetry-stack.ps1" %*
endlocal
