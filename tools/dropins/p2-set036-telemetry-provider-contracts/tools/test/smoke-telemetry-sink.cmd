@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0smoke-telemetry-sink.ps1" %*
endlocal
