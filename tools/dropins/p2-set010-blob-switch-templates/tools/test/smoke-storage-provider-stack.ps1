param(
    [string]$BaseUrl = "http://localhost:5173",
    [switch]$ExpectAzureBlob
)

$ErrorActionPreference = "Stop"

Write-Host "Storage Provider Stack Smoke Test"
Write-Host "Base URL       : $BaseUrl"
Write-Host "ExpectAzureBlob: $ExpectAzureBlob"
Write-Host ""

$provider = Invoke-RestMethod "$BaseUrl/api/cloud/storage/provider"
$diagnostics = Invoke-RestMethod "$BaseUrl/api/cloud/storage/azure-blob/diagnostics"
$locations = Invoke-RestMethod "$BaseUrl/api/cloud/storage/locations"

Write-Host "Provider       : $($provider.provider)"
Write-Host "Selected       : $($diagnostics.selectedProvider)"
Write-Host "Storage root   : $($diagnostics.storageRoot)"
Write-Host "Workspace root : $($locations.workspaceRoot.uri)"
Write-Host ""

if ($ExpectAzureBlob) {
    if ($diagnostics.azureBlobSelected -ne $true) {
        throw "Expected Azure Blob to be selected."
    }

    if ($provider.provider -ne "azureBlob") {
        throw "Expected active provider to be azureBlob, but got '$($provider.provider)'."
    }
}
else {
    if ($provider.provider -ne "localFileSystem") {
        throw "Expected active provider to be localFileSystem, but got '$($provider.provider)'."
    }
}

Write-Host "Running binary storage probe..."
$binaryProbe = Invoke-RestMethod -Method Post "$BaseUrl/api/cloud/storage/probe"

if ($binaryProbe.exists -ne $true) {
    throw "Binary storage probe did not report exists=true."
}

Write-Host "Running artifact storage probe..."
$artifactProbe = Invoke-RestMethod -Method Post "$BaseUrl/api/cloud/artifacts/probe"

if ($artifactProbe.exists -ne $true) {
    throw "Artifact storage probe did not report exists=true."
}

Write-Host "Running artifact manifest index probe..."
$indexProbe = Invoke-RestMethod -Method Post "$BaseUrl/api/cloud/artifacts/index/probe"

if ($null -eq $indexProbe.index -or $indexProbe.index.artifacts.Count -lt 1) {
    throw "Artifact manifest index probe did not return an index with artifacts."
}

Write-Host ""
Write-Host "Storage provider stack smoke test completed successfully."
