@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-azure-blob-storage-provider.ps1" %*
endlocal
