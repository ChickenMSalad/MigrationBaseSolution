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

$repoRoot = Get-RepositoryRoot
$adminWebRoot = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web')
$sourceRoot = [System.IO.Path]::Combine($adminWebRoot, 'src')
$appPath = [System.IO.Path]::Combine($sourceRoot, 'App.tsx')
$featuresRoot = [System.IO.Path]::Combine($sourceRoot, 'features')
$reportPath = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10', 'P10.2BA-AdminWebConsolidationBuildReadiness.Report.md')

foreach ($requiredPath in @($adminWebRoot, $sourceRoot, $featuresRoot)) {
    if (-not (Test-Path -Path $requiredPath -PathType Container)) {
        throw ('Required directory was not found: {0}' -f $requiredPath)
    }
}

foreach ($requiredFile in @($appPath, $reportPath)) {
    if (-not (Test-Path -Path $requiredFile -PathType Leaf)) {
        throw ('Required file was not found: {0}' -f $requiredFile)
    }
}

$appContent = Get-Content -Path $appPath -Raw
if ($appContent.Contains('"";')) {
    throw ('Malformed doubled quote import ending remains in {0}' -f $appPath)
}

$segments = @($appContent -split ';')
$seenImports = New-Object 'System.Collections.Generic.HashSet[string]'
$duplicateImports = New-Object System.Collections.ArrayList
foreach ($segment in $segments) {
    $trimmed = $segment.Trim()
    if ($trimmed.StartsWith('import ')) {
        if (-not $seenImports.Add($trimmed)) {
            [void] $duplicateImports.Add($trimmed)
        }
    }
}

if ($duplicateImports.Count -gt 0) {
    throw ('Duplicate import statements remain in App.tsx: {0}' -f ($duplicateImports -join ' | '))
}

$reportContent = Get-Content -Path $reportPath -Raw
if (-not $reportContent.Contains('P10.2BA - Admin Web Consolidation Build Readiness Report')) {
    throw ('Report marker was not found in {0}' -f $reportPath)
}

Write-Host 'P10.2BA Admin Web consolidation build readiness validation passed.'
