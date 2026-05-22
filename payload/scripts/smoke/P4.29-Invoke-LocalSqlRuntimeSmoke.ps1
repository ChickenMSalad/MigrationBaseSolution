[CmdletBinding()]
param(
    [string]$BaseUrl = "https://localhost:55436",
    [switch]$AllowUntrustedCertificate
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host ("[P4.29-SMOKE] {0}" -f $Message)
}

function Invoke-SmokeGet {
    param(
        [string]$Url
    )

    Write-Step ("GET {0}" -f $Url)

    if ($AllowUntrustedCertificate) {
        if (-not ("TrustAllCertsPolicy" -as [type])) {
            Add-Type @"
using System.Net;
using System.Security.Cryptography.X509Certificates;

public sealed class TrustAllCertsPolicy : ICertificatePolicy
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

        [System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
    }

    $response = Invoke-WebRequest -Uri $Url -Method Get -UseBasicParsing
    if ($response.StatusCode -lt 200 -or $response.StatusCode -ge 300) {
        throw ("Unexpected status code {0} from {1}" -f $response.StatusCode, $Url)
    }

    return $response
}

$trimmedBaseUrl = $BaseUrl.TrimEnd("/")

$endpoints = @(
    "/api/operational/command-center/summary",
    "/api/operational/sla-slo/summary",
    "/api/operational/notifications/summary",
    "/api/operational/capacity/summary",
    "/api/operational/cost/summary",
    "/api/operational/audit-trail/summary"
)

foreach ($endpoint in $endpoints) {
    $url = $trimmedBaseUrl + $endpoint
    $null = Invoke-SmokeGet -Url $url
}

Write-Step "Local SQL runtime smoke passed."
