@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-artifact-storage-bridge.ps1" %*
endlocal
