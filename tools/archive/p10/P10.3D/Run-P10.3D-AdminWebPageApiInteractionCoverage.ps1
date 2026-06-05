param(
    [string]$AdminApiBaseUrl = 'https://localhost:55436',
    [int]$TimeoutSeconds = 5,
    [int]$MaxGetEndpoints = 50
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$sourceRoot = Join-Path $adminWebRoot 'src'
$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.3D'
$summaryPath = Join-Path $artifactRoot 'page-api-interaction-coverage.summary.md'
$detailsPath = Join-Path $artifactRoot 'page-api-interaction-coverage.details.csv'

if (-not (Test-Path -LiteralPath $sourceRoot)) {
    throw ('Admin Web source root was not found: {0}' -f $sourceRoot)
}

New-Item -ItemType Directory -Force -Path $artifactRoot | Out-Null

$startedUtc = [DateTime]::UtcNow.ToString('o')
$baseUrl = $AdminApiBaseUrl.TrimEnd('/')
$endpointPattern = @'
["'](/api/[^"']+)["']
'@

$records = New-Object 'System.Collections.Generic.List[object]'
$files = Get-ChildItem -LiteralPath $sourceRoot -Recurse -File -Include '*.ts','*.tsx' |
    Where-Object { $_.FullName -notmatch '\\node_modules\\|\\dist\\|\\reference\\|\\apps\\' }

foreach ($file in $files) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    $matches = [regex]::Matches($content, $endpointPattern)
    foreach ($match in $matches) {
        if ($null -eq $match) { continue }
        if ($match.Groups.Count -lt 2) { continue }
        $endpoint = $match.Groups[1].Value
        if ([string]::IsNullOrWhiteSpace($endpoint)) { continue }
        if ($endpoint -like '*${*' -or $endpoint -like '*{*' -or $endpoint -like '*}*') { continue }
        $lineText = ''
        $lineNumber = 0
        $lines = Get-Content -LiteralPath $file.FullName
        for ($i = 0; $i -lt $lines.Length; $i++) {
            if ($lines[$i] -like ('*' + $endpoint + '*')) {
                $lineText = $lines[$i]
                $lineNumber = $i + 1
                break
            }
        }
        $method = 'GET'
        if ($lineText -match 'apiPost|\.post\(|fetchPost|method:\s*[''\"]POST') { $method = 'POST' }
        elseif ($lineText -match 'apiPut|\.put\(|method:\s*[''\"]PUT') { $method = 'PUT' }
        elseif ($lineText -match 'apiPatch|\.patch\(|method:\s*[''\"]PATCH') { $method = 'PATCH' }
        elseif ($lineText -match 'apiDelete|\.delete\(|method:\s*[''\"]DELETE') { $method = 'DELETE' }

        $relativeFile = $file.FullName.Substring($sourceRoot.Length).TrimStart('\')
        [void]$records.Add([pscustomobject]@{
            SourceFile = $relativeFile
            Line = $lineNumber
            Method = $method
            Endpoint = $endpoint
            Url = ($baseUrl + $endpoint)
            Result = 'Discovered'
            StatusCode = ''
            Error = ''
        })
    }
}

$unique = New-Object 'System.Collections.Generic.List[object]'
$seen = New-Object 'System.Collections.Generic.HashSet[string]'
foreach ($record in $records) {
    $key = ('{0} {1}' -f $record.Method, $record.Endpoint)
    if ($seen.Add($key)) {
        [void]$unique.Add($record)
    }
}

$getProbeCount = 0
foreach ($record in $unique) {
    if ($record.Method -ne 'GET') {
        $record.Result = 'SkippedUnsafeVerb'
        continue
    }
    if ($getProbeCount -ge $MaxGetEndpoints) {
        $record.Result = 'SkippedMaxGetLimit'
        continue
    }
    $getProbeCount = $getProbeCount + 1
    try {
        $response = Invoke-WebRequest -Uri $record.Url -Method Get -TimeoutSec $TimeoutSeconds -UseBasicParsing
        $record.StatusCode = [string]$response.StatusCode
        if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 400) {
            $record.Result = 'Success'
        }
        else {
            $record.Result = 'NonSuccess'
        }
    }
    catch {
        $statusCode = ''
        if ($_.Exception.Response -ne $null) {
            try { $statusCode = [string]([int]$_.Exception.Response.StatusCode) } catch { $statusCode = '' }
        }
        $record.StatusCode = $statusCode
        $record.Result = 'RequestFailed'
        $record.Error = $_.Exception.Message
    }
}

$unique | Export-Csv -LiteralPath $detailsPath -NoTypeInformation -Encoding UTF8

$finishedUtc = [DateTime]::UtcNow.ToString('o')
$successCount = 0
$nonSuccessCount = 0
$skippedCount = 0
foreach ($record in $unique) {
    if ($record.Result -eq 'Success') { $successCount = $successCount + 1 }
    elseif ($record.Result -like 'Skipped*') { $skippedCount = $skippedCount + 1 }
    else { $nonSuccessCount = $nonSuccessCount + 1 }
}

$summary = New-Object 'System.Collections.Generic.List[string]'
[void]$summary.Add('# P10.3D - Admin Web Page API Interaction Coverage')
[void]$summary.Add('')
[void]$summary.Add(('Started UTC: `{0}`' -f $startedUtc))
[void]$summary.Add(('Finished UTC: `{0}`' -f $finishedUtc))
[void]$summary.Add(('Admin API base URL: `{0}`' -f $baseUrl))
[void]$summary.Add(('Timeout seconds: `{0}`' -f $TimeoutSeconds))
[void]$summary.Add(('Discovered endpoint interactions: `{0}`' -f $records.Count))
[void]$summary.Add(('Unique method/path interactions: `{0}`' -f $unique.Count))
[void]$summary.Add(('Successful GET probes: `{0}`' -f $successCount))
[void]$summary.Add(('Non-success GET probes: `{0}`' -f $nonSuccessCount))
[void]$summary.Add(('Skipped non-GET/limit interactions: `{0}`' -f $skippedCount))
[void]$summary.Add('')
[void]$summary.Add(('Details: `{0}`' -f $detailsPath))

Set-Content -LiteralPath $summaryPath -Value $summary.ToArray() -Encoding UTF8
Write-Host ('Wrote summary: {0}' -f $summaryPath)
Write-Host ('Wrote details: {0}' -f $detailsPath)

if ($nonSuccessCount -gt 0) {
    throw ('Page API interaction coverage completed with {0} non-success GET probe(s). Review {1}' -f $nonSuccessCount, $summaryPath)
}

Write-Host 'P10.3D Admin Web page API interaction coverage completed successfully.'
