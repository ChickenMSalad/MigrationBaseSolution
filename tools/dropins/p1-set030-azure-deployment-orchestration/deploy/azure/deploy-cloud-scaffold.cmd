@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0deploy-cloud-scaffold.ps1" %*
endlocal
