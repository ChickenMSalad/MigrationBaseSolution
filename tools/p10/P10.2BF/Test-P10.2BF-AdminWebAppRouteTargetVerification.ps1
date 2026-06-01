Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$webRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$sourceRoot = Join-Path $webRoot 'src'
$appPath = Join-Path $sourceRoot 'App.tsx'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2BF-AdminWebAppRouteTargetVerification.Report.md'

if (-not (Test-Path -Path $appPath -PathType Leaf)) {
    throw ('App.tsx was not found: {0}' -f $appPath)
}

if (-not (Test-Path -Path $reportPath -PathType Leaf)) {
    throw ('Expected report was not found: {0}' -f $reportPath)
}

$content = Get-Content -Path $appPath -Raw
if ([string]::IsNullOrWhiteSpace($content)) {
    throw 'App.tsx is empty.'
}

$reportContent = Get-Content -Path $reportPath -Raw
if ($reportContent -notmatch 'P10\.2BF - Admin Web App Route Target Verification') {
    throw ('Report does not contain expected title: {0}' -f $reportPath)
}

$importPattern = 'import\s+(?:type\s+)?(?:\{(?<named>[^}]*)\}|(?<default>[A-Za-z_][A-Za-z0-9_]*))\s+from\s+[''\"](?<source>[^''\"]+)[''\"]\s*;'
$importMatches = @([regex]::Matches($content, $importPattern))
if ($importMatches.Length -eq 0) {
    throw 'No import declarations were parsed from App.tsx.'
}

$routePattern = '<Route\s+path=["''](?<path>[^"'']*)["'']\s+element=\{<(?<component>[A-Za-z_][A-Za-z0-9_]*)'
$routeMatches = @([regex]::Matches($content, $routePattern))
if ($routeMatches.Length -eq 0) {
    throw 'No route declarations were parsed from App.tsx.'
}

foreach ($match in $importMatches) {
    $source = [string]$match.Groups['source'].Value
    if ([string]::IsNullOrWhiteSpace($source)) {
        throw 'Parsed an import declaration with an empty source.'
    }
    if ($source.Contains('""')) {
        throw ('Import source contains double quote corruption: {0}' -f $source)
    }
}

$scriptFiles = @(Get-ChildItem -Path (Join-Path $repoRoot 'tools\p10') -Filter '*.ps1' -Recurse -File | Where-Object { $_.FullName -notlike ('*' + [System.IO.Path]::DirectorySeparatorChar + 'P10.2BF' + [System.IO.Path]::DirectorySeparatorChar + '*') })
foreach ($scriptFile in $scriptFiles) {
    $scriptText = Get-Content -Path $scriptFile.FullName -Raw
    if ($scriptText -match '\$[A-Za-z_][A-Za-z0-9_]*:') {
        throw ('Unsafe variable interpolation token found in {0}' -f $scriptFile.FullName)
    }
}

Write-Host 'P10.2BF Admin Web App route target verification test passed.'
