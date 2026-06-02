Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$repoRoot = $repoRoot.ProviderPath

$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$sourceRoot = Join-Path $adminWebRoot 'src'
$featureRoot = Join-Path $sourceRoot 'features'
$referenceRoot = Join-Path $adminWebRoot 'reference'
$appTsx = Join-Path $sourceRoot 'App.tsx'
$layoutTsx = Join-Path $sourceRoot 'components\Layout.tsx'
$docsRoot = Join-Path $repoRoot 'docs\P10'
$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.2CO-Repair'
$reportPath = Join-Path $docsRoot 'P10.2CO-Repair-AdminWebBuilderReachabilityInventory.md'
$artifactReportPath = Join-Path $artifactRoot 'builder-reachability-inventory.md'

if (-not (Test-Path -LiteralPath $adminWebRoot -PathType Container)) { throw ('Admin Web root was not found: {0}' -f $adminWebRoot) }
if (-not (Test-Path -LiteralPath $sourceRoot -PathType Container)) { throw ('Admin Web source root was not found: {0}' -f $sourceRoot) }
if (-not (Test-Path -LiteralPath $featureRoot -PathType Container)) { throw ('Admin Web feature root was not found: {0}' -f $featureRoot) }

New-Item -ItemType Directory -Force -Path $docsRoot | Out-Null
New-Item -ItemType Directory -Force -Path $artifactRoot | Out-Null

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2CO Repair - Admin Web Builder Reachability Inventory')
[void]$report.Add('')
[void]$report.Add(('Generated UTC: `{0}`' -f ([DateTime]::UtcNow.ToString('o'))))
[void]$report.Add(('Admin Web root: `{0}`' -f $adminWebRoot))
[void]$report.Add('')
[void]$report.Add('## Purpose')
[void]$report.Add('')
[void]$report.Add('Inventory Manifest Builder, Taxonomy Builder, and Mapping Builder candidates without mutating source. This repair avoids regex match arrays and collection-return helpers.')
[void]$report.Add('')

$builderFamilies = @(
    @{ Name = 'Manifest Builder'; Tokens = @('manifestbuilder', 'manifest-builder', 'manifest builder', 'manifest') },
    @{ Name = 'Taxonomy Builder'; Tokens = @('taxonomybuilder', 'taxonomy-builder', 'taxonomy builder', 'taxonomy') },
    @{ Name = 'Mapping Builder'; Tokens = @('mappingbuilder', 'mapping-builder', 'mapping builder', 'mapping') }
)

$sourceFiles = @()
if (Test-Path -LiteralPath $sourceRoot -PathType Container) {
    $sourceFiles = @(Get-ChildItem -LiteralPath $sourceRoot -Recurse -File -Include *.ts,*.tsx,*.js,*.jsx -ErrorAction SilentlyContinue | Where-Object {
        $fullName = $_.FullName
        ($fullName -notlike '*\node_modules\*') -and ($fullName -notlike '*\dist\*') -and ($fullName -notlike '*\reference\*')
    })
}

$referenceFiles = @()
if (Test-Path -LiteralPath $referenceRoot -PathType Container) {
    $referenceFiles = @(Get-ChildItem -LiteralPath $referenceRoot -Recurse -File -Include *.ts,*.tsx,*.js,*.jsx -ErrorAction SilentlyContinue | Where-Object {
        $fullName = $_.FullName
        ($fullName -notlike '*\node_modules\*') -and ($fullName -notlike '*\dist\*')
    })
}

$appTextLower = ''
if (Test-Path -LiteralPath $appTsx -PathType Leaf) { $appTextLower = ([System.IO.File]::ReadAllText($appTsx)).ToLowerInvariant() }
$layoutTextLower = ''
if (Test-Path -LiteralPath $layoutTsx -PathType Leaf) { $layoutTextLower = ([System.IO.File]::ReadAllText($layoutTsx)).ToLowerInvariant() }

foreach ($family in $builderFamilies) {
    $name = [string]$family.Name
    $tokens = @($family.Tokens)
    [void]$report.Add(('## {0}' -f $name))
    [void]$report.Add('')

    $canonicalMatches = New-Object 'System.Collections.Generic.List[string]'
    foreach ($file in $sourceFiles) {
        $relative = $file.FullName.Substring($sourceRoot.Length).TrimStart('\')
        $relativeLower = $relative.ToLowerInvariant()
        $fileNameLower = $file.Name.ToLowerInvariant()
        $matched = $false
        foreach ($token in $tokens) {
            $tokenText = [string]$token
            if ($relativeLower.Contains($tokenText) -or $fileNameLower.Contains($tokenText)) { $matched = $true; break }
        }
        if ($matched) { [void]$canonicalMatches.Add($relative) }
    }

    $referenceMatches = New-Object 'System.Collections.Generic.List[string]'
    foreach ($file in $referenceFiles) {
        $relative = $file.FullName.Substring($referenceRoot.Length).TrimStart('\')
        $relativeLower = $relative.ToLowerInvariant()
        $fileNameLower = $file.Name.ToLowerInvariant()
        $matched = $false
        foreach ($token in $tokens) {
            $tokenText = [string]$token
            if ($relativeLower.Contains($tokenText) -or $fileNameLower.Contains($tokenText)) { $matched = $true; break }
        }
        if ($matched) { [void]$referenceMatches.Add($relative) }
    }

    $appMentions = New-Object 'System.Collections.Generic.List[string]'
    $layoutMentions = New-Object 'System.Collections.Generic.List[string]'
    foreach ($token in $tokens) {
        $tokenText = [string]$token
        if (-not [string]::IsNullOrWhiteSpace($appTextLower) -and $appTextLower.Contains($tokenText)) { [void]$appMentions.Add($tokenText) }
        if (-not [string]::IsNullOrWhiteSpace($layoutTextLower) -and $layoutTextLower.Contains($tokenText)) { [void]$layoutMentions.Add($tokenText) }
    }

    [void]$report.Add(('Canonical candidate count: `{0}`' -f $canonicalMatches.Count))
    if ($canonicalMatches.Count -gt 0) { foreach ($item in $canonicalMatches) { [void]$report.Add(('- `{0}`' -f $item)) } } else { [void]$report.Add('- No canonical compiled-source candidates found.') }
    [void]$report.Add('')
    [void]$report.Add(('Reference candidate count: `{0}`' -f $referenceMatches.Count))
    if ($referenceMatches.Count -gt 0) { foreach ($item in $referenceMatches) { [void]$report.Add(('- `{0}`' -f $item)) } } else { [void]$report.Add('- No reference candidates found.') }
    [void]$report.Add('')
    [void]$report.Add(('App.tsx token mentions: `{0}`' -f $appMentions.Count))
    if ($appMentions.Count -gt 0) { foreach ($item in $appMentions) { [void]$report.Add(('- `{0}`' -f $item)) } } else { [void]$report.Add('- No App.tsx builder token mentions found.') }
    [void]$report.Add('')
    [void]$report.Add(('Layout.tsx token mentions: `{0}`' -f $layoutMentions.Count))
    if ($layoutMentions.Count -gt 0) { foreach ($item in $layoutMentions) { [void]$report.Add(('- `{0}`' -f $item)) } } else { [void]$report.Add('- No Layout.tsx builder token mentions found.') }
    [void]$report.Add('')

    if (($canonicalMatches.Count -gt 0) -and ($appMentions.Count -eq 0)) {
        [void]$report.Add('Recommended next action: canonical candidates exist but no App.tsx mention was found. Review and restore route/navigation in the next targeted set.')
    } elseif (($canonicalMatches.Count -eq 0) -and ($referenceMatches.Count -gt 0)) {
        [void]$report.Add('Recommended next action: candidates appear reference-only. Promote the correct page/API/types into canonical feature folders before adding routes.')
    } elseif (($canonicalMatches.Count -gt 0) -and ($appMentions.Count -gt 0)) {
        [void]$report.Add('Recommended next action: canonical candidates and App.tsx mentions exist. Verify navigation and runtime behavior.')
    } else {
        [void]$report.Add('Recommended next action: no local candidates found. Confirm whether this feature should be sourced from backend/API pages or a future UI set.')
    }
    [void]$report.Add('')
}

[void]$report.Add('## Guardrails')
[void]$report.Add('')
[void]$report.Add('- Report-only repair.')
[void]$report.Add('- No source files are moved or rewritten.')
[void]$report.Add('- Avoids regex match arrays, helper collection parameters, helper source-line parameters, and tool-folder self scans.')

[System.IO.File]::WriteAllLines($reportPath, $report, [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllLines($artifactReportPath, $report, [System.Text.UTF8Encoding]::new($false))

Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host ('Wrote artifact report: {0}' -f $artifactReportPath)
Write-Host 'P10.2CO Repair Admin Web builder reachability inventory applied.'
