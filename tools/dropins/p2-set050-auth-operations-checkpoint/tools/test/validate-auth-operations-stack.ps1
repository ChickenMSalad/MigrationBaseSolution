param(
    [string]$BaseUrl = "http://localhost:5173"
)

$ErrorActionPreference = "Stop"

Write-Host "Auth + Operations Stack Validation"
Write-Host "Base URL: $BaseUrl"
Write-Host ""

$checks = @(
    @{ Name = "auth policy readiness"; Script = ".\tools\test\smoke-auth-policy-readiness.ps1"; Args = @("-BaseUrl", $BaseUrl) },
    @{ Name = "endpoint policy inventory"; Script = ".\tools\test\smoke-endpoint-policy-inventory.ps1"; Args = @("-BaseUrl", $BaseUrl) },
    @{ Name = "credential access policy readiness"; Script = ".\tools\test\smoke-credential-access-policy-readiness.ps1"; Args = @("-BaseUrl", $BaseUrl) },
    @{ Name = "auth enforcement diagnostics"; Script = ".\tools\test\smoke-auth-enforcement-diagnostics.ps1"; Args = @("-BaseUrl", $BaseUrl) },
    @{ Name = "production safety gates"; Script = ".\tools\test\smoke-production-safety-gates.ps1"; Args = @("-BaseUrl", $BaseUrl) },
    @{ Name = "operational mode"; Script = ".\tools\test\smoke-operational-mode.ps1"; Args = @("-BaseUrl", $BaseUrl) },
    @{ Name = "queue execution governance"; Script = ".\tools\test\smoke-queue-execution-governance.ps1"; Args = @("-BaseUrl", $BaseUrl) }
)

foreach ($check in $checks) {
    if (!(Test-Path $check.Script)) {
        throw "Missing validation script for $($check.Name): $($check.Script)"
    }

    Write-Host "Running $($check.Name)..."
    & powershell -ExecutionPolicy Bypass -File $check.Script @($check.Args)
    Write-Host ""
}

Write-Host "Auth + operations stack validation completed successfully."
