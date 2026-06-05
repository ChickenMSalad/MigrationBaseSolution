Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    $scriptRootValue = $null
    if (Test-Path -Path 'variable:PSScriptRoot') {
        $scriptRootValue = $PSScriptRoot
    }

    if ([string]::IsNullOrWhiteSpace($scriptRootValue)) {
        $scriptRootValue = Split-Path -Parent $MyInvocation.MyCommand.Path
    }

    $candidate = Resolve-Path -Path (Join-Path -Path $scriptRootValue -ChildPath '..\..\..')
    return $candidate.Path
}

function Add-ReportText {
    param(
        [Parameter(Mandatory=$true)]
        [string] $Line
    )

    [void] $script:reportLines.Add($Line)
}

function Get-LeafFiles {
    param(
        [Parameter(Mandatory=$true)]
        [string] $Path
    )

    if (-not (Test-Path -Path $Path -PathType Container)) {
        return @()
    }

    return @(Get-ChildItem -Path $Path -File -Recurse | Sort-Object -Property FullName)
}

function Repair-AppImports {
    param(
        [Parameter(Mandatory=$true)]
        [string] $AppPath
    )

    if (-not (Test-Path -Path $AppPath -PathType Leaf)) {
        throw ('App.tsx was not found: {0}' -f $AppPath)
    }

    $original = Get-Content -Path $AppPath -Raw
    $content = $original.Replace('"";', '";')

    $segments = @($content -split ';')
    $importStatements = New-Object System.Collections.ArrayList
    $importKeys = New-Object 'System.Collections.Generic.HashSet[string]'
    $remainingSegments = New-Object System.Collections.ArrayList
    $insideImportPrefix = $true
    $duplicateCount = 0

    foreach ($segment in $segments) {
        $trimmed = $segment.Trim()
        if ($insideImportPrefix -and $trimmed.StartsWith('import ')) {
            if ($importKeys.Add($trimmed)) {
                [void] $importStatements.Add($trimmed)
            }
            else {
                $duplicateCount++
            }
        }
        else {
            $insideImportPrefix = $false
            if ($trimmed.Length -gt 0) {
                [void] $remainingSegments.Add($segment)
            }
        }
    }

    $rebuilt = $content
    if ($importStatements.Count -gt 0 -and $remainingSegments.Count -gt 0) {
        $importText = (($importStatements | ForEach-Object { $_ + ';' }) -join ' ')
        $remainingText = ($remainingSegments -join ';')
        $rebuilt = ($importText + ' ' + $remainingText).Trim()
    }

    if ($rebuilt -ne $original) {
        Set-Content -Path $AppPath -Value $rebuilt -Encoding UTF8
        Add-ReportText -Line ('Updated App.tsx import hygiene. RemovedDuplicateImports={0}' -f $duplicateCount)
        Write-Host 'Updated App.tsx import hygiene.'
    }
    else {
        Add-ReportText -Line 'App.tsx import hygiene already clean.'
        Write-Host 'App.tsx import hygiene already clean.'
    }
}

$repoRoot = Get-RepositoryRoot
$adminWebRoot = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web')
$sourceRoot = [System.IO.Path]::Combine($adminWebRoot, 'src')
$appPath = [System.IO.Path]::Combine($sourceRoot, 'App.tsx')
$featuresRoot = [System.IO.Path]::Combine($sourceRoot, 'features')
$appsSourceRoot = [System.IO.Path]::Combine($repoRoot, 'apps', 'migration-admin-ui', 'src')
$docsRoot = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10')
$reportPath = [System.IO.Path]::Combine($docsRoot, 'P10.2BA-AdminWebConsolidationBuildReadiness.Report.md')

foreach ($requiredPath in @($adminWebRoot, $sourceRoot, $featuresRoot)) {
    if (-not (Test-Path -Path $requiredPath -PathType Container)) {
        throw ('Required directory was not found: {0}' -f $requiredPath)
    }
}

if (-not (Test-Path -Path $appPath -PathType Leaf)) {
    throw ('Required file was not found: {0}' -f $appPath)
}

if (-not (Test-Path -Path $docsRoot -PathType Container)) {
    New-Item -Path $docsRoot -ItemType Directory -Force | Out-Null
}

$script:reportLines = New-Object System.Collections.ArrayList
Add-ReportText -Line '# P10.2BA - Admin Web Consolidation Build Readiness Report'
Add-ReportText -Line ''
Add-ReportText -Line ('GeneratedUtc: {0}' -f ([DateTime]::UtcNow.ToString('u')))
Add-ReportText -Line ''
Add-ReportText -Line '## Source Roots'
Add-ReportText -Line ('CanonicalAdminWeb: {0}' -f $adminWebRoot)
Add-ReportText -Line ('CanonicalSource: {0}' -f $sourceRoot)
Add-ReportText -Line ('AppsReferenceSourceExists: {0}' -f (Test-Path -Path $appsSourceRoot -PathType Container))
Add-ReportText -Line ''

Repair-AppImports -AppPath $appPath

Add-ReportText -Line ''
Add-ReportText -Line '## Residual Flat Folder Counts'
foreach ($folderName in @('pages', 'api', 'types')) {
    $folderPath = [System.IO.Path]::Combine($sourceRoot, $folderName)
    $files = @(Get-LeafFiles -Path $folderPath)
    Add-ReportText -Line ('{0}: {1}' -f $folderName, $files.Length)
    if ($files.Length -gt 0) {
        foreach ($file in $files) {
            $relative = $file.FullName.Substring($sourceRoot.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
            Add-ReportText -Line ('- {0}' -f ($relative -replace '\\', '/'))
        }
    }
}

Add-ReportText -Line ''
Add-ReportText -Line '## Feature Folder Count'
$featureFiles = @(Get-LeafFiles -Path $featuresRoot)
Add-ReportText -Line ('FeatureFiles: {0}' -f $featureFiles.Length)

Add-ReportText -Line ''
Add-ReportText -Line '## Build Commands'
Add-ReportText -Line 'Run manually from src/Admin/Migration.Admin.Web when ready:'
Add-ReportText -Line '```powershell'
Add-ReportText -Line 'npm install'
Add-ReportText -Line 'npm run build'
Add-ReportText -Line '```'

Set-Content -Path $reportPath -Value $script:reportLines -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2BA Admin Web consolidation build readiness applied.'
