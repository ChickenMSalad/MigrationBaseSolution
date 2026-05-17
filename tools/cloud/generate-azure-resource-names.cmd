@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0generate-azure-resource-names.ps1" %*
endlocal
