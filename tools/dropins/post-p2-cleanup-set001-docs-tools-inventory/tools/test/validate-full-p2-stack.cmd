@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0validate-full-p2-stack.ps1" %*
endlocal
