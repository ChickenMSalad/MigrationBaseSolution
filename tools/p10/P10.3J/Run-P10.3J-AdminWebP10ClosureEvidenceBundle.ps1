param(
    [string]$AdminWebBaseUrl = 'http://localhost:5173',
    [string]$AdminApiBaseUrl = 'https://localhost:55436'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$repoRoot = $repoRoot.ProviderPath
$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.3J'
if (-not (Test-Path -LiteralPath $artifactRoot)) {
    New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
}

$summaryPath = Join-Path $artifactRoot 'p10-closure-evidence.summary.md'
$detailsPath = Join-Path $artifactRoot 'p10-closure-evidence.details.csv'

$checks = New-Object 'System.Collections.Generic.List[object]'

function Add-Check {
    param(
        [string]$Name,
        [string]$Category,
        [string]$Path,
        [bool]$Required
    )

    $fullPath = Join-Path $repoRoot $Path
    $exists = Test-Path -LiteralPath $fullPath
    $status = 'Present'
    if (-not $exists) {
        if ($Required) {
            $status = 'Missing'
        }
        else {
            $status = 'OptionalMissing'
        }
    }

    $item = [pscustomobject]@{
        Name = $Name
        Category = $Category
        RelativePath = $Path
        Required = $Required
        Status = $status
    }
    [void]$checks.Add($item)
}

Add-Check -Name 'Runtime acceptance' -Category 'Runtime' -Path 'artifacts\p10\P10.3A\runtime-acceptance.summary.md' -Required $true
Add-Check -Name 'Route acceptance' -Category 'Runtime' -Path 'artifacts\p10\P10.3B\runtime-route-acceptance.summary.md' -Required $true
Add-Check -Name 'Browser runtime health' -Category 'Runtime' -Path 'artifacts\p10\P10.3C\browser-runtime-health.summary.md' -Required $true
Add-Check -Name 'Page API coverage' -Category 'Runtime' -Path 'artifacts\p10\P10.3D-Repair3\page-api-interaction-coverage.summary.md' -Required $true
Add-Check -Name 'Operator workflow acceptance' -Category 'Runtime' -Path 'artifacts\p10\P10.3E-Repair2\operator-workflow-acceptance.summary.md' -Required $true
Add-Check -Name 'Release readiness evidence gate' -Category 'Release' -Path 'artifacts\p10\P10.3F\release-readiness-evidence.summary.md' -Required $false
Add-Check -Name 'Site-up runbook' -Category 'Documentation' -Path 'docs\P10\P10.3G-AdminWebSiteUpRunbook.md' -Required $true
Add-Check -Name 'Manual UX checklist' -Category 'Documentation' -Path 'docs\P10\P10.3H-AdminWebManualUxAcceptanceChecklist.md' -Required $true
Add-Check -Name 'Production hardening inventory' -Category 'Documentation' -Path 'docs\P10\P10.3I-AdminWebProductionHardeningInventory.md' -Required $true
Add-Check -Name 'Admin Web package' -Category 'Source' -Path 'src\Admin\Migration.Admin.Web\package.json' -Required $true
Add-Check -Name 'Admin Web Vite config' -Category 'Source' -Path 'src\Admin\Migration.Admin.Web\vite.config.ts' -Required $true

$checks | Export-Csv -LiteralPath $detailsPath -NoTypeInformation -Encoding UTF8

$total = $checks.Count
$present = 0
$missingRequired = 0
$optionalMissing = 0
foreach ($check in $checks) {
    if ($check.Status -eq 'Present') { $present++ }
    if ($check.Status -eq 'Missing') { $missingRequired++ }
    if ($check.Status -eq 'OptionalMissing') { $optionalMissing++ }
}

$summary = New-Object 'System.Collections.Generic.List[string]'
[void]$summary.Add('# P10.3J - Admin Web P10 Closure Evidence Bundle')
[void]$summary.Add('')
[void]$summary.Add(('Generated UTC: `{0}`' -f ([DateTime]::UtcNow.ToString('o'))))
[void]$summary.Add(('Admin Web base URL: `{0}`' -f $AdminWebBaseUrl))
[void]$summary.Add(('Admin API base URL: `{0}`' -f $AdminApiBaseUrl))
[void]$summary.Add('')
[void]$summary.Add(('Total checks: `{0}`' -f $total))
[void]$summary.Add(('Present: `{0}`' -f $present))
[void]$summary.Add(('Missing required: `{0}`' -f $missingRequired))
[void]$summary.Add(('Optional missing: `{0}`' -f $optionalMissing))
[void]$summary.Add('')
[void]$summary.Add('## Evidence Status')
[void]$summary.Add('')
[void]$summary.Add('| Category | Name | Status | Required | Path |')
[void]$summary.Add('| --- | --- | --- | --- | --- |')
foreach ($check in $checks) {
    [void]$summary.Add(('| {0} | {1} | {2} | {3} | `{4}` |' -f $check.Category, $check.Name, $check.Status, $check.Required, $check.RelativePath))
}
[void]$summary.Add('')
[void]$summary.Add('## Deferred Work')
[void]$summary.Add('')
[void]$summary.Add('- Builder feature parity restoration from the recovered known-good site commit.')
[void]$summary.Add('- Any product-specific page/API restoration that requires explicit parity decisions.')
[void]$summary.Add('- Deployment environment-specific configuration outside local site-up evidence.')
[void]$summary.Add('')
[void]$summary.Add(('Details: `{0}`' -f $detailsPath))

Set-Content -LiteralPath $summaryPath -Value $summary.ToArray() -Encoding UTF8
Write-Host ('Wrote summary: {0}' -f $summaryPath)
Write-Host ('Wrote details: {0}' -f $detailsPath)

if ($missingRequired -gt 0) {
    throw ('P10 closure evidence bundle has {0} missing required artifact(s). Review {1}' -f $missingRequired, $summaryPath)
}

Write-Host 'P10 closure evidence bundle passed required artifact checks.'
