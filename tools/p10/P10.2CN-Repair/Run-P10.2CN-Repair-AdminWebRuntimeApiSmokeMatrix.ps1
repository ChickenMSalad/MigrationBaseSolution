param(
    [string]$AdminApiBaseUrl = '',
    [int]$TimeoutSec = 5,
    [int]$MaxEndpoints = 25
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$repoRootPath = $repoRoot.Path
$adminWebRoot = Join-Path $repoRootPath 'src\Admin\Migration.Admin.Web'
$sourceRoot = Join-Path $adminWebRoot 'src'
$artifactRoot = Join-Path $repoRootPath 'artifacts\p10\P10.2CN-Repair'
New-Item -ItemType Directory -Force -Path $artifactRoot | Out-Null

$summaryPath = Join-Path $artifactRoot 'runtime-api-smoke.summary.md'
$detailPath = Join-Path $artifactRoot 'runtime-api-smoke.details.csv'

if ([string]::IsNullOrWhiteSpace($AdminApiBaseUrl)) {
    $AdminApiBaseUrl = [Environment]::GetEnvironmentVariable('ADMIN_API_BASE_URL')
}
if ([string]::IsNullOrWhiteSpace($AdminApiBaseUrl)) {
    $AdminApiBaseUrl = [Environment]::GetEnvironmentVariable('VITE_ADMIN_API_BASE_URL')
}
if ([string]::IsNullOrWhiteSpace($AdminApiBaseUrl)) {
    $AdminApiBaseUrl = 'http://localhost:5000'
}
$AdminApiBaseUrl = $AdminApiBaseUrl.TrimEnd('/')
if ($TimeoutSec -lt 1) { $TimeoutSec = 5 }
if ($MaxEndpoints -lt 1) { $MaxEndpoints = 25 }

$summary = New-Object System.Collections.Generic.List[string]
[void]$summary.Add('# P10.2CN Repair - Admin Web Runtime API Smoke Matrix')
[void]$summary.Add('')
[void]$summary.Add(('Started UTC: `{0}`' -f ([DateTime]::UtcNow.ToString('o'))))
[void]$summary.Add(('Admin API base URL: `{0}`' -f $AdminApiBaseUrl))
[void]$summary.Add(('Timeout seconds: `{0}`' -f $TimeoutSec))
[void]$summary.Add(('Max endpoints: `{0}`' -f $MaxEndpoints))
[void]$summary.Add('')
Set-Content -Path $summaryPath -Value $summary -Encoding UTF8
Write-Host ('Wrote started summary: {0}' -f $summaryPath)

$details = New-Object System.Collections.Generic.List[string]
[void]$details.Add('Endpoint,Url,StatusCode,Outcome,Message')
Set-Content -Path $detailPath -Value $details -Encoding UTF8

if (-not (Test-Path -LiteralPath $sourceRoot)) {
    [void]$summary.Add(('Result: source root not found: `{0}`' -f $sourceRoot))
    Set-Content -Path $summaryPath -Value $summary -Encoding UTF8
    throw ('Admin Web source root was not found: {0}' -f $sourceRoot)
}

$endpoints = New-Object System.Collections.Generic.List[string]
$files = @(Get-ChildItem -Path $sourceRoot -Recurse -File -Include *.ts,*.tsx | Where-Object { $_.FullName -notmatch '\\reference\\' -and $_.FullName -notmatch '\\node_modules\\' -and $_.FullName -notmatch '\\dist\\' })
foreach ($file in $files) {
    $lines = @(Get-Content -LiteralPath $file.FullName)
    foreach ($line in $lines) {
        if ($null -eq $line) { continue }
        $text = [string]$line
        $index = $text.IndexOf('/api/')
        while ($index -ge 0) {
            $end = $index
            while ($end -lt $text.Length) {
                $ch = $text[$end]
                if ($ch -eq '''' -or $ch -eq '"' -or $ch -eq '`' -or $ch -eq ')' -or $ch -eq ',' -or $ch -eq ' ' -or $ch -eq '}') { break }
                $end++
            }
            if ($end -gt $index) {
                $endpoint = $text.Substring($index, $end - $index)
                if (-not [string]::IsNullOrWhiteSpace($endpoint) -and -not $endpoint.Contains('$') -and -not $endpoint.Contains('{')) {
                    if (-not $endpoints.Contains($endpoint)) { [void]$endpoints.Add($endpoint) }
                }
            }
            if ($end -ge $text.Length) { break }
            $nextStart = $end + 1
            if ($nextStart -ge $text.Length) { break }
            $index = $text.IndexOf('/api/', $nextStart)
        }
    }
}

[void]$summary.Add(('Discovered endpoint candidates: `{0}`' -f $endpoints.Count))
Set-Content -Path $summaryPath -Value $summary -Encoding UTF8

if ($endpoints.Count -eq 0) {
    [void]$summary.Add('Result: no endpoint candidates discovered. No requests were sent.')
    Set-Content -Path $summaryPath -Value $summary -Encoding UTF8
    Write-Host ('Wrote summary: {0}' -f $summaryPath)
    Write-Host ('Wrote details: {0}' -f $detailPath)
    exit 0
}

$probed = 0
foreach ($endpoint in $endpoints) {
    if ($probed -ge $MaxEndpoints) { break }
    $url = $AdminApiBaseUrl + $endpoint
    $statusCode = ''
    $outcome = 'Unknown'
    $message = ''
    try {
        $response = Invoke-WebRequest -Uri $url -Method Get -TimeoutSec $TimeoutSec -UseBasicParsing
        $statusCode = [string]$response.StatusCode
        $outcome = 'Success'
        $message = 'Request completed.'
    }
    catch {
        $message = $_.Exception.Message
        $responseObject = $null
        if ($_.Exception.PSObject.Properties.Name -contains 'Response') {
            $responseObject = $_.Exception.Response
        }
        if ($null -ne $responseObject) {
            try {
                $statusCode = [string]([int]$responseObject.StatusCode)
                $outcome = 'HttpStatus'
            }
            catch {
                $outcome = 'RequestFailed'
            }
        }
        else {
            $outcome = 'RequestFailed'
        }
    }
    $safeMessage = $message.Replace('"', '""').Replace("`r", ' ').Replace("`n", ' ')
    [void]$details.Add(('"{0}","{1}","{2}","{3}","{4}"' -f $endpoint, $url, $statusCode, $outcome, $safeMessage))
    Set-Content -Path $detailPath -Value $details -Encoding UTF8
    $probed++
    Write-Host ('Probed {0}: {1} {2}' -f $endpoint, $outcome, $statusCode)
}

[void]$summary.Add(('Probed endpoints: `{0}`' -f $probed))
[void]$summary.Add(('Finished UTC: `{0}`' -f ([DateTime]::UtcNow.ToString('o'))))
[void]$summary.Add('')
[void]$summary.Add(('Details: `{0}`' -f $detailPath))
Set-Content -Path $summaryPath -Value $summary -Encoding UTF8
Write-Host ('Wrote summary: {0}' -f $summaryPath)
Write-Host ('Wrote details: {0}' -f $detailPath)
