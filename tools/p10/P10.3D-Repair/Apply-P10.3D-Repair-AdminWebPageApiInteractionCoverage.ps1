Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
$toolRoot = Join-Path $repoRoot 'tools\p10\P10.3D-Repair'
$docsRoot = Join-Path $repoRoot 'docs\P10'
if (-not (Test-Path -LiteralPath $docsRoot)) {
    New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
}

$runnerPath = Join-Path $toolRoot 'Run-P10.3D-Repair-AdminWebPageApiInteractionCoverage.ps1'
$reportPath = Join-Path $docsRoot 'P10.3D-Repair-AdminWebPageApiInteractionCoverage.md'

$runner = @'
param(
    [string]$AdminApiBaseUrl = 'https://localhost:55436',
    [int]$TimeoutSeconds = 5,
    [int]$MaxGetProbes = 75
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$sourceRoot = Join-Path $adminWebRoot 'src'
$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.3D-Repair'
if (-not (Test-Path -LiteralPath $artifactRoot)) {
    New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
}

$summaryPath = Join-Path $artifactRoot 'page-api-interaction-coverage.summary.md'
$detailsPath = Join-Path $artifactRoot 'page-api-interaction-coverage.details.csv'

$details = New-Object 'System.Collections.Generic.List[object]'
$summary = New-Object 'System.Collections.Generic.List[string]'

function Add-DetailRow {
    param(
        [string]$SourceFile,
        [int]$Line,
        [string]$Method,
        [string]$Endpoint,
        [string]$Url,
        [string]$Result,
        [string]$StatusCode,
        [string]$Error
    )

    $script:details.Add([pscustomobject]@{
        SourceFile = $SourceFile
        Line = $Line
        Method = $Method
        Endpoint = $Endpoint
        Url = $Url
        Result = $Result
        StatusCode = $StatusCode
        Error = $Error
    }) | Out-Null
}

function Get-RelativePath {
    param(
        [string]$Root,
        [string]$Path
    )

    $rootFull = [System.IO.Path]::GetFullPath($Root).TrimEnd('\') + '\'
    $pathFull = [System.IO.Path]::GetFullPath($Path)
    if ($pathFull.StartsWith($rootFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $pathFull.Substring($rootFull.Length)
    }

    return $Path
}

function Get-EndpointFromLine {
    param([string]$Line)

    if ([string]::IsNullOrWhiteSpace($Line)) { return $null }

    $single = [regex]::Match($Line, "['"](/api/[^'"]+)['"]")
    if ($single.Success) { return $single.Groups[1].Value }

    return $null
}

function Get-MethodFromLine {
    param([string]$Line)

    if ([string]::IsNullOrWhiteSpace($Line)) { return 'GET' }
    if ($Line -match '\bapiPost\b|\.post\b|fetch\s*\([^\)]*method\s*:\s*[''\"]POST') { return 'POST' }
    if ($Line -match '\bapiPut\b|\.put\b|fetch\s*\([^\)]*method\s*:\s*[''\"]PUT') { return 'PUT' }
    if ($Line -match '\bapiDelete\b|\.delete\b|fetch\s*\([^\)]*method\s*:\s*[''\"]DELETE') { return 'DELETE' }
    if ($Line -match '\bapiPatch\b|\.patch\b|fetch\s*\([^\)]*method\s*:\s*[''\"]PATCH') { return 'PATCH' }
    return 'GET'
}

function Test-ActionLikeEndpoint {
    param(
        [string]$Endpoint,
        [string]$SourceFile
    )

    if ([string]::IsNullOrWhiteSpace($Endpoint)) { return $false }
    $lower = $Endpoint.ToLowerInvariant()
    if ($lower.EndsWith('/probe')) { return $true }
    if ($lower.Contains('/probe/')) { return $true }
    if ($lower.EndsWith('/build')) { return $true }
    if ($lower.EndsWith('/validate')) { return $true }
    if ($lower.EndsWith('/preview')) { return $true }
    if ($lower.EndsWith('/save')) { return $true }
    if ($lower.EndsWith('/serialize')) { return $true }
    if ($lower.Contains('/writer/')) { return $true }
    if ($lower.Contains('/dispatch')) { return $true }
    if ($lower.Contains('/failure-handler')) { return $true }
    if ($SourceFile -match 'probe|builder') { return $true }
    return $false
}

if (-not (Test-Path -LiteralPath $sourceRoot)) {
    throw ('Admin Web source root was not found: {0}' -f $sourceRoot)
}

$startedUtc = (Get-Date).ToUniversalTime().ToString('o')
$files = Get-ChildItem -LiteralPath $sourceRoot -Recurse -File -Include *.ts,*.tsx |
    Where-Object { $_.FullName -notmatch '\\node_modules\\|\\dist\\|\\reference\\|\\apps\\' } |
    Sort-Object FullName

$interactions = New-Object 'System.Collections.Generic.List[object]'
foreach ($file in $files) {
    $relative = Get-RelativePath -Root $sourceRoot -Path $file.FullName
    $lines = Get-Content -LiteralPath $file.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = [string]$lines[$i]
        $endpoint = Get-EndpointFromLine -Line $line
        if ([string]::IsNullOrWhiteSpace($endpoint)) { continue }
        $method = Get-MethodFromLine -Line $line
        $interactions.Add([pscustomobject]@{
            SourceFile = $relative
            Line = ($i + 1)
            Method = $method
            Endpoint = $endpoint
        }) | Out-Null
    }
}

$seen = New-Object 'System.Collections.Generic.HashSet[string]'
$getProbeCount = 0
$successfulGets = 0
$nonSuccessGets = 0
$skipped = 0
$verbMismatch = 0

foreach ($interaction in $interactions) {
    $key = ('{0}|{1}|{2}' -f $interaction.Method, $interaction.Endpoint, $interaction.SourceFile)
    if (-not $seen.Add($key)) { continue }

    $url = $AdminApiBaseUrl.TrimEnd('/') + $interaction.Endpoint

    if ($interaction.Method -ne 'GET') {
        $skipped++
        Add-DetailRow -SourceFile $interaction.SourceFile -Line $interaction.Line -Method $interaction.Method -Endpoint $interaction.Endpoint -Url $url -Result 'SkippedUnsafeVerb' -StatusCode '' -Error ''
        continue
    }

    if (Test-ActionLikeEndpoint -Endpoint $interaction.Endpoint -SourceFile $interaction.SourceFile) {
        $skipped++
        Add-DetailRow -SourceFile $interaction.SourceFile -Line $interaction.Line -Method $interaction.Method -Endpoint $interaction.Endpoint -Url $url -Result 'SkippedActionLikeEndpoint' -StatusCode '' -Error 'Endpoint appears to be action/probe style and is not a safe GET acceptance probe.'
        continue
    }

    if ($getProbeCount -ge $MaxGetProbes) {
        $skipped++
        Add-DetailRow -SourceFile $interaction.SourceFile -Line $interaction.Line -Method $interaction.Method -Endpoint $interaction.Endpoint -Url $url -Result 'SkippedMaxGetLimit' -StatusCode '' -Error ''
        continue
    }

    $getProbeCount++
    try {
        $response = Invoke-WebRequest -Uri $url -Method GET -TimeoutSec $TimeoutSeconds -UseBasicParsing
        $status = [int]$response.StatusCode
        if ($status -ge 200 -and $status -lt 400) {
            $successfulGets++
            Add-DetailRow -SourceFile $interaction.SourceFile -Line $interaction.Line -Method 'GET' -Endpoint $interaction.Endpoint -Url $url -Result 'Success' -StatusCode ([string]$status) -Error ''
        } else {
            $nonSuccessGets++
            Add-DetailRow -SourceFile $interaction.SourceFile -Line $interaction.Line -Method 'GET' -Endpoint $interaction.Endpoint -Url $url -Result 'NonSuccess' -StatusCode ([string]$status) -Error ''
        }
    } catch {
        $statusCode = ''
        $message = $_.Exception.Message
        if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
            $statusCode = [string][int]$_.Exception.Response.StatusCode
            if ($statusCode -eq '405' -and (Test-ActionLikeEndpoint -Endpoint $interaction.Endpoint -SourceFile $interaction.SourceFile)) {
                $verbMismatch++
                Add-DetailRow -SourceFile $interaction.SourceFile -Line $interaction.Line -Method 'GET' -Endpoint $interaction.Endpoint -Url $url -Result 'SkippedVerbMismatch' -StatusCode $statusCode -Error 'Endpoint exists but rejects GET; classified as action/probe endpoint.'
                continue
            }
        }
        $nonSuccessGets++
        Add-DetailRow -SourceFile $interaction.SourceFile -Line $interaction.Line -Method 'GET' -Endpoint $interaction.Endpoint -Url $url -Result 'RequestFailed' -StatusCode $statusCode -Error $message
    }
}

$finishedUtc = (Get-Date).ToUniversalTime().ToString('o')
$details | Export-Csv -LiteralPath $detailsPath -NoTypeInformation -Encoding UTF8

$summary.Add('# P10.3D Repair - Admin Web Page API Interaction Coverage') | Out-Null
$summary.Add('') | Out-Null
$summary.Add(('Started UTC: `{0}`' -f $startedUtc)) | Out-Null
$summary.Add(('Finished UTC: `{0}`' -f $finishedUtc)) | Out-Null
$summary.Add(('Admin API base URL: `{0}`' -f $AdminApiBaseUrl)) | Out-Null
$summary.Add(('Timeout seconds: `{0}`' -f $TimeoutSeconds)) | Out-Null
$summary.Add('') | Out-Null
$summary.Add(('Discovered endpoint interactions: `{0}`' -f $interactions.Count)) | Out-Null
$summary.Add(('Unique method/path/source interactions: `{0}`' -f $seen.Count)) | Out-Null
$summary.Add(('Successful GET probes: `{0}`' -f $successfulGets)) | Out-Null
$summary.Add(('Non-success GET probes: `{0}`' -f $nonSuccessGets)) | Out-Null
$summary.Add(('Skipped/action/non-GET interactions: `{0}`' -f $skipped)) | Out-Null
$summary.Add(('GET verb mismatches classified: `{0}`' -f $verbMismatch)) | Out-Null
$summary.Add('') | Out-Null
$summary.Add(('Details: `{0}`' -f $detailsPath)) | Out-Null
Set-Content -LiteralPath $summaryPath -Value $summary.ToArray() -Encoding UTF8

Write-Host ('Wrote summary: {0}' -f $summaryPath)
Write-Host ('Wrote details: {0}' -f $detailsPath)

if ($nonSuccessGets -gt 0) {
    throw ('Page API interaction coverage completed with {0} non-success GET probe(s). Review {1}' -f $nonSuccessGets, $summaryPath)
}

Write-Host 'Page API interaction coverage completed successfully.'
'@

Set-Content -LiteralPath $runnerPath -Value $runner -Encoding UTF8

$report = New-Object 'System.Collections.Generic.List[string]'
$report.Add('# P10.3D Repair - Admin Web Page API Interaction Coverage') | Out-Null
$report.Add('') | Out-Null
$report.Add('Repairs the P10.3D runtime coverage runner classification so action/probe endpoints are not treated as safe GET acceptance failures.') | Out-Null
$report.Add('') | Out-Null
$report.Add('The observed P10.3D non-success GET probes were 405 Method Not Allowed on cloud artifact/storage probe endpoints. Those are action/probe-style endpoints and should be skipped or classified as verb mismatch rather than failing site-up route/API acceptance.') | Out-Null
$report.Add('') | Out-Null
$report.Add(('Runner: `{0}`' -f $runnerPath)) | Out-Null
Set-Content -LiteralPath $reportPath -Value $report.ToArray() -Encoding UTF8

Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.3D Repair Admin Web page API interaction coverage applied.'
