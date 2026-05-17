@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-operational-mode.ps1" %*
endlocal
