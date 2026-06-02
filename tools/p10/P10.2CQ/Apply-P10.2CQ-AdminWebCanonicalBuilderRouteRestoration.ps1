Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$adminRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$srcRoot = Join-Path $adminRoot 'src'
$appPath = Join-Path $srcRoot 'App.tsx'
$featuresRoot = Join-Path $srcRoot 'features'
$referenceRoot = Join-Path $adminRoot 'reference\apps-migration-admin-ui\src'
$legacyAppsRoot = Join-Path $repoRoot 'apps\migration-admin-ui\src'
$docPath = Join-Path $repoRoot 'docs\P10\P10.2CQ-AdminWebCanonicalBuilderRouteRestoration.Report.md'

if (-not (Test-Path -LiteralPath $appPath)) { throw ('App.tsx not found: {0}' -f $appPath) }
if (-not (Test-Path -LiteralPath $featuresRoot)) { throw ('Canonical features root not found: {0}' -f $featuresRoot) }

$docDir = Split-Path -Parent $docPath
if (-not (Test-Path -LiteralPath $docDir)) { New-Item -ItemType Directory -Path $docDir -Force | Out-Null }

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2CQ - Admin Web Canonical Builder Route Restoration')
[void]$report.Add('')
[void]$report.Add(('Generated UTC: `{0}`' -f ([DateTime]::UtcNow.ToString('o'))))
[void]$report.Add('')

$builders = @(
    @{ Key = 'manifest'; Label = 'Manifest Builder'; Route = '/manifest-builder'; Names = @('ManifestBuilder','MigrationManifestBuilder') },
    @{ Key = 'taxonomy'; Label = 'Taxonomy Builder'; Route = '/taxonomy-builder'; Names = @('TaxonomyBuilder','MigrationTaxonomyBuilder') },
    @{ Key = 'mapping'; Label = 'Mapping Builder'; Route = '/mapping-builder'; Names = @('MappingBuilder','MigrationMappingBuilder') }
)

$app = Get-Content -LiteralPath $appPath -Raw
$originalApp = $app

[void]$report.Add('## Builder route restoration')
[void]$report.Add('')

foreach ($builder in $builders) {
    $label = [string]$builder.Label
    $route = [string]$builder.Route
    $names = @($builder.Names)
    [void]$report.Add(('### {0}' -f $label))

    $canonicalCandidates = New-Object 'System.Collections.Generic.List[object]'
    $allCanonicalPages = @(Get-ChildItem -LiteralPath $featuresRoot -Recurse -File -Filter '*.tsx' -ErrorAction SilentlyContinue)
    foreach ($page in $allCanonicalPages) {
        $baseName = [string]$page.BaseName
        $fullName = [string]$page.FullName
        $matched = $false
        foreach ($name in $names) {
            if ($baseName -ieq [string]$name) { $matched = $true }
        }
        if (-not $matched) {
            $lowerPath = $fullName.ToLowerInvariant()
            if ($lowerPath.Contains($builder.Key) -and $lowerPath.Contains('builder')) { $matched = $true }
        }
        if ($matched) { [void]$canonicalCandidates.Add($page) }
    }

    if ($canonicalCandidates.Count -eq 1) {
        $candidate = $canonicalCandidates[0]
        $candidatePath = [string]$candidate.FullName
        $componentName = [string]$candidate.BaseName
        $candidateContent = Get-Content -LiteralPath $candidatePath -Raw
        $importKind = ''
        if ($candidateContent.Contains(('export default function {0}' -f $componentName)) -or $candidateContent.Contains(('export default {0}' -f $componentName)) -or $candidateContent.Contains('export default function')) {
            $importKind = 'default'
        } elseif ($candidateContent.Contains(('export function {0}' -f $componentName)) -or $candidateContent.Contains(('export const {0}' -f $componentName)) -or $candidateContent.Contains(('export class {0}' -f $componentName))) {
            $importKind = 'named'
        }

        if ([string]::IsNullOrWhiteSpace($importKind)) {
            [void]$report.Add(('Canonical candidate found but export could not be identified, so no route was added: `{0}`' -f $candidatePath))
            [void]$report.Add('')
            continue
        }

        $relative = $candidatePath.Substring($srcRoot.Length).TrimStart('\','/')
        $relative = $relative.Replace('\\','/')
        if ($relative.EndsWith('.tsx')) { $relative = $relative.Substring(0, $relative.Length - 4) }
        $importSource = './' + $relative

        $hasRoute = $app.Contains(('path="{0}"' -f $route)) -or $app.Contains(("path='{0}'" -f $route))
        $hasComponentUsage = $app.Contains(('<{0}' -f $componentName))
        $hasImport = $app.Contains((' {0} ' -f $componentName)) -or $app.Contains(('{{ {0} }}' -f $componentName)) -or $app.Contains(('{0} from ' -f $componentName))

        if (-not $hasImport) {
            if ($importKind -eq 'default') {
                $importLine = ('import {0} from "{1}";' -f $componentName, $importSource)
            } else {
                $importLine = ('import {{ {0} }} from "{1}";' -f $componentName, $importSource)
            }
            $lines = New-Object 'System.Collections.Generic.List[string]'
            $existingLines = @($app -split "`r?`n")
            $inserted = $false
            $lastImportIndex = -1
            for ($i = 0; $i -lt $existingLines.Length; $i++) {
                if ([string]$existingLines[$i] -like 'import *') { $lastImportIndex = $i }
            }
            for ($i = 0; $i -lt $existingLines.Length; $i++) {
                [void]$lines.Add([string]$existingLines[$i])
                if ($i -eq $lastImportIndex) {
                    [void]$lines.Add($importLine)
                    $inserted = $true
                }
            }
            if (-not $inserted) {
                $lines.Insert(0, $importLine)
            }
            $app = [string]::Join("`r`n", $lines)
            [void]$report.Add(('Added import: `{0}`' -f $importLine))
        } else {
            [void]$report.Add(('Import already present or component already referenced: `{0}`' -f $componentName))
        }

        if (-not $hasRoute -and -not $hasComponentUsage) {
            $routeLine = ('        <Route path="{0}" element={{<{1} />}} />' -f $route, $componentName)
            $routeMarker = '</Routes>'
            if ($app.Contains($routeMarker)) {
                $app = $app.Replace($routeMarker, ($routeLine + "`r`n" + '      ' + $routeMarker))
                [void]$report.Add(('Added route: `{0}`' -f $routeLine.Trim()))
            } else {
                [void]$report.Add('Unable to find `</Routes>` marker, so no route was added.')
            }
        } else {
            [void]$report.Add(('Route or component usage already present for `{0}`.' -f $route))
        }

        [void]$report.Add(('Canonical candidate: `{0}`' -f $candidatePath))
        [void]$report.Add('')
    } elseif ($canonicalCandidates.Count -gt 1) {
        [void]$report.Add(('Ambiguous canonical candidates found: `{0}`. No route added.' -f $canonicalCandidates.Count))
        foreach ($candidate in $canonicalCandidates) { [void]$report.Add(('- `{0}`' -f $candidate.FullName)) }
        [void]$report.Add('')
    } else {
        [void]$report.Add('No canonical builder page candidate found. Reference/legacy candidates are listed below.')
        $candidateRoots = @($referenceRoot, $legacyAppsRoot)
        foreach ($candidateRoot in $candidateRoots) {
            if (-not (Test-Path -LiteralPath $candidateRoot)) { continue }
            $files = @(Get-ChildItem -LiteralPath $candidateRoot -Recurse -File -Filter '*.tsx' -ErrorAction SilentlyContinue)
            foreach ($file in $files) {
                $lowerPath = ([string]$file.FullName).ToLowerInvariant()
                if ($lowerPath.Contains($builder.Key) -and $lowerPath.Contains('builder')) {
                    [void]$report.Add(('- `{0}`' -f $file.FullName))
                }
            }
        }
        [void]$report.Add('')
    }
}

if ($app -ne $originalApp) {
    Set-Content -LiteralPath $appPath -Value $app -Encoding UTF8
    [void]$report.Add('## App.tsx')
    [void]$report.Add('')
    [void]$report.Add('Updated App.tsx with guarded canonical builder routes.')
} else {
    [void]$report.Add('## App.tsx')
    [void]$report.Add('')
    [void]$report.Add('No App.tsx changes were required.')
}

Set-Content -LiteralPath $docPath -Value $report -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $docPath)
Write-Host 'P10.2CQ Admin Web canonical builder route restoration applied.'
