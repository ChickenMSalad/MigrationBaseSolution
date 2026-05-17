param(
    [string]$BaseUrl = "http://localhost:5173",
    [switch]$ExpectArtifactStorage
)

$ErrorActionPreference = "Stop"

Write-Host "Audit Artifact Persistence Smoke Test"
Write-Host "Base URL              : $BaseUrl"
Write-Host "ExpectArtifactStorage : $ExpectArtifactStorage"
Write-Host ""

$config = Invoke-RestMethod "$BaseUrl/api/cloud/audit/artifact-persistence/configuration"
$provider = Invoke-RestMethod "$BaseUrl/api/cloud/audit/persistence/provider"

Write-Host "Configured provider : $($config.provider)"
Write-Host "Active provider     : $($provider.providerKind)"
Write-Host "Durable             : $($provider.isDurable)"
Write-Host "Artifact linking    : $($provider.supportsArtifactLinking)"

if ($ExpectArtifactStorage -and $provider.providerKind -ne "artifactStorage") {
    throw "Expected artifactStorage audit provider, got $($provider.providerKind)."
}

$probe = Invoke-RestMethod -Method Post "$BaseUrl/api/cloud/audit/persistence/probe"

if ($probe.result.accepted -ne $true) {
    throw "Audit persistence probe was not accepted."
}

if ($ExpectArtifactStorage -and [string]::IsNullOrWhiteSpace($probe.result.artifactObjectKey)) {
    throw "Expected artifact object key from artifact audit provider."
}

Write-Host "Audit id            : $($probe.result.auditId)"
Write-Host "Artifact object key : $($probe.result.artifactObjectKey)"
Write-Host ""
Write-Host "Audit artifact persistence smoke test completed successfully."
