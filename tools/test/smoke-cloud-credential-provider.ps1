param(
    [string]$BaseUrl = "http://localhost:5173",
    [string]$Role = "source",
    [string]$Connector = "aem",
    [string]$CredentialSet = "default",
    [string]$SecretKind = "password"
)

$ErrorActionPreference = "Stop"

Write-Host "Cloud Credential Provider Smoke Test"
Write-Host "Base URL      : $BaseUrl"
Write-Host "Role          : $Role"
Write-Host "Connector     : $Connector"
Write-Host "Credential Set: $CredentialSet"
Write-Host "Secret Kind   : $SecretKind"
Write-Host ""

$provider = Invoke-RestMethod "$BaseUrl/api/cloud/credentials/provider"
Write-Host "Provider kind : $($provider.providerKind)"
Write-Host "Configured    : $($provider.isConfigured)"
Write-Host "Managed ID    : $($provider.usesManagedIdentity)"

$nameUri = "$BaseUrl/api/cloud/credentials/secret-name?role=$Role&connector=$Connector&credentialSet=$CredentialSet&secretKind=$SecretKind"
$reference = Invoke-RestMethod $nameUri

if ([string]::IsNullOrWhiteSpace($reference.secretName)) {
    throw "Secret name was not resolved."
}

Write-Host "Secret name   : $($reference.secretName)"

$existsUri = "$BaseUrl/api/cloud/credentials/secret-exists?role=$Role&connector=$Connector&credentialSet=$CredentialSet&secretKind=$SecretKind"
$exists = Invoke-RestMethod $existsUri

Write-Host "Exists        : $($exists.exists)"
Write-Host "Value returned: $($exists.valueReturned)"

if ($exists.valueReturned -ne $false) {
    throw "Secret existence endpoint should never return secret values."
}

Write-Host ""
Write-Host "Cloud credential provider smoke test completed successfully."
