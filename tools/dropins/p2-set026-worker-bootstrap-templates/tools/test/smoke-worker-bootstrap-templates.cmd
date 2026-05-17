@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-worker-bootstrap-templates.ps1" %*
endlocal
