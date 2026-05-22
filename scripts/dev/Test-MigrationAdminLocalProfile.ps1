[CmdletBinding()]
param(
    [string]$BaseUrl = "https://localhost:55436",
    [switch]$AllowUntrustedCertificate
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host ("[P4.30-TEST] {0}" -f $Message)
}

if ($AllowUntrustedCertificate) {
    if (-not ("TrustAllCertsPolicyP430" -as [type])) {
        Add-Type @"
using System.Net;
using System.Security.Cryptography.X509Certificates;

public sealed class TrustAllCertsPolicyP430 : ICertificatePolicy
{
    public bool CheckValidationResult(
        ServicePoint srvPoint,
        X509Certificate certificate,
        WebRequest request,
        int certificateProblem)
    {
        return true;
    }
}
"@
    }

    [System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicyP430
}

$trimmedBaseUrl = $BaseUrl.TrimEnd("/")
$endpoint = $trimmedBaseUrl + "/api/operational/command-center/summary"

Write-Step ("GET {0}" -f $endpoint)

$response = Invoke-WebRequest -Uri $endpoint -Method Get -UseBasicParsing

if ($response.StatusCode -lt 200 -or $response.StatusCode -ge 300) {
    throw ("Unexpected status code {0}" -f $response.StatusCode)
}

Write-Step "Local Admin API profile is reachable."
