@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0validate-audit-persistence-stack.ps1" %*
endlocal
