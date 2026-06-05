Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$sourceRoot = Join-Path $adminWebRoot 'src'
$featuresRoot = Join-Path $sourceRoot 'features'
$referenceRoot = Join-Path $adminWebRoot 'reference\apps-migration-admin-ui\src\features'
$docsRoot = Join-Path $repoRoot 'docs\P10'
$reportPath = Join-Path $docsRoot 'P10.2BI-AdminWebAppsReferenceQuarantine.Report.md'

if (-not (Test-Path -Path $featuresRoot -PathType Container)) {
    throw ('Canonical Admin Web features folder was not found: {0}' -f $featuresRoot)
}

if (-not (Test-Path -Path $docsRoot -PathType Container)) {
    New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
}
if (-not (Test-Path -Path $referenceRoot -PathType Container)) {
    New-Item -ItemType Directory -Path $referenceRoot -Force | Out-Null
}

$canonicalGroupNames = @(
    'connectors',
    'governance',
    'operations',
    'platform',
    'security'
)

$appsReferenceCandidateNames = @(
    'audit',
    'capacity',
    'commandCenter',
    'cost',
    'credentials',
    'events',
    'executionProfiles',
    'executionSessions',
    'executionWorkers',
    'notifications',
    'operational',
    'slaSlo',
    'sqlHealth',
    'workers'
)

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2BI - Admin Web Apps Reference Quarantine')
[void]$report.Add('')
[void]$report.Add(('Generated: {0:yyyy-MM-dd HH:mm:ss}' -f (Get-Date)))
[void]$report.Add('')
[void]$report.Add('## Intent')
[void]$report.Add('')
[void]$report.Add('Move ungrouped apps-style residual feature folders out of the compiled canonical Admin Web src/features tree while preserving them as reference material inside the Admin Web project.')
[void]$report.Add('')
[void]$report.Add('## Canonical grouped feature folders expected to remain in src/features')
[void]$report.Add('')
foreach ($name in $canonicalGroupNames) {
    $path = Join-Path $featuresRoot $name
    if (Test-Path -Path $path -PathType Container) {
        [void]$report.Add(('- present: {0}' -f $name))
    }
    else {
        [void]$report.Add(('- missing: {0}' -f $name))
    }
}
[void]$report.Add('')
[void]$report.Add('## Apps-style residual feature folders')
[void]$report.Add('')

foreach ($name in $appsReferenceCandidateNames) {
    $sourcePath = Join-Path $featuresRoot $name
    $targetPath = Join-Path $referenceRoot $name

    if (Test-Path -Path $sourcePath -PathType Container) {
        if (Test-Path -Path $targetPath -PathType Container) {
            [void]$report.Add(('- already preserved, leaving source in place for manual review: {0}' -f $name))
            continue
        }

        $targetParent = Split-Path -Parent $targetPath
        if (-not (Test-Path -Path $targetParent -PathType Container)) {
            New-Item -ItemType Directory -Path $targetParent -Force | Out-Null
        }

        Move-Item -Path $sourcePath -Destination $targetPath
        [void]$report.Add(('- moved to reference: {0}' -f $name))
        Write-Host ('Moved apps reference feature folder: {0}' -f $name)
    }
    elseif (Test-Path -Path $targetPath -PathType Container) {
        [void]$report.Add(('- already in reference: {0}' -f $name))
        Write-Host ('Already in apps reference area: {0}' -f $name)
    }
    else {
        [void]$report.Add(('- not present locally: {0}' -f $name))
    }
}

[void]$report.Add('')
[void]$report.Add('## Notes')
[void]$report.Add('')
[void]$report.Add('- This set does not touch App.tsx.')
[void]$report.Add('- This set does not delete apps reference material.')
[void]$report.Add('- Reference material is intentionally outside src so TypeScript build scope stays focused on the deployable Admin Web source tree.')

Set-Content -Path $reportPath -Value $report -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2BI Admin Web apps reference quarantine applied.'
