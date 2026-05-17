@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0new-release-manifest.ps1" %*
endlocal
