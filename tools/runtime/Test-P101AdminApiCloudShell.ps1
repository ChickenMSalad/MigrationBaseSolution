[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $BaseUrl,

    [Parameter(Mandatory = $false)]
    [int] $TimeoutSeconds = 30,

    [Parameter(Mandatory = $false)]
    [string] $OutputPath
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    if (-not [string]::IsNullOrWhiteSpace($PSCommandPath)) {
        $scriptRoot = Split-Path -Parent $PSCommandPath
    }
}
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    throw 'Unable to resolve script root.'
}

$repoRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $artifactRoot = [System.IO.Path]::Combine($repoRoot, 'artifacts', 'p10', 'site')
    if (-not (Test-Path -LiteralPath $artifactRoot)) {
        New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
    }
    $OutputPath = [System.IO.Path]::Combine($artifactRoot, 'admin-api-cloud-shell-readiness.txt')
}
elseif (-not [System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = [System.IO.Path]::Combine($repoRoot, $OutputPath)
}

$outputParent = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($outputParent) -and -not (Test-Path -LiteralPath $outputParent)) {
    New-Item -ItemType Directory -Path $outputParent -Force | Out-Null
}

$normalizedBaseUrl = $BaseUrl.TrimEnd('/')
$swaggerJsonUrl = $normalizedBaseUrl + '/swagger/v1/swagger.json'
$swaggerUiUrl = $normalizedBaseUrl + '/swagger'

$results = @()
foreach ($url in @($swaggerJsonUrl, $swaggerUiUrl)) {
    try {
        $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec $TimeoutSeconds
        $results += [pscustomobject]@{
            Url = $url
            StatusCode = [int]$response.StatusCode
            Success = $true
            Message = 'OK'
        }
    }
    catch {
        $results += [pscustomobject]@{
            Url = $url
            StatusCode = 0
            Success = $false
            Message = $_.Exception.Message
        }
    }
}

$lines = @()
$lines += '# P10.1C Admin API Cloud Shell Readiness'
$lines += ''
$lines += ('GeneratedUtc: {0:o}' -f [DateTimeOffset]::UtcNow)
$lines += ('BaseUrl: {0}' -f $normalizedBaseUrl)
$lines += ''
foreach ($result in $results) {
    $status = 'Failed'
    if ($result.Success) {
        $status = 'Passed'
    }
    $lines += ('- {0} [{1}] {2} {3}' -f $status, $result.StatusCode, $result.Url, $result.Message)
}

Set-Content -LiteralPath $OutputPath -Value $lines -Encoding UTF8

$failures = @($results | Where-Object { -not $_.Success })
if ($failures.Count -gt 0) {
    throw ('Admin API cloud shell readiness failed. See {0}' -f $OutputPath)
}

Write-Host ('Admin API cloud shell readiness passed. Report: {0}' -f $OutputPath)
