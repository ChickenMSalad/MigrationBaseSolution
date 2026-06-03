param(
    [string]$AdminWebBaseUrl = 'http://localhost:5173',
    [string]$AdminApiBaseUrl = 'https://localhost:55436'
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.3F'
if (-not (Test-Path -LiteralPath $artifactRoot)) {
    New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
}

function Get-LatestSummaryPath {
    param(
        [string]$Pattern
    )

    $artifactsRoot = Join-Path $repoRoot 'artifacts\p10'
    if (-not (Test-Path -LiteralPath $artifactsRoot)) {
        return $null
    }

    $items = Get-ChildItem -Path $artifactsRoot -Recurse -File -Filter $Pattern -ErrorAction SilentlyContinue | Sort-Object LastWriteTimeUtc -Descending
    foreach ($item in $items) {
        return $item.FullName
    }

    return $null
}

function Get-SummaryValue {
    param(
        [string]$Path,
        [string]$Label
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $null
    }
    if (-not (Test-Path -LiteralPath $Path)) {
        return $null
    }

    $lines = Get-Content -LiteralPath $Path
    foreach ($line in $lines) {
        $needle = ('{0}:' -f $Label)
        if ($line -like ('*' + $needle + '*')) {
            $parts = $line.Split('`')
            if ($parts.Length -ge 2) {
                return $parts[1]
            }
            return $line
        }
    }

    return $null
}

$checks = New-Object 'System.Collections.Generic.List[object]'

$runtimeSummary = Get-LatestSummaryPath -Pattern 'runtime-acceptance.summary.md'
$routeSummary = Get-LatestSummaryPath -Pattern 'runtime-route-acceptance.summary.md'
$browserSummary = Get-LatestSummaryPath -Pattern 'browser-runtime-health.summary.md'
$pageApiSummary = Get-LatestSummaryPath -Pattern 'page-api-interaction-coverage.summary.md'
$operatorSummary = Get-LatestSummaryPath -Pattern 'operator-workflow-acceptance.summary.md'

$summaries = @(
    @{ Name = 'Runtime acceptance'; Path = $runtimeSummary; NonSuccessLabel = 'Non-success probes' },
    @{ Name = 'Route acceptance'; Path = $routeSummary; NonSuccessLabel = 'Non-success probes' },
    @{ Name = 'Browser runtime health'; Path = $browserSummary; NonSuccessLabel = 'Non-success probes' },
    @{ Name = 'Page API coverage'; Path = $pageApiSummary; NonSuccessLabel = 'Real non-success GET probes' },
    @{ Name = 'Operator workflow acceptance'; Path = $operatorSummary; NonSuccessLabel = 'Non-success probes' }
)

foreach ($summary in $summaries) {
    $name = [string]$summary.Name
    $path = [string]$summary.Path
    $label = [string]$summary.NonSuccessLabel
    $status = 'MissingEvidence'
    $details = 'Summary file was not found.'

    if (-not [string]::IsNullOrWhiteSpace($path)) {
        $nonSuccess = Get-SummaryValue -Path $path -Label $label
        if ($null -eq $nonSuccess) {
            $status = 'Present'
            $details = 'Summary exists, but the expected non-success metric was not found.'
        }
        elseif ($nonSuccess -eq '0') {
            $status = 'Pass'
            $details = ('{0}: {1}' -f $label, $nonSuccess)
        }
        else {
            $status = 'Review'
            $details = ('{0}: {1}' -f $label, $nonSuccess)
        }
    }

    $checks.Add([pscustomobject]@{
        Area = $name
        Status = $status
        Details = $details
        EvidencePath = $path
    }) | Out-Null
}

$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$packageJson = Join-Path $adminWebRoot 'package.json'
$distIndex = Join-Path $adminWebRoot 'dist\index.html'

$checks.Add([pscustomobject]@{
    Area = 'Admin Web package'
    Status = $(if (Test-Path -LiteralPath $packageJson) { 'Pass' } else { 'Review' })
    Details = 'package.json presence check'
    EvidencePath = $packageJson
}) | Out-Null

$checks.Add([pscustomobject]@{
    Area = 'Admin Web production dist'
    Status = $(if (Test-Path -LiteralPath $distIndex) { 'Pass' } else { 'Review' })
    Details = 'dist/index.html presence check after production build'
    EvidencePath = $distIndex
}) | Out-Null

$csvPath = Join-Path $artifactRoot 'release-readiness-evidence.details.csv'
$summaryPath = Join-Path $artifactRoot 'release-readiness-evidence.summary.md'
$checks | Export-Csv -LiteralPath $csvPath -NoTypeInformation -Encoding UTF8

$passCount = 0
$reviewCount = 0
$missingCount = 0
foreach ($check in $checks) {
    if ($check.Status -eq 'Pass') {
        $passCount = $passCount + 1
    }
    elseif ($check.Status -eq 'MissingEvidence') {
        $missingCount = $missingCount + 1
    }
    else {
        $reviewCount = $reviewCount + 1
    }
}

$summaryLines = New-Object 'System.Collections.Generic.List[string]'
[void]$summaryLines.Add('# P10.3F - Admin Web Release Readiness Evidence Gate')
[void]$summaryLines.Add('')
[void]$summaryLines.Add(('Generated UTC: `{0}`' -f ([DateTime]::UtcNow.ToString('o'))))
[void]$summaryLines.Add(('Admin Web base URL: `{0}`' -f $AdminWebBaseUrl))
[void]$summaryLines.Add(('Admin API base URL: `{0}`' -f $AdminApiBaseUrl))
[void]$summaryLines.Add('')
[void]$summaryLines.Add(('Total checks: `{0}`' -f $checks.Count))
[void]$summaryLines.Add(('Pass checks: `{0}`' -f $passCount))
[void]$summaryLines.Add(('Review checks: `{0}`' -f $reviewCount))
[void]$summaryLines.Add(('Missing evidence checks: `{0}`' -f $missingCount))
[void]$summaryLines.Add('')
[void]$summaryLines.Add('## Checks')
[void]$summaryLines.Add('')
foreach ($check in $checks) {
    [void]$summaryLines.Add(('- {0}: `{1}` - {2}' -f $check.Area, $check.Status, $check.Details))
}
[void]$summaryLines.Add('')
[void]$summaryLines.Add(('Details: `{0}`' -f $csvPath))

Set-Content -LiteralPath $summaryPath -Value $summaryLines.ToArray() -Encoding UTF8
Write-Host ('Wrote summary: {0}' -f $summaryPath)
Write-Host ('Wrote details: {0}' -f $csvPath)

if ($missingCount -gt 0) {
    throw ('Release readiness evidence gate has {0} missing evidence check(s). Review {1}' -f $missingCount, $summaryPath)
}
if ($reviewCount -gt 0) {
    throw ('Release readiness evidence gate has {0} review check(s). Review {1}' -f $reviewCount, $summaryPath)
}

Write-Host 'P10.3F release readiness evidence gate passed.'
