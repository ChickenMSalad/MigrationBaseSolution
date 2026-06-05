Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$repoRootPath = $repoRoot.ProviderPath

$docsDir = Join-Path $repoRootPath 'docs\P10'
if (-not (Test-Path -LiteralPath $docsDir)) {
    New-Item -ItemType Directory -Path $docsDir -Force | Out-Null
}

$reportPath = Join-Path $docsDir 'P10.2CU-AdminWebBuilderApiContractSmoke.md'
$runnerPath = Join-Path $scriptRoot 'Run-P10.2CU-AdminWebBuilderApiContractSmoke.ps1'

$runnerContent = @'
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

param(
    [string]$AdminApiBaseUrl = 'https://localhost:55436',
    [int]$TimeoutSeconds = 5,
    [int]$MaxEndpoints = 50
)

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$repoRootPath = $repoRoot.ProviderPath
$adminWebRoot = Join-Path $repoRootPath 'src\Admin\Migration.Admin.Web'
$sourceRoot = Join-Path $adminWebRoot 'src'
$artifactRoot = Join-Path $repoRootPath 'artifacts\p10\P10.2CU'

if (-not (Test-Path -LiteralPath $artifactRoot)) {
    New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
}

$summaryPath = Join-Path $artifactRoot 'builder-api-contract-smoke.summary.md'
$detailsPath = Join-Path $artifactRoot 'builder-api-contract-smoke.details.csv'

$summary = New-Object 'System.Collections.Generic.List[string]'
[void]$summary.Add('# P10.2CU - Admin Web Builder API Contract Smoke')
[void]$summary.Add('')
[void]$summary.Add(('Started UTC: `{0}`' -f ([DateTime]::UtcNow.ToString('o'))))
[void]$summary.Add(('Admin API base URL: `{0}`' -f $AdminApiBaseUrl))
[void]$summary.Add(('Timeout seconds: `{0}`' -f $TimeoutSeconds))
[void]$summary.Add(('Max endpoints: `{0}`' -f $MaxEndpoints))
[void]$summary.Add('')
Set-Content -LiteralPath $summaryPath -Value $summary.ToArray() -Encoding UTF8

$endpointSet = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
$builderTerms = @('manifest-builder', 'manifestBuilder', 'ManifestBuilder', 'taxonomy-builder', 'taxonomyBuilder', 'TaxonomyBuilder', 'mapping-builder', 'mappingBuilder', 'MappingBuilder', 'taxonomy', 'mapping', 'manifest')

if (Test-Path -LiteralPath $sourceRoot) {
    $files = @(Get-ChildItem -LiteralPath $sourceRoot -Recurse -File -Include '*.ts','*.tsx' | Where-Object { $_.FullName -notmatch '\\reference\\' })
    foreach ($file in $files) {
        $pathText = $file.FullName
        $includeFile = $false
        foreach ($term in $builderTerms) {
            if ($pathText.IndexOf($term, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                $includeFile = $true
                break
            }
        }

        $content = Get-Content -LiteralPath $file.FullName -Raw
        if (-not $includeFile) {
            foreach ($term in $builderTerms) {
                if ($content.IndexOf($term, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                    $includeFile = $true
                    break
                }
            }
        }

        if (-not $includeFile) { continue }

        $matches = [regex]::Matches($content, '/api/[A-Za-z0-9_\-/{}]+')
        foreach ($match in $matches) {
            $value = $match.Value.Trim()
            if ($value.Length -gt 0) {
                [void]$endpointSet.Add($value)
            }
        }
    }
}

$endpoints = New-Object 'System.Collections.Generic.List[string]'
foreach ($endpoint in $endpointSet) {
    [void]$endpoints.Add($endpoint)
}
$endpointArray = $endpoints.ToArray() | Sort-Object

$details = New-Object 'System.Collections.Generic.List[object]'
$probeCount = 0
foreach ($endpoint in $endpointArray) {
    if ($probeCount -ge $MaxEndpoints) { break }
    $probeCount++

    $method = 'GET'
    $status = 'NotRun'
    $statusCode = ''
    $message = ''

    $lowerEndpoint = $endpoint.ToLowerInvariant()
    if ($lowerEndpoint.Contains('/build') -or $lowerEndpoint.Contains('/probe') -or $lowerEndpoint.Contains('/validate') -or $lowerEndpoint.Contains('/create') -or $lowerEndpoint.Contains('/update') -or $lowerEndpoint.Contains('/delete')) {
        $method = 'SKIP'
        $status = 'SkippedActionEndpoint'
        $message = 'Endpoint appears action-oriented; skipped by non-mutating smoke runner.'
    }
    else {
        $uri = ($AdminApiBaseUrl.TrimEnd('/') + $endpoint)
        try {
            $response = Invoke-WebRequest -Uri $uri -Method Get -TimeoutSec $TimeoutSeconds -UseBasicParsing
            $status = 'Success'
            $statusCode = [string]$response.StatusCode
            $message = $response.StatusDescription
        }
        catch {
            $status = 'RequestFailed'
            $message = $_.Exception.Message
            if ($_.Exception.Response -ne $null) {
                try {
                    $statusCode = [string]([int]$_.Exception.Response.StatusCode)
                }
                catch {
                    $statusCode = ''
                }
            }
        }
    }

    [void]$details.Add([pscustomobject]@{
        Endpoint = $endpoint
        Method = $method
        Status = $status
        StatusCode = $statusCode
        Message = $message
    })
}

$details | Export-Csv -LiteralPath $detailsPath -NoTypeInformation -Encoding UTF8

$successCount = @($details | Where-Object { $_.Status -eq 'Success' }).Length
$skipCount = @($details | Where-Object { $_.Status -eq 'SkippedActionEndpoint' }).Length
$failureCount = @($details | Where-Object { $_.Status -eq 'RequestFailed' }).Length

[void]$summary.Add(('Discovered builder endpoint candidates: `{0}`' -f $endpointSet.Count))
[void]$summary.Add(('Probed or classified endpoints: `{0}`' -f $details.Count))
[void]$summary.Add(('Success: `{0}`' -f $successCount))
[void]$summary.Add(('Skipped action endpoints: `{0}`' -f $skipCount))
[void]$summary.Add(('Request failures: `{0}`' -f $failureCount))
[void]$summary.Add(('Finished UTC: `{0}`' -f ([DateTime]::UtcNow.ToString('o'))))
[void]$summary.Add('')
[void]$summary.Add(('Details: `{0}`' -f $detailsPath))
Set-Content -LiteralPath $summaryPath -Value $summary.ToArray() -Encoding UTF8

Write-Host ('Wrote summary: {0}' -f $summaryPath)
Write-Host ('Wrote details: {0}' -f $detailsPath)
'@

Set-Content -LiteralPath $runnerPath -Value $runnerContent -Encoding UTF8

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2CU - Admin Web Builder API Contract Smoke')
[void]$report.Add('')
[void]$report.Add('Adds a bounded, non-mutating smoke runner for Manifest, Taxonomy, and Mapping builder API endpoint candidates discovered from the canonical Admin Web source tree.')
[void]$report.Add('')
[void]$report.Add('The runner:')
[void]$report.Add('- accepts an Admin API base URL parameter;')
[void]$report.Add('- writes summary and CSV details immediately under artifacts/p10/P10.2CU;')
[void]$report.Add('- applies per-request timeouts;')
[void]$report.Add('- skips action-like endpoints instead of POSTing blindly;')
[void]$report.Add('- does not mutate Admin Web source files.')
[void]$report.Add('')
[void]$report.Add('Example:')
[void]$report.Add('')
[void]$report.Add('```powershell')
[void]$report.Add('powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.2CU\Run-P10.2CU-AdminWebBuilderApiContractSmoke.ps1 -AdminApiBaseUrl "https://localhost:55436"')
[void]$report.Add('```')
Set-Content -LiteralPath $reportPath -Value $report.ToArray() -Encoding UTF8

Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host ('Wrote runner: {0}' -f $runnerPath)
Write-Host 'P10.2CU Admin Web builder API contract smoke applied.'
