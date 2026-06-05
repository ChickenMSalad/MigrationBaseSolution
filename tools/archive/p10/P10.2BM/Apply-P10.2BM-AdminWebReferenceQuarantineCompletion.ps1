Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path

$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$sourceRoot = Join-Path $adminWebRoot 'src'
$featuresRoot = Join-Path $sourceRoot 'features'
$referenceFeaturesRoot = Join-Path $adminWebRoot 'reference\apps-migration-admin-ui\src\features'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2BM-AdminWebReferenceQuarantineCompletion.Report.md'

if (-not (Test-Path -Path $adminWebRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}
if (-not (Test-Path -Path $sourceRoot -PathType Container)) {
    throw ('Admin Web source root was not found: {0}' -f $sourceRoot)
}
if (-not (Test-Path -Path $featuresRoot -PathType Container)) {
    throw ('Admin Web features root was not found: {0}' -f $featuresRoot)
}

$report = New-Object System.Collections.Generic.List[string]
[void]$report.Add('# P10.2BM - Admin Web Reference Quarantine Completion')
[void]$report.Add('')
[void]$report.Add(('Generated: {0:O}' -f (Get-Date)))
[void]$report.Add('')

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

if (-not (Test-Path -Path (Split-Path -Parent $reportPath) -PathType Container)) {
    New-Item -Path (Split-Path -Parent $reportPath) -ItemType Directory -Force | Out-Null
}
if (-not (Test-Path -Path $referenceFeaturesRoot -PathType Container)) {
    New-Item -Path $referenceFeaturesRoot -ItemType Directory -Force | Out-Null
}

[void]$report.Add('## Candidate Results')
[void]$report.Add('')

foreach ($candidateName in $candidateNames) {
    $sourceFolder = Join-Path $featuresRoot $candidateName
    $targetFolder = Join-Path $referenceFeaturesRoot $candidateName

    if (-not (Test-Path -Path $sourceFolder -PathType Container)) {
        [void]$report.Add(('- Already absent from compiled features: `{0}`' -f $candidateName))
        continue
    }

    $referenceTokens = @(
        ('features/{0}/' -f $candidateName),
        ('features\{0}\' -f $candidateName),
        ('./features/{0}/' -f $candidateName),
        ('./features\{0}\' -f $candidateName)
    )

    $referencingFiles = New-Object System.Collections.Generic.List[string]
    foreach ($sourceFile in $sourceFiles) {
        if ($sourceFile.FullName -like (Join-Path $sourceFolder '*')) {
            continue
        }

        $content = Get-Content -Path $sourceFile.FullName -Raw
        $hasReference = $false
        foreach ($token in $referenceTokens) {
            if ($content.IndexOf($token, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                $hasReference = $true
                break
            }
        }

        if ($hasReference) {
            $relativePath = $sourceFile.FullName.Substring($repoRoot.Length).TrimStart('\')
            [void]$referencingFiles.Add($relativePath)
        }
    }

    if ($referencingFiles.Count -gt 0) {
        [void]$report.Add(('- Skipped referenced folder: `{0}`' -f $candidateName))
        foreach ($relativePath in $referencingFiles) {
            [void]$report.Add(('  - reference from `{0}`' -f $relativePath))
        }
        continue
    }

    if (-not (Test-Path -Path $targetFolder -PathType Container)) {
        Move-Item -Path $sourceFolder -Destination $targetFolder
        [void]$report.Add(('- Moved to reference: `{0}`' -f $candidateName))
        Write-Host ('Moved to reference: {0}' -f $candidateName)
        continue
    }

    $allSourceFiles = @(Get-ChildItem -Path $sourceFolder -Recurse -File)
    foreach ($file in $allSourceFiles) {
        $relative = $file.FullName.Substring($sourceFolder.Length).TrimStart('\')
        $destination = Join-Path $targetFolder $relative
        $destinationParent = Split-Path -Parent $destination
        if (-not (Test-Path -Path $destinationParent -PathType Container)) {
            New-Item -Path $destinationParent -ItemType Directory -Force | Out-Null
        }
        if (-not (Test-Path -Path $destination -PathType Leaf)) {
            Copy-Item -Path $file.FullName -Destination $destination
        }
    }

    $allCopied = $true
    foreach ($file in $allSourceFiles) {
        $relative = $file.FullName.Substring($sourceFolder.Length).TrimStart('\')
        $destination = Join-Path $targetFolder $relative
        if (-not (Test-Path -Path $destination -PathType Leaf)) {
            $allCopied = $false
            break
        }
    }

    if ($allCopied) {
        Remove-Item -Path $sourceFolder -Recurse -Force
        [void]$report.Add(('- Merged into existing reference and removed compiled folder: `{0}`' -f $candidateName))
        Write-Host ('Merged into reference: {0}' -f $candidateName)
    }
    else {
        [void]$report.Add(('- Copied but retained because verification failed: `{0}`' -f $candidateName))
    }
}

[void]$report.Add('')
[void]$report.Add('## Remaining top-level feature folders')
[void]$report.Add('')
$remainingFeatureFolders = @(Get-ChildItem -Path $featuresRoot -Directory | Sort-Object -Property Name)
foreach ($folder in $remainingFeatureFolders) {
    [void]$report.Add(('- `{0}`' -f $folder.Name))
}

Set-Content -Path $reportPath -Value $report -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2BM Admin Web reference quarantine completion applied.'
