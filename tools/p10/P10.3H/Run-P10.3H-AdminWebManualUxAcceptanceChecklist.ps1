param(
    [string]$AdminWebBaseUrl = 'http://localhost:5173',
    [string]$AdminApiBaseUrl = 'https://localhost:55436',
    [int]$TimeoutSeconds = 5
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.3H'
if (-not (Test-Path -LiteralPath $artifactRoot)) {
    New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
}

$summaryPath = Join-Path $artifactRoot 'manual-ux-acceptance.summary.md'
$detailsPath = Join-Path $artifactRoot 'manual-ux-acceptance.details.csv'

$started = [DateTime]::UtcNow.ToString('o')
$routes = New-Object 'System.Collections.Generic.List[string]'
[void]$routes.Add('/')
[void]$routes.Add('/runtime-dashboard')
[void]$routes.Add('/execution-sessions')
[void]$routes.Add('/failure-retry')
[void]$routes.Add('/operations/operational-events')
[void]$routes.Add('/connector-configuration')
[void]$routes.Add('/credential-vault')
[void]$routes.Add('/manifest-builder')
[void]$routes.Add('/taxonomy-builder')
[void]$routes.Add('/mapping-builder')

$apiPaths = New-Object 'System.Collections.Generic.List[string]'
[void]$apiPaths.Add('/api/connectors')
[void]$apiPaths.Add('/api/projects')
[void]$apiPaths.Add('/api/runs')
[void]$apiPaths.Add('/api/artifacts')
[void]$apiPaths.Add('/api/credentials')
[void]$apiPaths.Add('/api/cloud/auth/configuration')
[void]$apiPaths.Add('/api/cloud/storage/provider')

$rows = New-Object 'System.Collections.Generic.List[object]'
function Invoke-CurlProbe {
    param(
        [string]$Url,
        [int]$Timeout
    )

    $curl = Get-Command curl.exe -ErrorAction SilentlyContinue
    if ($null -eq $curl) {
        return @{ Status = 'Skipped'; Code = ''; Message = 'curl.exe not available' }
    }

    $output = & $curl.Source -k -s -o NUL -w '%{http_code}' --max-time $Timeout $Url 2>$null
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) {
        return @{ Status = 'RequestFailed'; Code = ''; Message = ('curl exit {0}' -f $exitCode) }
    }
    $codeText = [string]$output
    $status = 'NonSuccess'
    if ($codeText -ge '200' -and $codeText -lt '400') { $status = 'Success' }
    return @{ Status = $status; Code = $codeText; Message = '' }
}

foreach ($route in $routes) {
    $url = $AdminWebBaseUrl.TrimEnd('/') + $route
    $probe = Invoke-CurlProbe -Url $url -Timeout $TimeoutSeconds
    [void]$rows.Add([pscustomobject]@{ Kind = 'Route'; Target = $route; Url = $url; Status = $probe.Status; HttpStatus = $probe.Code; Message = $probe.Message })
}
foreach ($apiPath in $apiPaths) {
    $url = $AdminApiBaseUrl.TrimEnd('/') + $apiPath
    $probe = Invoke-CurlProbe -Url $url -Timeout $TimeoutSeconds
    [void]$rows.Add([pscustomobject]@{ Kind = 'Api'; Target = $apiPath; Url = $url; Status = $probe.Status; HttpStatus = $probe.Code; Message = $probe.Message })
}

$rows | Export-Csv -LiteralPath $detailsPath -NoTypeInformation -Encoding UTF8

$successCount = 0
$nonSuccessCount = 0
foreach ($row in $rows) {
    if ($row.Status -eq 'Success') { $successCount++ } else { $nonSuccessCount++ }
}

$summary = New-Object 'System.Collections.Generic.List[string]'
[void]$summary.Add('# P10.3H - Admin Web Manual UX Acceptance Evidence')
[void]$summary.Add('')
[void]$summary.Add(('Started UTC: `{0}`' -f $started))
[void]$summary.Add(('Finished UTC: `{0}`' -f ([DateTime]::UtcNow.ToString('o'))))
[void]$summary.Add(('Admin Web base URL: `{0}`' -f $AdminWebBaseUrl))
[void]$summary.Add(('Admin API base URL: `{0}`' -f $AdminApiBaseUrl))
[void]$summary.Add(('Timeout seconds: `{0}`' -f $TimeoutSeconds))
[void]$summary.Add('')
[void]$summary.Add(('Total probes: `{0}`' -f $rows.Count))
[void]$summary.Add(('Successful probes: `{0}`' -f $successCount))
[void]$summary.Add(('Non-success probes: `{0}`' -f $nonSuccessCount))
[void]$summary.Add('')
[void]$summary.Add('Manual UX note: this runner verifies reachable shell routes and core API availability. It does not replace human click-through validation of forms, empty states, or deferred historical builder parity.')
[void]$summary.Add('')
[void]$summary.Add(('Details: `{0}`' -f $detailsPath))
Set-Content -LiteralPath $summaryPath -Value $summary.ToArray() -Encoding UTF8

Write-Host ('Wrote summary: {0}' -f $summaryPath)
Write-Host ('Wrote details: {0}' -f $detailsPath)
if ($nonSuccessCount -gt 0) {
    throw ('Manual UX acceptance evidence completed with {0} non-success probe(s). Review {1}' -f $nonSuccessCount, $summaryPath)
}
Write-Host 'P10.3H Admin Web manual UX acceptance evidence passed.'
