@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0validate-p2-completion.ps1" %*
endlocal
