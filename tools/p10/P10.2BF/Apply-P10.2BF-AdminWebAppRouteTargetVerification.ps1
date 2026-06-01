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

$reportDir = Split-Path -Parent $reportPath
if (-not (Test-Path -Path $reportDir -PathType Container)) {
    New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
}

$content = Get-Content -Path $appPath -Raw
$lines = @(Get-Content -Path $appPath)
$report = New-Object System.Collections.Generic.List[string]
[void]$report.Add('# P10.2BF - Admin Web App Route Target Verification')
[void]$report.Add('')
[void]$report.Add(('Generated from local file: `{0}`' -f $appPath))
[void]$report.Add('')

$importPattern = 'import\s+(?:type\s+)?(?:\{(?<named>[^}]*)\}|(?<default>[A-Za-z_][A-Za-z0-9_]*))\s+from\s+[''\"](?<source>[^''\"]+)[''\"]\s*;'
$importMatches = @([regex]::Matches($content, $importPattern))
$imports = New-Object System.Collections.Generic.List[object]
$seenImportLines = New-Object 'System.Collections.Generic.HashSet[string]'
$duplicateImports = New-Object System.Collections.Generic.List[string]
$malformedImports = New-Object System.Collections.Generic.List[string]

foreach ($line in $lines) {
    $trimmed = $line.Trim()
    if ($trimmed.StartsWith('import ')) {
        if (-not $trimmed.EndsWith(';')) {
            [void]$malformedImports.Add($trimmed)
        }
        if (-not $seenImportLines.Add($trimmed)) {
            [void]$duplicateImports.Add($trimmed)
        }
    }
}

foreach ($match in $importMatches) {
    $source = [string]$match.Groups['source'].Value
    $symbols = New-Object System.Collections.Generic.List[string]
    $defaultSymbol = [string]$match.Groups['default'].Value
    if (-not [string]::IsNullOrWhiteSpace($defaultSymbol)) {
        [void]$symbols.Add($defaultSymbol.Trim())
    }
    $named = [string]$match.Groups['named'].Value
    if (-not [string]::IsNullOrWhiteSpace($named)) {
        $parts = @($named.Split(','))
        foreach ($part in $parts) {
            $candidate = $part.Trim()
            if ([string]::IsNullOrWhiteSpace($candidate)) { continue }
            if ($candidate -match '\s+as\s+') {
                $aliasParts = @($candidate -split '\s+as\s+')
                if ($aliasParts.Length -gt 1) {
                    $candidate = $aliasParts[$aliasParts.Length - 1].Trim()
                }
            }
            if (-not [string]::IsNullOrWhiteSpace($candidate)) {
                [void]$symbols.Add($candidate)
            }
        }
    }
    [void]$imports.Add([pscustomobject]@{
        Source = $source
        Symbols = @($symbols)
        Text = [string]$match.Value
    })
}

$missingImportTargets = New-Object System.Collections.Generic.List[string]
foreach ($import in $imports) {
    $source = [string]$import.Source
    if (-not $source.StartsWith('.')) { continue }
    $candidateBase = Join-Path $sourceRoot ($source.Replace('/', '\'))
    $candidatePaths = @(
        $candidateBase,
        ($candidateBase + '.ts'),
        ($candidateBase + '.tsx'),
        (Join-Path $candidateBase 'index.ts'),
        (Join-Path $candidateBase 'index.tsx')
    )
    $found = $false
    foreach ($candidatePath in $candidatePaths) {
        if (Test-Path -Path $candidatePath -PathType Leaf) {
            $found = $true
            break
        }
    }
    if (-not $found) {
        [void]$missingImportTargets.Add($source)
    }
}

$routePattern = '<Route\s+path=["''](?<path>[^"'']*)["'']\s+element=\{<(?<component>[A-Za-z_][A-Za-z0-9_]*)'
$routeMatches = @([regex]::Matches($content, $routePattern))
$routeComponents = New-Object 'System.Collections.Generic.HashSet[string]'
$routePaths = New-Object System.Collections.Generic.List[string]
$duplicateRoutePaths = New-Object System.Collections.Generic.List[string]
$seenRoutePaths = New-Object 'System.Collections.Generic.HashSet[string]'

foreach ($routeMatch in $routeMatches) {
    $routePath = [string]$routeMatch.Groups['path'].Value
    $component = [string]$routeMatch.Groups['component'].Value
    [void]$routeComponents.Add($component)
    [void]$routePaths.Add($routePath)
    if (-not $seenRoutePaths.Add($routePath)) {
        [void]$duplicateRoutePaths.Add($routePath)
    }
}

$importedSymbols = New-Object 'System.Collections.Generic.HashSet[string]'
foreach ($import in $imports) {
    foreach ($symbol in @($import.Symbols)) {
        if (-not [string]::IsNullOrWhiteSpace([string]$symbol)) {
            [void]$importedSymbols.Add([string]$symbol)
        }
    }
}

$missingRouteImports = New-Object System.Collections.Generic.List[string]
foreach ($component in $routeComponents) {
    if (-not $importedSymbols.Contains($component)) {
        [void]$missingRouteImports.Add($component)
    }
}

$unusedImports = New-Object System.Collections.Generic.List[string]
foreach ($symbol in $importedSymbols) {
    $usageMatches = @([regex]::Matches($content, ('\b' + [regex]::Escape($symbol) + '\b')))
    if ($usageMatches.Length -le 1) {
        [void]$unusedImports.Add($symbol)
    }
}

[void]$report.Add('## Summary')
[void]$report.Add('')
[void]$report.Add(('- Import declarations parsed: {0}' -f $imports.Count))
[void]$report.Add(('- Route declarations parsed: {0}' -f $routeMatches.Length))
[void]$report.Add(('- Duplicate import lines: {0}' -f $duplicateImports.Count))
[void]$report.Add(('- Malformed import lines: {0}' -f $malformedImports.Count))
[void]$report.Add(('- Missing local import targets: {0}' -f $missingImportTargets.Count))
[void]$report.Add(('- Route components missing imports: {0}' -f $missingRouteImports.Count))
[void]$report.Add(('- Duplicate route paths: {0}' -f $duplicateRoutePaths.Count))
[void]$report.Add(('- Potential unused imports: {0}' -f $unusedImports.Count))
[void]$report.Add('')

[void]$report.Add('## Missing Local Import Targets')
[void]$report.Add('')
if ($missingImportTargets.Count -eq 0) { [void]$report.Add('- None') } else { foreach ($item in $missingImportTargets) { [void]$report.Add(('- `{0}`' -f $item)) } }
[void]$report.Add('')

[void]$report.Add('## Route Components Missing Imports')
[void]$report.Add('')
if ($missingRouteImports.Count -eq 0) { [void]$report.Add('- None') } else { foreach ($item in $missingRouteImports) { [void]$report.Add(('- `{0}`' -f $item)) } }
[void]$report.Add('')

[void]$report.Add('## Duplicate Route Paths')
[void]$report.Add('')
if ($duplicateRoutePaths.Count -eq 0) { [void]$report.Add('- None') } else { foreach ($item in $duplicateRoutePaths) { [void]$report.Add(('- `{0}`' -f $item)) } }
[void]$report.Add('')

[void]$report.Add('## Duplicate Import Lines')
[void]$report.Add('')
if ($duplicateImports.Count -eq 0) { [void]$report.Add('- None') } else { foreach ($item in $duplicateImports) { [void]$report.Add(('- `{0}`' -f $item)) } }
[void]$report.Add('')

[void]$report.Add('## Malformed Import Lines')
[void]$report.Add('')
if ($malformedImports.Count -eq 0) { [void]$report.Add('- None') } else { foreach ($item in $malformedImports) { [void]$report.Add(('- `{0}`' -f $item)) } }
[void]$report.Add('')

[void]$report.Add('## Potential Unused Imports')
[void]$report.Add('')
if ($unusedImports.Count -eq 0) { [void]$report.Add('- None') } else { foreach ($item in $unusedImports) { [void]$report.Add(('- `{0}`' -f $item)) } }
[void]$report.Add('')

[System.IO.File]::WriteAllLines($reportPath, [string[]]$report)
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2BF Admin Web App route target verification applied.'
