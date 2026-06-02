Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $scriptRoot = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
        $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    }

    $candidate = Resolve-Path -Path (Join-Path $scriptRoot '..\..\..')
    return $candidate.Path
}

$repoRoot = Get-RepoRoot
$adminSrc = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web\src'
$apiDir = Join-Path $adminSrc 'api'
$typesDir = Join-Path $adminSrc 'types'
$featuresDir = Join-Path $adminSrc 'features'
$docsDir = Join-Path $repoRoot 'docs\P10'

$requiredDirectories = @($adminSrc, $apiDir, $typesDir, $featuresDir)
foreach ($directory in $requiredDirectories) {
    if (-not (Test-Path -Path $directory -PathType Container)) {
        throw ('Required directory was not found: {0}' -f $directory)
    }
}

if (-not (Test-Path -Path $docsDir -PathType Container)) {
    New-Item -Path $docsDir -ItemType Directory -Force | Out-Null
}

$apiReadme = Join-Path $apiDir 'README.md'
$typesReadme = Join-Path $typesDir 'README.md'
$reportPath = Join-Path $docsDir 'P10.2BR-AdminWebSharedCoreBoundaryDocumentation.Report.md'

$apiReadmeLines = New-Object 'System.Collections.Generic.List[string]'
[void]$apiReadmeLines.Add('# Admin Web Shared API Surface')
[void]$apiReadmeLines.Add('')
[void]$apiReadmeLines.Add('This folder contains shared/core Admin Web API clients, contracts, readiness surfaces, storage helpers, queue/runtime helpers, and cross-feature utilities.')
[void]$apiReadmeLines.Add('')
[void]$apiReadmeLines.Add('Feature-specific API clients should live with their feature under `src/features/<domain>/<feature>/api`.')
[void]$apiReadmeLines.Add('')
[void]$apiReadmeLines.Add('Do not add new feature-only APIs here unless they are intentionally shared by multiple feature areas.')
[void]$apiReadmeLines.Add('')
[void]$apiReadmeLines.Add('The `api/core` folder remains the shared lower-level client boundary.')
Set-Content -Path $apiReadme -Value $apiReadmeLines.ToArray() -Encoding UTF8

$typesReadmeLines = New-Object 'System.Collections.Generic.List[string]'
[void]$typesReadmeLines.Add('# Admin Web Shared Types')
[void]$typesReadmeLines.Add('')
[void]$typesReadmeLines.Add('This folder contains shared Admin Web type surfaces that are not owned by a single feature.')
[void]$typesReadmeLines.Add('')
[void]$typesReadmeLines.Add('Feature-specific types should live with their feature under `src/features/<domain>/<feature>/types`.')
[void]$typesReadmeLines.Add('')
[void]$typesReadmeLines.Add('Do not add new feature-only types here unless they are intentionally shared by multiple feature areas.')
Set-Content -Path $typesReadme -Value $typesReadmeLines.ToArray() -Encoding UTF8

$apiFiles = @(Get-ChildItem -Path $apiDir -File -Filter '*.ts' -Recurse | Where-Object {
    $_.FullName -notmatch '[\\/]node_modules[\\/]'
})
$typeFiles = @(Get-ChildItem -Path $typesDir -File -Filter '*.ts' -Recurse | Where-Object {
    $_.FullName -notmatch '[\\/]node_modules[\\/]'
})
$featureRoots = @(Get-ChildItem -Path $featuresDir -Directory | Sort-Object -Property Name)

$reportLines = New-Object 'System.Collections.Generic.List[string]'
[void]$reportLines.Add('# P10.2BR - Admin Web Shared Core Boundary Documentation Report')
[void]$reportLines.Add('')
[void]$reportLines.Add('## Summary')
[void]$reportLines.Add('')
[void]$reportLines.Add(('- Shared API TypeScript files: {0}' -f $apiFiles.Length))
[void]$reportLines.Add(('- Shared type TypeScript files: {0}' -f $typeFiles.Length))
[void]$reportLines.Add(('- Canonical feature group roots: {0}' -f $featureRoots.Length))
[void]$reportLines.Add('')
[void]$reportLines.Add('## Canonical Feature Group Roots')
[void]$reportLines.Add('')
foreach ($featureRoot in $featureRoots) {
    [void]$reportLines.Add(('- `{0}`' -f $featureRoot.Name))
}
[void]$reportLines.Add('')
[void]$reportLines.Add('## Boundary')
[void]$reportLines.Add('')
[void]$reportLines.Add('The remaining flat `src/api` and `src/types` folders are documented as shared/core boundaries. Feature-only additions should be placed under `src/features/<domain>/<feature>`.')
[void]$reportLines.Add('')
[void]$reportLines.Add('## Files Written')
[void]$reportLines.Add('')
[void]$reportLines.Add('- `src/Admin/Migration.Admin.Web/src/api/README.md`')
[void]$reportLines.Add('- `src/Admin/Migration.Admin.Web/src/types/README.md`')
Set-Content -Path $reportPath -Value $reportLines.ToArray() -Encoding UTF8

Write-Host ('Wrote API boundary README: {0}' -f $apiReadme)
Write-Host ('Wrote types boundary README: {0}' -f $typesReadme)
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2BR Admin Web shared core boundary documentation applied.'
