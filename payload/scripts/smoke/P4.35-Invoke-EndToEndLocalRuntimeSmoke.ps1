[CmdletBinding()]
param(
    [string]$BaseUrl = "https://localhost:55436",
    [string]$ServerInstance = "(localdb)\MSSQLLocalDB",
    [string]$DatabaseName = "MigrationBaseSolution_Operational",
    [switch]$AllowUntrustedCertificate,
    [switch]$SkipDatabaseValidation,
    [switch]$SkipUiBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host ("[P4.35-SMOKE] {0}" -f $Message)
}

function Enable-UntrustedCertificateSupport {
    if (-not ("TrustAllCertsPolicyP435" -as [type])) {
        Add-Type @"
using System.Net;
using System.Security.Cryptography.X509Certificates;

public sealed class TrustAllCertsPolicyP435 : ICertificatePolicy
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

    [System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicyP435
}

function Invoke-SmokeGet {
    param([string]$Url)

    Write-Step ("GET {0}" -f $Url)

    $response = Invoke-WebRequest -Uri $Url -Method Get -UseBasicParsing

    if ($response.StatusCode -lt 200 -or $response.StatusCode -ge 300) {
        throw ("Unexpected status code {0} from {1}" -f $response.StatusCode, $Url)
    }

    return $response.Content
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path

if ($AllowUntrustedCertificate) {
    Enable-UntrustedCertificateSupport
}

if (-not $SkipDatabaseValidation) {
    $databaseTestPath = Join-Path $repoRoot "scripts/sql/P4.32-Test-OperationalSqlDatabase.ps1"
    if (-not (Test-Path -LiteralPath $databaseTestPath)) {
        throw ("Database validation script not found: {0}" -f $databaseTestPath)
    }

    Write-Step "Validating operational SQL database"
    & $databaseTestPath -ServerInstance $ServerInstance -DatabaseName $DatabaseName
}

$trimmedBaseUrl = $BaseUrl.TrimEnd("/")

$sqlHealthJson = Invoke-SmokeGet -Url ($trimmedBaseUrl + "/api/operational/sql/health")
$sqlHealth = $sqlHealthJson | ConvertFrom-Json

if (-not ($sqlHealth.PSObject.Properties.Name -contains "status")) {
    throw "SQL health response did not include status."
}

if ($sqlHealth.status -eq "unhealthy") {
    throw ("SQL health endpoint returned unhealthy: {0}" -f $sqlHealth.message)
}

Write-Step ("SQL health status: {0}" -f $sqlHealth.status)

$commandCenterJson = Invoke-SmokeGet -Url ($trimmedBaseUrl + "/api/operational/command-center/summary")
$commandCenter = $commandCenterJson | ConvertFrom-Json

if (-not ($commandCenter.PSObject.Properties.Name -contains "runtimeStatus")) {
    throw "Command-center summary response did not include runtimeStatus."
}

Write-Step ("Command-center status: {0}" -f $commandCenter.runtimeStatus)

$expectedEndpoints = @(
    "/api/operational/audit-trail/summary",
    "/api/operational/notifications/summary",
    "/api/operational/sla-slo/summary",
    "/api/operational/capacity/summary",
    "/api/operational/cost/summary"
)

foreach ($endpoint in $expectedEndpoints) {
    $null = Invoke-SmokeGet -Url ($trimmedBaseUrl + $endpoint)
}

if (-not $SkipUiBuild) {
    $uiPath = Join-Path $repoRoot "apps/migration-admin-ui"
    if (-not (Test-Path -LiteralPath $uiPath)) {
        throw ("UI path not found: {0}" -f $uiPath)
    }

    Push-Location $uiPath
    try {
        Write-Step "Running UI build"
        npm run build
    }
    finally {
        Pop-Location
    }
}

Write-Step "End-to-end local runtime smoke passed."
