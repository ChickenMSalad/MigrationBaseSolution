param(
    [string]$AdminApiBaseUrl = 'https://localhost:55436',
    [int]$TimeoutSeconds = 5,
    [int]$MaxGetProbes = 100
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..')
$adminWebSourceRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web\src'
if (-not (Test-Path $adminWebSourceRoot)) {
    throw ('Admin Web source root was not found: {0}' -f $adminWebSourceRoot)
}

$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.3D-Repair2'
if (-not (Test-Path $artifactRoot)) {
    New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
}

$summaryPath = Join-Path $artifactRoot 'page-api-interaction-coverage.summary.md'
$detailsPath = Join-Path $artifactRoot 'page-api-interaction-coverage.details.csv'
$startedUtc = [DateTime]::UtcNow.ToString('o')

$endpointMap = @{}
$sourceFiles = Get-ChildItem -Path $adminWebSourceRoot -Recurse -File -Include '*.ts','*.tsx' |
    Where-Object {
        $_.FullName -notlike '*\node_modules\*' -and
        $_.FullName -notlike '*\dist\*' -and
        $_.FullName -notlike '*\reference\*'
    }

foreach ($file in $sourceFiles) {
    $relativePath = $file.FullName.Substring($adminWebSourceRoot.Length).TrimStart('\')
    $lines = Get-Content -Path $file.FullName
    foreach ($line in $lines) {
        if ($null -eq $line) { continue }
        $lineText = [string]$line
        $searchStart = 0
        while ($true) {
            $apiIndex = $lineText.IndexOf('/api/', $searchStart, [StringComparison]::OrdinalIgnoreCase)
            if ($apiIndex -lt 0) { break }

            $endIndex = $apiIndex
            while ($endIndex -lt $lineText.Length) {
                $char = $lineText[$endIndex]
                if ($char -eq '"' -or $char -eq "'" -or $char -eq '`' -or $char -eq ')' -or $char -eq ',' -or $char -eq ' ' -or $char -eq ';') {
                    break
                }
                $endIndex = $endIndex + 1
            }

            if ($endIndex -gt $apiIndex) {
                $path = $lineText.Substring($apiIndex, $endIndex - $apiIndex)
                if ($path.IndexOf('$', [StringComparison]::Ordinal) -ge 0) {
                    $dollarIndex = $path.IndexOf('$', [StringComparison]::Ordinal)
                    if ($dollarIndex -gt 0) {
                        $path = $path.Substring(0, $dollarIndex)
                    }
                }
                if ($path.EndsWith('/', [StringComparison]::Ordinal)) {
                    $path = $path.TrimEnd('/')
                }

                if ($path.Length -gt 5) {
                    $method = 'GET'
                    $lowerLine = $lineText.ToLowerInvariant()
                    if ($lowerLine.Contains('post') -or $lowerLine.Contains('apipost')) { $method = 'POST' }
                    elseif ($lowerLine.Contains('put') -or $lowerLine.Contains('apiput')) { $method = 'PUT' }
                    elseif ($lowerLine.Contains('delete') -or $lowerLine.Contains('apidelete')) { $method = 'DELETE' }
                    elseif ($lowerLine.Contains('patch') -or $lowerLine.Contains('apipatch')) { $method = 'PATCH' }

                    $key = ('{0} {1}' -f $method, $path)
                    if (-not $endpointMap.ContainsKey($key)) {
                        $endpointMap[$key] = [pscustomobject]@{
                            Method = $method
                            Path = $path
                            Source = $relativePath
                        }
                    }
                }
            }

            $searchStart = $apiIndex + 5
            if ($searchStart -ge $lineText.Length) { break }
        }
    }
}

$details = New-Object 'System.Collections.Generic.List[object]'
$getProbeCount = 0
$successCount = 0
$nonSuccessCount = 0
$verbMismatchCount = 0
$skippedCount = 0

$keys = @($endpointMap.Keys | Sort-Object)
foreach ($key in $keys) {
    $entry = $endpointMap[$key]
    $classification = 'SkippedNonGet'
    $statusCode = ''
    $result = 'Skipped'
    $errorMessage = ''

    if ($entry.Method -eq 'GET' -and $getProbeCount -lt $MaxGetProbes) {
        $getProbeCount = $getProbeCount + 1
        $targetUrl = $AdminApiBaseUrl.TrimEnd('/') + $entry.Path
        try {
            $response = Invoke-WebRequest -Uri $targetUrl -Method Get -TimeoutSec $TimeoutSeconds -UseBasicParsing
            $statusCode = [string][int]$response.StatusCode
            if ([int]$response.StatusCode -ge 200 -and [int]$response.StatusCode -lt 300) {
                $result = 'Success'
                $classification = 'Success'
                $successCount = $successCount + 1
            }
            else {
                $result = 'NonSuccess'
                $classification = 'HttpFailure'
                $nonSuccessCount = $nonSuccessCount + 1
            }
        }
        catch {
            $webException = $_.Exception
            $responseObject = $webException.Response
            if ($null -ne $responseObject) {
                try {
                    $statusCode = [string][int]$responseObject.StatusCode
                }
                catch {
                    $statusCode = ''
                }
            }
            $errorMessage = $webException.Message
            if ($statusCode -eq '405' -and ($entry.Path.ToLowerInvariant().Contains('/probe') -or $entry.Path.ToLowerInvariant().Contains('/build') -or $entry.Path.ToLowerInvariant().Contains('/validate') -or $entry.Path.ToLowerInvariant().Contains('/preview'))) {
                $result = 'VerbMismatch'
                $classification = 'VerbMismatchEvidence'
                $verbMismatchCount = $verbMismatchCount + 1
            }
            else {
                $result = 'NonSuccess'
                $classification = 'HttpFailure'
                $nonSuccessCount = $nonSuccessCount + 1
            }
        }
    }
    else {
        $skippedCount = $skippedCount + 1
    }

    [void]$details.Add([pscustomobject]@{
        Method = $entry.Method
        Path = $entry.Path
        Source = $entry.Source
        Result = $result
        Classification = $classification
        StatusCode = $statusCode
        Error = $errorMessage
    })
}

$details | Export-Csv -Path $detailsPath -NoTypeInformation -Encoding UTF8

$finishedUtc = [DateTime]::UtcNow.ToString('o')
$summary = New-Object 'System.Collections.Generic.List[string]'
[void]$summary.Add('# P10.3D Repair2 - Admin Web Page API Interaction Coverage')
[void]$summary.Add('')
[void]$summary.Add(('Started UTC: `{0}`' -f $startedUtc))
[void]$summary.Add(('Finished UTC: `{0}`' -f $finishedUtc))
[void]$summary.Add(('Admin API base URL: `{0}`' -f $AdminApiBaseUrl))
[void]$summary.Add(('Timeout seconds: `{0}`' -f $TimeoutSeconds))
[void]$summary.Add(('Discovered endpoint interactions: `{0}`' -f $endpointMap.Count))
[void]$summary.Add(('GET probes attempted: `{0}`' -f $getProbeCount))
[void]$summary.Add(('Successful GET probes: `{0}`' -f $successCount))
[void]$summary.Add(('Verb mismatch evidence: `{0}`' -f $verbMismatchCount))
[void]$summary.Add(('Non-success GET probes: `{0}`' -f $nonSuccessCount))
[void]$summary.Add(('Skipped non-GET/limit interactions: `{0}`' -f $skippedCount))
[void]$summary.Add('')
[void]$summary.Add(('Details: `{0}`' -f $detailsPath))
Set-Content -Path $summaryPath -Value $summary.ToArray() -Encoding UTF8

Write-Host ('Wrote summary: {0}' -f $summaryPath)
Write-Host ('Wrote details: {0}' -f $detailsPath)

if ($nonSuccessCount -gt 0) {
    throw ('Page API interaction coverage completed with {0} real non-success GET probe(s). Review {1}' -f $nonSuccessCount, $summaryPath)
}

Write-Host 'Page API interaction coverage completed without real non-success GET probes.'
