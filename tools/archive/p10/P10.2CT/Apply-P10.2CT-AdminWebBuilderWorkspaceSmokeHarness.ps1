Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$sourceRoot = Join-Path $adminWebRoot 'src'
$appPath = Join-Path $sourceRoot 'App.tsx'
$layoutPath = Join-Path $sourceRoot 'components\Layout.tsx'
$docsRoot = Join-Path $repoRoot 'docs\P10'
$reportPath = Join-Path $docsRoot 'P10.2CT-AdminWebBuilderWorkspaceSmokeHarness.md'
$artifactsRoot = Join-Path $repoRoot 'artifacts\p10\P10.2CT'

New-Item -ItemType Directory -Force -Path $docsRoot | Out-Null
New-Item -ItemType Directory -Force -Path $artifactsRoot | Out-Null

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2CT - Admin Web Builder Workspace Smoke Harness')
[void]$report.Add('')
[void]$report.Add(('Generated UTC: `{0}`' -f ([DateTime]::UtcNow.ToString('o'))))
[void]$report.Add('')
[void]$report.Add('## Purpose')
[void]$report.Add('')
[void]$report.Add('Validate that the restored Manifest, Taxonomy, and Mapping builder workspaces have local source files, route references, and navigation posture before runtime click-through testing.')
[void]$report.Add('')

if (-not (Test-Path $adminWebRoot)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}
if (-not (Test-Path $appPath)) {
    throw ('App.tsx was not found: {0}' -f $appPath)
}

$appText = Get-Content -Path $appPath -Raw
$layoutText = ''
if (Test-Path $layoutPath) {
    $layoutText = Get-Content -Path $layoutPath -Raw
}

$builders = @(
    [pscustomobject]@{ Name = 'Manifest Builder'; Route = '/manifest-builder'; Keywords = @('ManifestBuilder','manifest-builder','Manifest Builder','Manifest') },
    [pscustomobject]@{ Name = 'Taxonomy Builder'; Route = '/taxonomy-builder'; Keywords = @('TaxonomyBuilder','taxonomy-builder','Taxonomy Builder','Taxonomy') },
    [pscustomobject]@{ Name = 'Mapping Builder'; Route = '/mapping-builder'; Keywords = @('MappingBuilder','mapping-builder','Mapping Builder','Mapping') }
)

[void]$report.Add('## Builder Reachability Snapshot')
[void]$report.Add('')
[void]$report.Add('| Builder | Expected route | Candidate source files | App route/reference | Layout/navigation reference |')
[void]$report.Add('|---|---:|---:|---:|---:|')

foreach ($builder in $builders) {
    $candidateCount = 0
    $files = Get-ChildItem -Path $sourceRoot -Recurse -File -Include '*.tsx','*.ts' -ErrorAction SilentlyContinue
    foreach ($file in $files) {
        $fileName = $file.Name
        $fullName = $file.FullName
        $matched = $false
        foreach ($keyword in $builder.Keywords) {
            if ($fileName.IndexOf($keyword, [StringComparison]::OrdinalIgnoreCase) -ge 0 -or $fullName.IndexOf($keyword, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
                $matched = $true
            }
        }
        if ($matched) {
            $candidateCount++
        }
    }

    $appHasRoute = $false
    if ($appText.IndexOf($builder.Route, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
        $appHasRoute = $true
    }
    foreach ($keyword in $builder.Keywords) {
        if ($appText.IndexOf($keyword, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
            $appHasRoute = $true
        }
    }

    $layoutHasReference = $false
    if ($layoutText.Length -gt 0) {
        if ($layoutText.IndexOf($builder.Route, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
            $layoutHasReference = $true
        }
        foreach ($keyword in $builder.Keywords) {
            if ($layoutText.IndexOf($keyword, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
                $layoutHasReference = $true
            }
        }
    }

    [void]$report.Add(('| {0} | `{1}` | {2} | {3} | {4} |' -f $builder.Name, $builder.Route, $candidateCount, $appHasRoute, $layoutHasReference))
}

[void]$report.Add('')
[void]$report.Add('## Runtime Smoke')
[void]$report.Add('')
[void]$report.Add('After Admin Web is running locally, use:')
[void]$report.Add('')
[void]$report.Add('```powershell')
[void]$report.Add('powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.2CT\Run-P10.2CT-AdminWebBuilderWorkspaceSmoke.ps1 -AdminWebBaseUrl "http://localhost:5173"')
[void]$report.Add('```')
[void]$report.Add('')
[void]$report.Add('The runner checks whether the builder routes return an HTML response from the Vite dev server or preview server. It does not call backend mutation endpoints.')

Set-Content -Path $reportPath -Value $report.ToArray() -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2CT Admin Web builder workspace smoke harness applied.'
