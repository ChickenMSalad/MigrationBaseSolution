Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$sourceRoot = Join-Path $adminWebRoot 'src'
$featuresRoot = Join-Path $sourceRoot 'features'
$referenceRoot = Join-Path $adminWebRoot 'reference\apps-migration-admin-ui\src\features'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2BI-AdminWebAppsReferenceQuarantine.Report.md'

if (-not (Test-Path -Path $featuresRoot -PathType Container)) {
    throw ('Canonical Admin Web features folder was not found: {0}' -f $featuresRoot)
}
if (-not (Test-Path -Path $referenceRoot -PathType Container)) {
    throw ('Apps reference features folder was not found: {0}' -f $referenceRoot)
}
if (-not (Test-Path -Path $reportPath -PathType Leaf)) {
    throw ('Expected report was not found: {0}' -f $reportPath)
}

$canonicalGroupNames = @(
    'connectors',
    'governance',
    'operations',
    'platform',
    'security'
)
foreach ($name in $canonicalGroupNames) {
    $path = Join-Path $featuresRoot $name
    if (-not (Test-Path -Path $path -PathType Container)) {
        throw ('Expected canonical grouped feature folder was not found: {0}' -f $path)
    }
}

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

$violations = New-Object 'System.Collections.Generic.List[string]'
foreach ($name in $appsReferenceCandidateNames) {
    $compiledPath = Join-Path $featuresRoot $name
    $referencePath = Join-Path $referenceRoot $name
    if ((Test-Path -Path $compiledPath -PathType Container) -and (Test-Path -Path $referencePath -PathType Container)) {
        [void]$violations.Add(('Apps-style feature exists in both compiled src/features and reference area: {0}' -f $name))
    }
}

if ($violations.Count -gt 0) {
    foreach ($line in $violations) {
        Write-Host $line
    }
    throw 'Apps reference quarantine left duplicate compiled/reference feature folders.'
}

$reportContent = Get-Content -Path $reportPath -Raw
if ([string]::IsNullOrWhiteSpace($reportContent)) {
    throw ('Expected report was empty: {0}' -f $reportPath)
}

Write-Host 'P10.2BI Admin Web apps reference quarantine validation passed.'
