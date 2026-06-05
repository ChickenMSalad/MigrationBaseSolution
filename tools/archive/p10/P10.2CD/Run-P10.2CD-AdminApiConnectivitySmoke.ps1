param(
    [string]$AdminApiBaseUrl = ''
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$repoRootPath = $repoRoot.Path
$adminWebRoot = Join-Path $repoRootPath 'src\Admin\Migration.Admin.Web'
$artifactRoot = Join-Path $repoRootPath 'artifacts\p10\P10.2CD'
New-Item -ItemType Directory -Force -Path $artifactRoot | Out-Null

function Read-EnvValue {
    param(
        [string]$FilePath,
        [string]$Name
    )

    if (-not (Test-Path -Path $FilePath -PathType Leaf)) {
        return ''
    }

    $envLines = @(Get-Content -Path $FilePath)
    foreach ($envLine in $envLines) {
        if ($null -eq $envLine) {
            continue
        }

        $trimmed = $envLine.Trim()
        if ($trimmed.Length -eq 0) {
            continue
        }
        if ($trimmed.StartsWith('#')) {
            continue
        }

        $prefix = $Name + '='
        if ($trimmed.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $trimmed.Substring($prefix.Length).Trim()
        }
    }

    return ''
}

$resolvedBaseUrl = $AdminApiBaseUrl
if ([string]::IsNullOrWhiteSpace($resolvedBaseUrl)) {
    $envLocal = Join-Path $adminWebRoot '.env.local'
    $envExample = Join-Path $adminWebRoot '.env.example'
    $resolvedBaseUrl = Read-EnvValue -FilePath $envLocal -Name 'VITE_ADMIN_API_PROXY_TARGET'
    if ([string]::IsNullOrWhiteSpace($resolvedBaseUrl)) {
        $resolvedBaseUrl = Read-EnvValue -FilePath $envExample -Name 'VITE_ADMIN_API_PROXY_TARGET'
    }
}

if ([string]::IsNullOrWhiteSpace($resolvedBaseUrl)) {
    $resolvedBaseUrl = 'https://localhost:55436'
}

$resolvedBaseUrl = $resolvedBaseUrl.TrimEnd('/')
$summaryPath = Join-Path $artifactRoot 'admin-api-connectivity-summary.md'
$jsonPath = Join-Path $artifactRoot 'admin-api-connectivity-results.json'

$probePaths = @(
    '/health',
    '/swagger/v1/swagger.json',
    '/api/admin/system/status',
    '/api/diagnostics'
)

$results = New-Object 'System.Collections.Generic.List[object]'
$summary = New-Object 'System.Collections.Generic.List[string]'
[void]$summary.Add('# P10.2CD - Admin API Connectivity Smoke')
[void]$summary.Add('')
[void]$summary.Add(('Base URL: `{0}`' -f $resolvedBaseUrl))
[void]$summary.Add('')

$reachable = $false
foreach ($probePath in $probePaths) {
    $uri = $resolvedBaseUrl + $probePath
    $statusCode = $null
    $statusDescription = ''
    $errorMessage = ''
    try {
        $response = Invoke-WebRequest -Uri $uri -UseBasicParsing -TimeoutSec 10 -Method GET
        $statusCode = [int]$response.StatusCode
        $statusDescription = [string]$response.StatusDescription
        if ($statusCode -ge 200 -and $statusCode -lt 500) {
            $reachable = $true
        }
    }
    catch {
        $errorMessage = $_.Exception.Message
        if ($_.Exception.Response -ne $null) {
            try {
                $statusCode = [int]$_.Exception.Response.StatusCode
                $statusDescription = [string]$_.Exception.Response.StatusDescription
                if ($statusCode -ge 200 -and $statusCode -lt 500) {
                    $reachable = $true
                }
            }
            catch {
                if ([string]::IsNullOrWhiteSpace($errorMessage)) {
                    $errorMessage = 'Request failed and response metadata could not be read.'
                }
            }
        }
    }

    $result = [pscustomobject]@{
        path = $probePath
        uri = $uri
        statusCode = $statusCode
        statusDescription = $statusDescription
        error = $errorMessage
    }
    [void]$results.Add($result)

    if ($statusCode -ne $null) {
        [void]$summary.Add(('- `{0}` => {1} {2}' -f $probePath, $statusCode, $statusDescription))
    }
    else {
        [void]$summary.Add(('- `{0}` => request failed: {1}' -f $probePath, $errorMessage))
    }
}

$results | ConvertTo-Json -Depth 5 | Set-Content -Path $jsonPath -Encoding UTF8
Set-Content -Path $summaryPath -Value $summary -Encoding UTF8
Write-Host ('Wrote summary: {0}' -f $summaryPath)
Write-Host ('Wrote JSON results: {0}' -f $jsonPath)

if (-not $reachable) {
    throw ('Admin API was not reachable at {0}. Start the Admin API or pass -AdminApiBaseUrl.' -f $resolvedBaseUrl)
}

Write-Host 'P10.2CD Admin API connectivity smoke completed.'
