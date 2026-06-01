Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path

$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$sourceRoot = Join-Path $adminWebRoot 'src'
$featuresRoot = Join-Path $sourceRoot 'features'
$referenceFeaturesRoot = Join-Path $adminWebRoot 'reference\apps-migration-admin-ui\src\features'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2BM-AdminWebReferenceQuarantineCompletion.Report.md'

if (-not (Test-Path -Path $featuresRoot -PathType Container)) {
    throw ('Features root was not found: {0}' -f $featuresRoot)
}
if (-not (Test-Path -Path $referenceFeaturesRoot -PathType Container)) {
    throw ('Reference features root was not found: {0}' -f $referenceFeaturesRoot)
}
if (-not (Test-Path -Path $reportPath -PathType Leaf)) {
    throw ('Expected report was not found: {0}' -f $reportPath)
}

$canonicalFolderNames = @('connectors', 'governance', 'operations', 'platform', 'security')
foreach ($folderName in $canonicalFolderNames) {
    $folderPath = Join-Path $featuresRoot $folderName
    if (-not (Test-Path -Path $folderPath -PathType Container)) {
        throw ('Canonical feature folder was not found: {0}' -f $folderPath)
    }
}

$candidateNames = @(
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

$sourceFiles = @(
    Get-ChildItem -Path $sourceRoot -Recurse -File -Include '*.ts','*.tsx' |
        Where-Object {
            $fullName = $_.FullName
            ($fullName -notlike '*\reference\*') -and
            ($fullName -notlike '*\node_modules\*') -and
            ($fullName -notlike '*\dist\*')
        }
)

foreach ($candidateName in $candidateNames) {
    $compiledFolder = Join-Path $featuresRoot $candidateName
    if (-not (Test-Path -Path $compiledFolder -PathType Container)) {
        continue
    }

    $referenceTokens = @(
        ('features/{0}/' -f $candidateName),
        ('features\{0}\' -f $candidateName),
        ('./features/{0}/' -f $candidateName),
        ('./features\{0}\' -f $candidateName)
    )

    $isReferenced = $false
    foreach ($sourceFile in $sourceFiles) {
        if ($sourceFile.FullName -like (Join-Path $compiledFolder '*')) {
            continue
        }
        $content = Get-Content -Path $sourceFile.FullName -Raw
        foreach ($token in $referenceTokens) {
            if ($content.IndexOf($token, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                $isReferenced = $true
                break
            }
        }
        if ($isReferenced) {
            break
        }
    }

    if (-not $isReferenced) {
        throw ('Unreferenced apps-style folder still remains in compiled features: {0}' -f $compiledFolder)
    }
}

Write-Host 'P10.2BM Admin Web reference quarantine completion validation passed.'
