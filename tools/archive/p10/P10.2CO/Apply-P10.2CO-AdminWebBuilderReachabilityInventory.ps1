Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$adminRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$srcRoot = Join-Path $adminRoot 'src'
$appPath = Join-Path $srcRoot 'App.tsx'
$layoutPath = Join-Path $srcRoot 'components\Layout.tsx'
$referenceRoot = Join-Path $adminRoot 'reference\apps-migration-admin-ui\src'
$docsDir = Join-Path $repoRoot 'docs\P10'
$artifactDir = Join-Path $repoRoot 'artifacts\p10\P10.2CO'

if (-not (Test-Path -LiteralPath $adminRoot)) { throw ('Admin Web root not found: {0}' -f $adminRoot) }
if (-not (Test-Path -LiteralPath $srcRoot)) { throw ('Admin Web src root not found: {0}' -f $srcRoot) }
if (-not (Test-Path -LiteralPath $appPath)) { throw ('App.tsx not found: {0}' -f $appPath) }

New-Item -ItemType Directory -Force -Path $docsDir | Out-Null
New-Item -ItemType Directory -Force -Path $artifactDir | Out-Null

$report = New-Object System.Collections.Generic.List[string]
[void]$report.Add('# P10.2CO - Admin Web Builder Reachability Inventory')
[void]$report.Add('')
[void]$report.Add(('Generated UTC: `{0}`' -f ([DateTime]::UtcNow.ToString('o'))))
[void]$report.Add('')
[void]$report.Add(('Admin Web root: `{0}`' -f $adminRoot))
[void]$report.Add('')

$builders = @(
    @{ Key = 'ManifestBuilder'; Label = 'Manifest Builder'; Terms = @('manifest'); Route = '/manifest-builder'; Component = 'ManifestBuilder' },
    @{ Key = 'TaxonomyBuilder'; Label = 'Taxonomy Builder'; Terms = @('taxonomy'); Route = '/taxonomy-builder'; Component = 'TaxonomyBuilder' },
    @{ Key = 'MappingBuilder'; Label = 'Mapping Builder'; Terms = @('mapping'); Route = '/mapping-builder'; Component = 'MappingBuilder' }
)

$canonicalSearchRoots = New-Object System.Collections.Generic.List[string]
$featuresRoot = Join-Path $srcRoot 'features'
$pagesRoot = Join-Path $srcRoot 'pages'
if (Test-Path -LiteralPath $featuresRoot) { [void]$canonicalSearchRoots.Add($featuresRoot) }
if (Test-Path -LiteralPath $pagesRoot) { [void]$canonicalSearchRoots.Add($pagesRoot) }

$referenceSearchRoots = New-Object System.Collections.Generic.List[string]
$referenceFeatures = Join-Path $referenceRoot 'features'
$referencePages = Join-Path $referenceRoot 'pages'
if (Test-Path -LiteralPath $referenceFeatures) { [void]$referenceSearchRoots.Add($referenceFeatures) }
if (Test-Path -LiteralPath $referencePages) { [void]$referenceSearchRoots.Add($referencePages) }

function Find-BuilderCandidatesInRoots {
    param(
        [string[]]$Roots,
        [string[]]$Terms
    )

    $matches = New-Object System.Collections.Generic.List[object]
    foreach ($root in $Roots) {
        if (-not (Test-Path -LiteralPath $root)) { continue }
        $files = @(Get-ChildItem -LiteralPath $root -Recurse -File -Include '*.tsx' -ErrorAction SilentlyContinue)
        foreach ($file in $files) {
            $name = $file.Name.ToLowerInvariant()
            $full = $file.FullName.ToLowerInvariant()
            $matchedTerm = $false
            foreach ($term in $Terms) {
                if ($name.Contains($term.ToLowerInvariant()) -or $full.Contains($term.ToLowerInvariant())) {
                    $matchedTerm = $true
                    break
                }
            }
            if (-not $matchedTerm) { continue }
            if ($name.Contains('builder') -or $full.Contains('builder') -or $name.Contains('build') -or $full.Contains('build')) {
                [void]$matches.Add($file)
            }
        }
    }
    return @($matches)
}

function Get-RelativeImportPath {
    param(
        [string]$FromFile,
        [string]$ToFile
    )

    $fromDir = Split-Path -Parent $FromFile
    $fromUri = New-Object System.Uri(($fromDir.TrimEnd('\') + '\'))
    $toUri = New-Object System.Uri($ToFile)
    $relative = $fromUri.MakeRelativeUri($toUri).ToString()
    $relative = [System.Uri]::UnescapeDataString($relative)
    $relative = $relative -replace '/', '/'
    $relative = $relative -replace '\.tsx$', ''
    $relative = $relative -replace '\.ts$', ''
    if (-not $relative.StartsWith('.')) { $relative = './' + $relative }
    return $relative
}

function Ensure-AppRoute {
    param(
        [string]$AppPath,
        [string]$Component,
        [string]$RoutePath,
        [string]$PagePath,
        [System.Collections.Generic.List[string]]$Report
    )

    $content = Get-Content -LiteralPath $AppPath -Raw
    $importPath = Get-RelativeImportPath -FromFile $AppPath -ToFile $PagePath
    $importLine = ('import {{ {0} }} from "{1}";' -f $Component, $importPath)

    if ($content -notmatch ('\b' + [regex]::Escape($Component) + '\b')) {
        $lines = New-Object System.Collections.Generic.List[string]
        $originalLines = @(Get-Content -LiteralPath $AppPath)
        $inserted = $false
        foreach ($line in $originalLines) {
            if (-not $inserted -and $line -notmatch '^\s*import\s+') {
                [void]$lines.Add($importLine)
                $inserted = $true
            }
            [void]$lines.Add($line)
        }
        if (-not $inserted) { [void]$lines.Add($importLine) }
        Set-Content -LiteralPath $AppPath -Value $lines -Encoding UTF8
        $content = Get-Content -LiteralPath $AppPath -Raw
        [void]$Report.Add(('- Added App.tsx import for {0}: `{1}`' -f $Component, $importPath))
    }
    elseif ($content -notlike ('*' + $importPath + '*')) {
        [void]$Report.Add(('- App.tsx already references {0}; import path was left unchanged for manual review.' -f $Component))
    }
    else {
        [void]$Report.Add(('- App.tsx already imports {0}.' -f $Component))
    }

    $content = Get-Content -LiteralPath $AppPath -Raw
    if ($content.Contains(('path="{0}"' -f $RoutePath)) -or $content.Contains(("path='{0}'" -f $RoutePath))) {
        [void]$Report.Add(('- Route already exists: `{0}`' -f $RoutePath))
        return
    }

    $routeLine = ('          <Route path="{0}" element={{<{1} />}} />' -f $RoutePath, $Component)
    $lines2 = New-Object System.Collections.Generic.List[string]
    $appLines = @(Get-Content -LiteralPath $AppPath)
    $routeInserted = $false
    foreach ($line in $appLines) {
        if (-not $routeInserted -and $line -match '^\s*</Routes>') {
            [void]$lines2.Add($routeLine)
            $routeInserted = $true
        }
        [void]$lines2.Add($line)
    }
    if (-not $routeInserted) {
        throw ('Could not locate </Routes> in App.tsx while adding route {0}' -f $RoutePath)
    }
    Set-Content -LiteralPath $AppPath -Value $lines2 -Encoding UTF8
    [void]$Report.Add(('- Added App.tsx route: `{0}` -> `{1}`' -f $RoutePath, $Component))
}

function Ensure-LayoutNavEntry {
    param(
        [string]$LayoutPath,
        [string]$Label,
        [string]$RoutePath,
        [System.Collections.Generic.List[string]]$Report
    )

    if (-not (Test-Path -LiteralPath $LayoutPath)) {
        [void]$Report.Add(('- Layout not found; navigation entry was not updated for `{0}`.' -f $Label))
        return
    }

    $content = Get-Content -LiteralPath $LayoutPath -Raw
    if ($content.Contains($RoutePath)) {
        [void]$Report.Add(('- Navigation already references `{0}`.' -f $RoutePath))
        return
    }

    $navLine = ('    {{ label: "{0}", to: "{1}" }},' -f $Label, $RoutePath)
    $lines = New-Object System.Collections.Generic.List[string]
    $layoutLines = @(Get-Content -LiteralPath $LayoutPath)
    $inserted = $false
    foreach ($line in $layoutLines) {
        if (-not $inserted -and $line -match '^\s*\];\s*$') {
            [void]$lines.Add($navLine)
            $inserted = $true
        }
        [void]$lines.Add($line)
    }
    if ($inserted) {
        Set-Content -LiteralPath $LayoutPath -Value $lines -Encoding UTF8
        [void]$Report.Add(('- Added navigation entry: `{0}` -> `{1}`' -f $Label, $RoutePath))
    }
    else {
        [void]$Report.Add(('- Could not locate nav array terminator in Layout.tsx; navigation entry was not added for `{0}`.' -f $Label))
    }
}

[void]$report.Add('## Builder Findings')
[void]$report.Add('')

foreach ($builder in $builders) {
    [void]$report.Add(('### {0}' -f $builder.Label))
    [void]$report.Add('')

    $canonicalMatches = @(Find-BuilderCandidatesInRoots -Roots @($canonicalSearchRoots) -Terms @($builder.Terms))
    $referenceMatches = @(Find-BuilderCandidatesInRoots -Roots @($referenceSearchRoots) -Terms @($builder.Terms))

    [void]$report.Add(('Canonical candidate count: `{0}`' -f $canonicalMatches.Length))
    foreach ($match in $canonicalMatches) { [void]$report.Add(('- `{0}`' -f $match.FullName)) }
    [void]$report.Add(('Reference candidate count: `{0}`' -f $referenceMatches.Length))
    foreach ($match in $referenceMatches) { [void]$report.Add(('- `{0}`' -f $match.FullName)) }

    if ($canonicalMatches.Length -gt 0) {
        $selected = $canonicalMatches[0]
        Ensure-AppRoute -AppPath $appPath -Component $builder.Component -RoutePath $builder.Route -PagePath $selected.FullName -Report $report
        Ensure-LayoutNavEntry -LayoutPath $layoutPath -Label $builder.Label -RoutePath $builder.Route -Report $report
    }
    elseif ($referenceMatches.Length -gt 0) {
        [void]$report.Add(('- Restoration required: {0} exists only in reference/source material. No compile-scope copy was performed in this set.' -f $builder.Label))
    }
    else {
        [void]$report.Add(('- No local candidates found for {0}.' -f $builder.Label))
    }

    [void]$report.Add('')
}

[void]$report.Add('## Notes')
[void]$report.Add('')
[void]$report.Add('- This set intentionally avoids copying reference pages into compiled Admin Web source because builder pages may require feature-specific API and component adaptation.')
[void]$report.Add('- If any builder is reference-only, the next set should restore that builder as a compile-safe feature page with route and navigation wiring.')

$reportPath = Join-Path $docsDir 'P10.2CO-AdminWebBuilderReachabilityInventory.md'
Set-Content -LiteralPath $reportPath -Value $report -Encoding UTF8
Copy-Item -LiteralPath $reportPath -Destination (Join-Path $artifactDir 'P10.2CO-AdminWebBuilderReachabilityInventory.md') -Force

Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2CO Admin Web builder reachability inventory applied.'
