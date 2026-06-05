Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Find-RepoRoot {
    param([string]$StartPath)

    $current = Resolve-Path -Path $StartPath
    while ($null -ne $current) {
        $candidate = Join-Path -Path $current.Path -ChildPath 'MigrationBaseSolution.sln'
        if (Test-Path -Path $candidate -PathType Leaf) { return $current.Path }
        $parent = Split-Path -Path $current.Path -Parent
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $current.Path) { break }
        $current = Resolve-Path -Path $parent
    }
    throw 'Unable to locate repository root containing MigrationBaseSolution.sln.'
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Find-RepoRoot -StartPath $scriptRoot
$adminRoot = Join-Path -Path $repoRoot -ChildPath 'src/Admin/Migration.Admin.Web'
$sourceRoot = Join-Path -Path $adminRoot -ChildPath 'src'
$featureRoot = Join-Path -Path $sourceRoot -ChildPath 'features'
$docsRoot = Join-Path -Path $repoRoot -ChildPath 'docs/P10'
$reportPath = Join-Path -Path $docsRoot -ChildPath 'P10.2BS-AdminWebConsolidationBuildSurfaceReview.Report.md'

if (-not (Test-Path -Path $adminRoot -PathType Container)) { throw ('Admin Web root not found: {0}' -f $adminRoot) }
if (-not (Test-Path -Path $sourceRoot -PathType Container)) { throw ('Admin Web source root not found: {0}' -f $sourceRoot) }
if (-not (Test-Path -Path $docsRoot -PathType Container)) { New-Item -Path $docsRoot -ItemType Directory -Force | Out-Null }

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2BS - Admin Web Consolidation Build Surface Review')
[void]$report.Add('')
[void]$report.Add(('Generated: {0:u}' -f (Get-Date).ToUniversalTime()))
[void]$report.Add(('Admin Web: `{0}`' -f $adminRoot))
[void]$report.Add('')

$packageJson = Join-Path -Path $adminRoot -ChildPath 'package.json'
$tsconfigJson = Join-Path -Path $adminRoot -ChildPath 'tsconfig.json'
$viteConfig = Join-Path -Path $adminRoot -ChildPath 'vite.config.ts'
$appFile = Join-Path -Path $sourceRoot -ChildPath 'App.tsx'

[void]$report.Add('## Build Surface Files')
$buildFiles = @(
    [pscustomobject]@{ Label = 'package.json'; Path = $packageJson },
    [pscustomobject]@{ Label = 'tsconfig.json'; Path = $tsconfigJson },
    [pscustomobject]@{ Label = 'vite.config.ts'; Path = $viteConfig },
    [pscustomobject]@{ Label = 'src/App.tsx'; Path = $appFile }
)
foreach ($item in $buildFiles) {
    if (Test-Path -Path $item.Path -PathType Leaf) { [void]$report.Add(('- OK: {0}' -f $item.Label)) }
    else { [void]$report.Add(('- MISSING: {0} at `{1}`' -f $item.Label, $item.Path)) }
}
[void]$report.Add('')

[void]$report.Add('## Package Script Posture')
if (Test-Path -Path $packageJson -PathType Leaf) {
    $packageText = Get-Content -Path $packageJson -Raw
    if ($packageText -match '"build"\s*:\s*"[^"]*tsc\s+-b[^"]*vite\s+build') { [void]$report.Add('- OK: build script includes TypeScript build and Vite build.') }
    else { [void]$report.Add('- REVIEW: build script was not recognized as `tsc -b && vite build`.') }
    if ($packageText -match '"dev"\s*:\s*"vite"') { [void]$report.Add('- OK: dev script invokes Vite.') }
    else { [void]$report.Add('- REVIEW: dev script does not appear to invoke Vite.') }
} else { [void]$report.Add('- MISSING: package.json unavailable.') }
[void]$report.Add('')

[void]$report.Add('## TypeScript Scope Posture')
if (Test-Path -Path $tsconfigJson -PathType Leaf) {
    $tsconfigText = Get-Content -Path $tsconfigJson -Raw
    if ($tsconfigText -match '"include"\s*:\s*\[\s*"src"\s*\]') { [void]$report.Add('- OK: tsconfig includes canonical `src` scope.') }
    else { [void]$report.Add('- REVIEW: tsconfig include scope is not the simple canonical `src` scope.') }
} else { [void]$report.Add('- MISSING: tsconfig.json unavailable.') }
[void]$report.Add('')

[void]$report.Add('## Vite Proxy Posture')
if (Test-Path -Path $viteConfig -PathType Leaf) {
    $viteText = Get-Content -Path $viteConfig -Raw
    if ($viteText.Contains('VITE_ADMIN_API_PROXY_TARGET')) { [void]$report.Add('- OK: Vite config references VITE_ADMIN_API_PROXY_TARGET.') }
    else { [void]$report.Add('- REVIEW: Vite config does not reference VITE_ADMIN_API_PROXY_TARGET.') }
    if ($viteText.Contains('"/api"') -or $viteText.Contains("'/api'")) { [void]$report.Add('- OK: Vite config includes an API proxy path.') }
    else { [void]$report.Add('- REVIEW: Vite config does not appear to include an API proxy path.') }
} else { [void]$report.Add('- MISSING: vite.config.ts unavailable.') }
[void]$report.Add('')

[void]$report.Add('## Canonical Feature Root')
if (Test-Path -Path $featureRoot -PathType Container) {
    $expectedGroups = @('operations','governance','platform','security','connectors')
    foreach ($group in $expectedGroups) {
        $groupPath = Join-Path -Path $featureRoot -ChildPath $group
        if (Test-Path -Path $groupPath -PathType Container) { [void]$report.Add(('- OK: features/{0}' -f $group)) }
        else { [void]$report.Add(('- REVIEW: features/{0} missing.' -f $group)) }
    }
} else { [void]$report.Add('- MISSING: canonical features root unavailable.') }
[void]$report.Add('')

$sourceFiles = @(Get-ChildItem -Path $sourceRoot -Recurse -File -Include '*.ts','*.tsx' -ErrorAction Stop)
[void]$report.Add('## Compiled Source Import Hygiene')
[void]$report.Add(('Source files scanned: {0}' -f $sourceFiles.Length))
$tsxImportFindings = New-Object 'System.Collections.Generic.List[string]'
$referenceImportFindings = New-Object 'System.Collections.Generic.List[string]'
$appsImportFindings = New-Object 'System.Collections.Generic.List[string]'
foreach ($sourceFile in $sourceFiles) {
    $relativePath = $sourceFile.FullName.Substring($sourceRoot.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $lines = @(Get-Content -Path $sourceFile.FullName)
    foreach ($line in $lines) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $trimmed = $line.Trim()
        if (-not ($trimmed.StartsWith('import '))) { continue }
        if ($trimmed.Contains(".tsx'") -or $trimmed.Contains('.tsx"')) { [void]$tsxImportFindings.Add(('{0}: {1}' -f $relativePath, $trimmed)) }
        if ($trimmed.Contains('/reference/') -or $trimmed.Contains('../reference') -or $trimmed.Contains('reference/')) { [void]$referenceImportFindings.Add(('{0}: {1}' -f $relativePath, $trimmed)) }
        if ($trimmed.Contains('/apps/') -or $trimmed.Contains('../apps') -or $trimmed.Contains('apps/migration-admin-ui')) { [void]$appsImportFindings.Add(('{0}: {1}' -f $relativePath, $trimmed)) }
    }
}
if ($tsxImportFindings.Count -eq 0) { [void]$report.Add('- OK: no `.tsx` extension-bearing imports found in compiled source.') }
else { [void]$report.Add(('- REVIEW: `.tsx` extension-bearing imports found: {0}' -f $tsxImportFindings.Count)); foreach ($finding in $tsxImportFindings) { [void]$report.Add(('  - {0}' -f $finding)) } }
if ($referenceImportFindings.Count -eq 0) { [void]$report.Add('- OK: no imports from reference material found in compiled source.') }
else { [void]$report.Add(('- REVIEW: reference imports found: {0}' -f $referenceImportFindings.Count)); foreach ($finding in $referenceImportFindings) { [void]$report.Add(('  - {0}' -f $finding)) } }
if ($appsImportFindings.Count -eq 0) { [void]$report.Add('- OK: no imports from apps material found in compiled source.') }
else { [void]$report.Add(('- REVIEW: apps imports found: {0}' -f $appsImportFindings.Count)); foreach ($finding in $appsImportFindings) { [void]$report.Add(('  - {0}' -f $finding)) } }
[void]$report.Add('')
[void]$report.Add('## Next-Step Recommendation')
[void]$report.Add('- Use this report to decide whether the next set can begin P10 site-up work or needs one more targeted source cleanup.')
Set-Content -Path $reportPath -Value $report -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2BS Admin Web consolidation build surface review applied.'
