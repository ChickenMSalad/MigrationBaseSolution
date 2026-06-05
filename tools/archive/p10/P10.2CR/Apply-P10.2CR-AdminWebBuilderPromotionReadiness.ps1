
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $current = (Resolve-Path -LiteralPath $PSScriptRoot).Path
    while ($true) {
        if (Test-Path -LiteralPath (Join-Path $current '.git')) { return $current }
        $parent = Split-Path -Parent $current
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $current) {
            throw 'Unable to locate repository root from script path.'
        }
        $current = $parent
    }
}

function Add-Text {
    param(
        [Parameter(Mandatory=$true)][System.Collections.Generic.List[string]]$Target,
        [AllowEmptyString()][string]$Value
    )
    [void]$Target.Add($Value)
}

$repoRoot = Get-RepoRoot
$adminRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$sourceRoot = Join-Path $adminRoot 'src'
$canonicalFeaturesRoot = Join-Path $sourceRoot 'features'
$referenceSourceRoot = Join-Path $adminRoot 'reference\apps-migration-admin-ui\src'
$legacyAppsSourceRoot = Join-Path $repoRoot 'apps\migration-admin-ui\src'
$appFile = Join-Path $sourceRoot 'App.tsx'
$layoutFile = Join-Path $sourceRoot 'components\Layout.tsx'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2CR-AdminWebBuilderPromotionReadiness.Report.md'

if (-not (Test-Path -LiteralPath $adminRoot)) { throw ('Admin Web root was not found: {0}' -f $adminRoot) }
if (-not (Test-Path -LiteralPath $sourceRoot)) { throw ('Admin Web src root was not found: {0}' -f $sourceRoot) }
if (-not (Test-Path -LiteralPath $appFile)) { throw ('App.tsx was not found: {0}' -f $appFile) }

$report = New-Object 'System.Collections.Generic.List[string]'
Add-Text -Target $report -Value '# P10.2CR - Admin Web Builder Promotion Readiness'
Add-Text -Target $report -Value ''
Add-Text -Target $report -Value ('Generated UTC: `{0}`' -f ([DateTime]::UtcNow.ToString('o')))
Add-Text -Target $report -Value ('Admin Web root: `{0}`' -f $adminRoot)
Add-Text -Target $report -Value ''
Add-Text -Target $report -Value '## Intent'
Add-Text -Target $report -Value ''
Add-Text -Target $report -Value 'This report checks whether Manifest Builder, Taxonomy Builder, and Mapping Builder have canonical Admin Web source and route/navigation reachability. It does not promote or rewrite source files.'
Add-Text -Target $report -Value ''

$appText = [System.IO.File]::ReadAllText($appFile)
$layoutText = ''
if (Test-Path -LiteralPath $layoutFile) { $layoutText = [System.IO.File]::ReadAllText($layoutFile) }

$builderRows = @(
    @{ Key='manifest'; Display='Manifest Builder'; Route='/manifest-builder'; FileTerms=@('manifest','builder') },
    @{ Key='taxonomy'; Display='Taxonomy Builder'; Route='/taxonomy-builder'; FileTerms=@('taxonomy','builder') },
    @{ Key='mapping'; Display='Mapping Builder'; Route='/mapping-builder'; FileTerms=@('mapping','builder') }
)

Add-Text -Target $report -Value '## Summary'
Add-Text -Target $report -Value ''
Add-Text -Target $report -Value '| Builder | Canonical candidates | Reference candidates | Legacy apps candidates | Route present | Navigation mention | Status |'
Add-Text -Target $report -Value '|---|---:|---:|---:|---|---|---|'

$detailSections = New-Object 'System.Collections.Generic.List[string]'

foreach ($builder in $builderRows) {
    $terms = @($builder.FileTerms)
    $canonicalCandidates = New-Object 'System.Collections.Generic.List[string]'
    $referenceCandidates = New-Object 'System.Collections.Generic.List[string]'
    $legacyCandidates = New-Object 'System.Collections.Generic.List[string]'

    $searchRoots = @(
        @{ Name='canonical'; Path=$sourceRoot; Target=$canonicalCandidates },
        @{ Name='reference'; Path=$referenceSourceRoot; Target=$referenceCandidates },
        @{ Name='legacy'; Path=$legacyAppsSourceRoot; Target=$legacyCandidates }
    )

    foreach ($searchRoot in $searchRoots) {
        $path = [string]$searchRoot.Path
        if (-not (Test-Path -LiteralPath $path)) { continue }
        $files = @(Get-ChildItem -LiteralPath $path -Recurse -File -Include '*.tsx','*.ts' -ErrorAction SilentlyContinue)
        foreach ($file in $files) {
            $relative = $file.FullName.Substring($path.Length).TrimStart('\','/')
            $relativeLower = $relative.ToLowerInvariant()
            $nameLower = $file.Name.ToLowerInvariant()
            $isMatch = $true
            foreach ($term in $terms) {
                $termText = [string]$term
                if (-not ($relativeLower.Contains($termText.ToLowerInvariant()) -or $nameLower.Contains($termText.ToLowerInvariant()))) {
                    $isMatch = $false
                    break
                }
            }
            if ($isMatch) { [void]$searchRoot.Target.Add($relative) }
        }
    }

    $routePresent = $appText.Contains([string]$builder.Route)
    $navMention = $false
    if (-not [string]::IsNullOrEmpty($layoutText)) {
        $navMention = ($layoutText.ToLowerInvariant().Contains(([string]$builder.Display).ToLowerInvariant()) -or $layoutText.ToLowerInvariant().Contains(([string]$builder.Route).ToLowerInvariant()))
    }

    $status = 'Needs review'
    if ($canonicalCandidates.Count -gt 0 -and $routePresent -and $navMention) { $status = 'Canonical and reachable' }
    elseif ($canonicalCandidates.Count -gt 0 -and $routePresent) { $status = 'Canonical route present; navigation review needed' }
    elseif ($canonicalCandidates.Count -gt 0) { $status = 'Canonical candidate present; route/navigation missing' }
    elseif ($referenceCandidates.Count -gt 0 -or $legacyCandidates.Count -gt 0) { $status = 'Reference-only candidate; promotion needed' }
    else { $status = 'No builder candidate found' }

    Add-Text -Target $report -Value ('| {0} | {1} | {2} | {3} | {4} | {5} | {6} |' -f $builder.Display, $canonicalCandidates.Count, $referenceCandidates.Count, $legacyCandidates.Count, $routePresent, $navMention, $status)

    Add-Text -Target $detailSections -Value ('## {0}' -f $builder.Display)
    Add-Text -Target $detailSections -Value ''
    Add-Text -Target $detailSections -Value ('Route expected: `{0}`' -f $builder.Route)
    Add-Text -Target $detailSections -Value ('Route present in App.tsx: `{0}`' -f $routePresent)
    Add-Text -Target $detailSections -Value ('Navigation mention in Layout.tsx: `{0}`' -f $navMention)
    Add-Text -Target $detailSections -Value ''
    Add-Text -Target $detailSections -Value '### Canonical candidates'
    if ($canonicalCandidates.Count -eq 0) { Add-Text -Target $detailSections -Value '- none found' } else { foreach ($item in $canonicalCandidates) { Add-Text -Target $detailSections -Value ('- `{0}`' -f $item) } }
    Add-Text -Target $detailSections -Value ''
    Add-Text -Target $detailSections -Value '### Reference candidates'
    if ($referenceCandidates.Count -eq 0) { Add-Text -Target $detailSections -Value '- none found' } else { foreach ($item in $referenceCandidates) { Add-Text -Target $detailSections -Value ('- `{0}`' -f $item) } }
    Add-Text -Target $detailSections -Value ''
    Add-Text -Target $detailSections -Value '### Legacy apps candidates'
    if ($legacyCandidates.Count -eq 0) { Add-Text -Target $detailSections -Value '- none found' } else { foreach ($item in $legacyCandidates) { Add-Text -Target $detailSections -Value ('- `{0}`' -f $item) } }
    Add-Text -Target $detailSections -Value ''
    Add-Text -Target $detailSections -Value '### Recommended next action'
    if ($status -eq 'Reference-only candidate; promotion needed') {
        Add-Text -Target $detailSections -Value '- Promote only after selecting the exact source folder from the candidate list and normalizing imports/API dependencies in a build-gated set.'
    } elseif ($status -eq 'Canonical candidate present; route/navigation missing') {
        Add-Text -Target $detailSections -Value '- Add route/navigation reachability in a targeted build-gated set.'
    } elseif ($status -eq 'Canonical route present; navigation review needed') {
        Add-Text -Target $detailSections -Value '- Add navigation entry only if the page is intended for user-facing access.'
    } else {
        Add-Text -Target $detailSections -Value ('- {0}' -f $status)
    }
    Add-Text -Target $detailSections -Value ''
}

Add-Text -Target $report -Value ''
foreach ($line in $detailSections) { Add-Text -Target $report -Value $line }

$reportDirectory = Split-Path -Parent $reportPath
if (-not (Test-Path -LiteralPath $reportDirectory)) { New-Item -ItemType Directory -Path $reportDirectory -Force | Out-Null }
[System.IO.File]::WriteAllLines($reportPath, $report, [System.Text.Encoding]::UTF8)
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2CR Admin Web builder promotion readiness applied.'
