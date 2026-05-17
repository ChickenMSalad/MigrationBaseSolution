param(
    [string]$BaseUrl = "http://localhost:5173",
    [switch]$AllowUnconfigured,
    [switch]$ExpectArtifactStorage
)

$ErrorActionPreference = "Stop"

Write-Host "Operational Diagnostics Stack Validation"
Write-Host "Base URL             : $BaseUrl"
Write-Host "AllowUnconfigured    : $AllowUnconfigured"
Write-Host "ExpectArtifactStorage: $ExpectArtifactStorage"
Write-Host ""

$checks = @(
    @{
        Name = "queue execution stack"
        Script = ".\tools\test\validate-queue-execution-stack.ps1"
        Args = @("-BaseUrl", $BaseUrl)
        AllowUnconfigured = $true
        ExpectArtifactStorage = $false
    },
    @{
        Name = "audit persistence stack"
        Script = ".\tools\test\validate-audit-persistence-stack.ps1"
        Args = @("-BaseUrl", $BaseUrl)
        AllowUnconfigured = $false
        ExpectArtifactStorage = $true
    },
    @{
        Name = "telemetry stack"
        Script = ".\tools\test\validate-telemetry-stack.ps1"
        Args = @("-BaseUrl", $BaseUrl)
        AllowUnconfigured = $false
        ExpectArtifactStorage = $false
    },
    @{
        Name = "operational readiness rollup"
        Script = ".\tools\test\smoke-operational-readiness-rollups.ps1"
        Args = @("-BaseUrl", $BaseUrl)
        AllowUnconfigured = $false
        ExpectArtifactStorage = $false
    }
)

foreach ($check in $checks) {
    if (!(Test-Path $check.Script)) {
        throw "Missing validation script for $($check.Name): $($check.Script)"
    }

    Write-Host "Running $($check.Name)..."

    $args = @($check.Args)

    if ($AllowUnconfigured -and $check.AllowUnconfigured) {
        $args += "-AllowUnconfigured"
    }

    if ($ExpectArtifactStorage -and $check.ExpectArtifactStorage) {
        $args += "-ExpectArtifactStorage"
    }

    & powershell -ExecutionPolicy Bypass -File $check.Script @args

    Write-Host ""
}

Write-Host "Operational diagnostics stack validation completed successfully."
