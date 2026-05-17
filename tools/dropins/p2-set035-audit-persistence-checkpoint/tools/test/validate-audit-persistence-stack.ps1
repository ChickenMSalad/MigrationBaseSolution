param(
    [string]$BaseUrl = "http://localhost:5173",
    [switch]$ExpectArtifactStorage
)

$ErrorActionPreference = "Stop"

Write-Host "Audit Persistence Stack Validation"
Write-Host "Base URL             : $BaseUrl"
Write-Host "ExpectArtifactStorage: $ExpectArtifactStorage"
Write-Host ""

$checks = @(
    @{ Name = "audit persistence"; Script = ".\tools\test\smoke-audit-persistence.ps1"; Args = @("-BaseUrl", $BaseUrl) },
    @{ Name = "audit artifact persistence"; Script = ".\tools\test\smoke-audit-artifact-persistence.ps1"; Args = @("-BaseUrl", $BaseUrl) },
    @{ Name = "audit event writer"; Script = ".\tools\test\smoke-audit-event-writer.ps1"; Args = @("-BaseUrl", $BaseUrl) },
    @{ Name = "queue audit events"; Script = ".\tools\test\smoke-queue-audit-events.ps1"; Args = @("-BaseUrl", $BaseUrl) },
    @{ Name = "cloud operation audit"; Script = ".\tools\test\smoke-cloud-operation-audit.ps1"; Args = @("-BaseUrl", $BaseUrl) }
)

foreach ($check in $checks) {
    if (!(Test-Path $check.Script)) {
        throw "Missing validation script for $($check.Name): $($check.Script)"
    }

    Write-Host "Running $($check.Name)..."

    $args = @($check.Args)

    if ($ExpectArtifactStorage -and $check.Script -like "*smoke-audit-artifact-persistence.ps1") {
        $args += "-ExpectArtifactStorage"
    }

    & powershell -ExecutionPolicy Bypass -File $check.Script @args

    Write-Host ""
}

Write-Host "Audit persistence stack validation completed successfully."
