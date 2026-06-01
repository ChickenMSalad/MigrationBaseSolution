Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $start = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($start)) {
        $start = (Get-Location).Path
    }

    $current = [System.IO.DirectoryInfo]::new($start)
    while ($null -ne $current) {
        $srcPath = [System.IO.Path]::Combine($current.FullName, 'src')
        if (Test-Path -LiteralPath $srcPath -PathType Container) {
            return $current.FullName
        }
        $current = $current.Parent
    }

    throw 'Unable to locate repository root.'
}

$repoRoot = Get-RepoRoot
$sourceRoot = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$featuresRoot = [System.IO.Path]::Combine($sourceRoot, 'features')
$reportPath = [System.IO.Path]::Combine($repoRoot, 'artifacts', 'p10', 'P10.2AV', 'AdminWebResidualFeatureAssetConsolidation.md')

if (-not (Test-Path -LiteralPath $sourceRoot -PathType Container)) {
    throw ('Canonical Admin Web src root not found: {0}' -f $sourceRoot)
}
if (-not (Test-Path -LiteralPath $featuresRoot -PathType Container)) {
    throw ('Canonical Admin Web features root not found: {0}' -f $featuresRoot)
}
if (-not (Test-Path -LiteralPath $reportPath -PathType Leaf)) {
    throw ('Expected P10.2AV report was not found: {0}' -f $reportPath)
}

$featureFiles = @(Get-ChildItem -LiteralPath $featuresRoot -Recurse -File -Include *.ts,*.tsx)
foreach ($file in $featureFiles) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    if ($content -match "from\s+['\"]\.\./\.\./\.\./\.\./api/[^'\"]+['\"]") {
        throw ('Legacy deep flat API import remains in feature file: {0}' -f $file.FullName)
    }
    if ($content -match "from\s+['\"]\.\./\.\./\.\./\.\./types/[^'\"]+['\"]") {
        throw ('Legacy deep flat types import remains in feature file: {0}' -f $file.FullName)
    }
}

$scriptRoot = [System.IO.Path]::Combine($repoRoot, 'tools', 'p10', 'P10.2AV')
$scriptFiles = @(Get-ChildItem -LiteralPath $scriptRoot -File -Filter *.ps1)
foreach ($script in $scriptFiles) {
    $text = Get-Content -LiteralPath $script.FullName -Raw
    if ($text -match '\$[A-Za-z_][A-Za-z0-9_]*:') {
        throw ('Unsafe PowerShell variable-colon interpolation pattern found in {0}' -f $script.FullName)
    }
    if ($text -match '@\(\s*@\(') {
        throw ('Unsafe nested array pattern found in {0}' -f $script.FullName)
    }
    $bytes = [System.IO.File]::ReadAllBytes($script.FullName)
    foreach ($byte in $bytes) {
        if (($byte -lt 32) -and ($byte -ne 9) -and ($byte -ne 10) -and ($byte -ne 13)) {
            throw ('Unexpected control character found in {0}' -f $script.FullName)
        }
    }
}

Write-Host 'P10.2AV validation passed.'
