param(
    [string]$BaseUrl = "http://localhost:5173",
    [switch]$AllowUnconfigured,
    [switch]$ExpectArtifactStorage
)

$ErrorActionPreference = "Stop"

Write-Host "P2 Completion Validation"
Write-Host "Base URL             : $BaseUrl"
Write-Host "AllowUnconfigured    : $AllowUnconfigured"
Write-Host "ExpectArtifactStorage: $ExpectArtifactStorage"
Write-Host ""

$fullArgs = @("-BaseUrl", $BaseUrl)

if ($AllowUnconfigured) {
    $fullArgs += "-AllowUnconfigured"
}

if ($ExpectArtifactStorage) {
    $fullArgs += "-ExpectArtifactStorage"
}

if (!(Test-Path ".\tools\test\validate-full-p2-stack.ps1")) {
    throw "Missing full P2 validator: .\tools\test\validate-full-p2-stack.ps1"
}

powershell -ExecutionPolicy Bypass -File .\tools\test\validate-full-p2-stack.ps1 @fullArgs

Write-Host ""
Write-Host "Running final P2 readiness report smoke test..."

if (!(Test-Path ".\tools\test\smoke-p2-readiness-report.ps1")) {
    throw "Missing P2 readiness report smoke test: .\tools\test\smoke-p2-readiness-report.ps1"
}

powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-p2-readiness-report.ps1 -BaseUrl $BaseUrl

Write-Host ""
Write-Host "P2 completion validation completed successfully."
