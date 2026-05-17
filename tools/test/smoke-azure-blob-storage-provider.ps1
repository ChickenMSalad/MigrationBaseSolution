param(
    [string]$BaseUrl = "http://localhost:5173",
    [switch]$ExpectAzureBlob
)

$ErrorActionPreference = "Stop"

Write-Host "Azure Blob Storage Provider Diagnostics Smoke Test"
Write-Host "Base URL       : $BaseUrl"
Write-Host "ExpectAzureBlob: $ExpectAzureBlob"
Write-Host ""

$diagnostics = Invoke-RestMethod "$BaseUrl/api/cloud/storage/azure-blob/diagnostics"

Write-Host "Storage root     : $($diagnostics.storageRoot)"
Write-Host "Selected provider: $($diagnostics.selectedProvider)"
Write-Host "Active provider  : $($diagnostics.activeProvider)"
Write-Host "Container        : $($diagnostics.containerName)"
Write-Host ""

if ($ExpectAzureBlob -and $diagnostics.azureBlobSelected -ne $true) {
    throw "Expected Azure Blob provider selection, but Azure Blob was not selected."
}

if ($ExpectAzureBlob -and $diagnostics.azureBlobConfigured -ne $true) {
    throw "Expected Azure Blob to be configured, but diagnostics reported it is not configured."
}

if (!$ExpectAzureBlob -and $diagnostics.selectedProvider -ne "localFileSystem") {
    Write-Host "WARN expected local file-system by default, but selected provider was $($diagnostics.selectedProvider)" -ForegroundColor Yellow
}

if ($diagnostics.warnings.Count -gt 0) {
    Write-Host "Warnings:"
    $diagnostics.warnings | ForEach-Object { Write-Host "  - $_" }
}

Write-Host ""
Write-Host "Azure Blob storage provider diagnostics smoke test completed successfully."
