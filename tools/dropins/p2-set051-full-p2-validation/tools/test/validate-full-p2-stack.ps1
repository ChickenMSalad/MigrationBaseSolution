param(
    [string]$BaseUrl = "http://localhost:5173",
    [switch]$AllowUnconfigured,
    [switch]$ExpectArtifactStorage
)

$ErrorActionPreference = "Stop"

Write-Host "Full P2 Stack Validation"
Write-Host "Base URL             : $BaseUrl"
Write-Host "AllowUnconfigured    : $AllowUnconfigured"
Write-Host "ExpectArtifactStorage: $ExpectArtifactStorage"
Write-Host ""

$validators = @(
    @{
        Name = "Operational diagnostics stack"
        Script = ".\tools\test\validate-operational-diagnostics-stack.ps1"
        Args = @("-BaseUrl", $BaseUrl)
    },
    @{
        Name = "Auth + operations stack"
        Script = ".\tools\test\validate-auth-operations-stack.ps1"
        Args = @("-BaseUrl", $BaseUrl)
    }
)

foreach ($validator in $validators) {
    if (!(Test-Path $validator.Script)) {
        throw "Missing validator: $($validator.Script)"
    }

    Write-Host "Running $($validator.Name)..."

    $args = @($validator.Args)

    if ($AllowUnconfigured) {
        $args += "-AllowUnconfigured"
    }

    if ($ExpectArtifactStorage) {
        $args += "-ExpectArtifactStorage"
    }

    & powershell -ExecutionPolicy Bypass -File $validator.Script @args

    Write-Host ""
}

Write-Host "Full P2 stack validation completed successfully."
