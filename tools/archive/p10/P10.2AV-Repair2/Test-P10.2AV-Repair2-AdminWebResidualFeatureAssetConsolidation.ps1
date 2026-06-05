Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    $current = $PSScriptRoot
    while ($null -ne $current -and $current -ne '') {
        $candidate = [System.IO.Path]::Combine($current, 'src', 'Admin', 'Migration.Admin.Web', 'src')
        if ([System.IO.Directory]::Exists($candidate)) {
            return $current
        }
        $parent = [System.IO.Directory]::GetParent($current)
        if ($null -eq $parent) { break }
        $current = $parent.FullName
    }
    throw 'Unable to locate repository root from script location.'
}

function Require-File {
    param(
        [string] $Path,
        [string] $Label
    )
    if (-not ([System.IO.File]::Exists($Path))) {
        throw ('Required file missing for {0}: {1}' -f $Label, $Path)
    }
}

$repoRoot = Get-RepositoryRoot
$sourceRoot = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$featureRoot = [System.IO.Path]::Combine($sourceRoot, 'features')
$reportPath = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10', 'P10.2AV-Repair2-AdminWebResidualFeatureAssetConsolidation.md')
Require-File -Path $reportPath -Label 'AV Repair2 report'

if (-not [System.IO.Directory]::Exists($featureRoot)) {
    throw ('Feature root missing: {0}' -f $featureRoot)
}

$scriptFiles = @(
    [System.IO.Path]::Combine($PSScriptRoot, 'Apply-P10.2AV-Repair2-AdminWebResidualFeatureAssetConsolidation.ps1'),
    [System.IO.Path]::Combine($PSScriptRoot, 'Test-P10.2AV-Repair2-AdminWebResidualFeatureAssetConsolidation.ps1')
)

foreach ($scriptFile in $scriptFiles) {
    Require-File -Path $scriptFile -Label 'AV Repair2 script'
    $text = [System.IO.File]::ReadAllText($scriptFile)
    if ($text.Contains('$Label:') -or $text.Contains('$Path:')) {
        throw ('Unsafe variable interpolation token found in {0}' -f $scriptFile)
    }
    if ($text.Contains('@(' + [Environment]::NewLine + '    @(')) {
        throw ('Unsafe nested array validation pattern found in {0}' -f $scriptFile)
    }
}

Write-Host 'P10.2AV Repair2 validation passed.'
