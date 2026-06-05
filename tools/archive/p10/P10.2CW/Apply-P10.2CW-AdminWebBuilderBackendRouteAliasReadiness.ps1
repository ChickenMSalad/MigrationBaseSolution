Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$repoRootPath = $repoRoot.Path

$adminWebRoot = Join-Path $repoRootPath 'src\Admin\Migration.Admin.Web'
$adminWebSrc = Join-Path $adminWebRoot 'src'
$adminApiRoot = Join-Path $repoRootPath 'src\Admin'
$docsDir = Join-Path $repoRootPath 'docs\P10'
New-Item -ItemType Directory -Force -Path $docsDir | Out-Null
$reportPath = Join-Path $docsDir 'P10.2CW-AdminWebBuilderBackendRouteAliasReadiness.md'

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2CW - Admin Web Builder Backend Route Alias Readiness')
[void]$report.Add('')
[void]$report.Add(('Generated UTC: `{0}`' -f ([DateTime]::UtcNow.ToString('o'))))
[void]$report.Add('')
[void]$report.Add('## Purpose')
[void]$report.Add('')
[void]$report.Add('This set prepares the next endpoint-alignment step without guessing or adding fake endpoints. It inspects the local repo for restored builder UI calls and real backend controller route declarations, then classifies which aliases can be safely added in a later implementation set.')
[void]$report.Add('')

if (-not (Test-Path -LiteralPath $adminWebSrc)) {
    throw ('Admin Web source root was not found: {0}' -f $adminWebSrc)
}
if (-not (Test-Path -LiteralPath $adminApiRoot)) {
    throw ('Admin source root was not found: {0}' -f $adminApiRoot)
}

$builderTerms = @('manifest-builder', 'taxonomy-builder', 'mapping-builder', 'mapping-profiles', 'manifest', 'taxonomy', 'mapping')
$uiFiles = New-Object 'System.Collections.Generic.List[object]'
$controllerFiles = New-Object 'System.Collections.Generic.List[object]'

$uiCandidates = Get-ChildItem -LiteralPath $adminWebSrc -Recurse -File -Include *.ts,*.tsx | Where-Object {
    $full = $_.FullName
    ($full -notmatch '\\node_modules\\') -and ($full -notmatch '\\dist\\') -and ($full -notmatch '\\reference\\')
}
foreach ($file in $uiCandidates) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    $matched = $false
    foreach ($term in $builderTerms) {
        if ($content.IndexOf($term, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
            $matched = $true
        }
    }
    if ($matched) {
        [void]$uiFiles.Add($file)
    }
}

$backendCandidates = Get-ChildItem -LiteralPath $adminApiRoot -Recurse -File -Include *.cs | Where-Object {
    $full = $_.FullName
    ($full -notmatch '\\bin\\') -and ($full -notmatch '\\obj\\') -and ($full -notmatch '\\.git\\')
}
foreach ($file in $backendCandidates) {
    $name = $file.Name
    $content = Get-Content -LiteralPath $file.FullName -Raw
    $isCandidate = $false
    if ($name -like '*Controller.cs') {
        foreach ($term in @('Manifest','Taxonomy','Mapping','Builder','Profile')) {
            if ($name.IndexOf($term, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
                $isCandidate = $true
            }
        }
    }
    foreach ($term in $builderTerms) {
        if ($content.IndexOf($term, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
            $isCandidate = $true
        }
    }
    if ($isCandidate) {
        [void]$controllerFiles.Add($file)
    }
}

[void]$report.Add('## Builder UI files containing builder endpoint terms')
[void]$report.Add('')
if ($uiFiles.Count -eq 0) {
    [void]$report.Add('- None found in compiled Admin Web source.')
} else {
    foreach ($file in $uiFiles) {
        $relative = $file.FullName.Substring($repoRootPath.Length).TrimStart('\')
        [void]$report.Add(('- `{0}`' -f $relative))
    }
}
[void]$report.Add('')

[void]$report.Add('## Backend controller/source candidates')
[void]$report.Add('')
if ($controllerFiles.Count -eq 0) {
    [void]$report.Add('- None found under `src/Admin`.')
} else {
    foreach ($file in $controllerFiles) {
        $relative = $file.FullName.Substring($repoRootPath.Length).TrimStart('\')
        [void]$report.Add(('- `{0}`' -f $relative))
    }
}
[void]$report.Add('')

[void]$report.Add('## Route alias readiness')
[void]$report.Add('')
[void]$report.Add('The previous runtime smoke showed builder UI endpoint candidates. This report intentionally does not add controller aliases. The next implementation set should add aliases only when this report identifies a real backend controller or service surface that already handles the corresponding builder operation.')
[void]$report.Add('')
[void]$report.Add('Expected next-step classifications:')
[void]$report.Add('')
[void]$report.Add('- `manifest-builder/build`: safe only if a real manifest build endpoint/service is present; previous smoke showed route existence with verb mismatch.')
[void]$report.Add('- `manifest-builder/validate` and `manifest-builder/preview`: safe only if corresponding backend validation/preview surfaces exist.')
[void]$report.Add('- `taxonomy-builder/*`: do not implement aliases unless taxonomy backend surfaces are present.')
[void]$report.Add('- `mapping-builder/*` and `mapping-profiles`: do not implement aliases unless mapping/profile backend surfaces are present.')
[void]$report.Add('')

Set-Content -LiteralPath $reportPath -Value $report.ToArray() -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2CW Admin Web builder backend route alias readiness applied.'
