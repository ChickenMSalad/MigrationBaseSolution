Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$adminRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$sourceRoot = Join-Path $adminRoot 'src'
$appPath = Join-Path $sourceRoot 'App.tsx'
$featuresRoot = Join-Path $sourceRoot 'features'
$docsRoot = Join-Path $repoRoot 'docs\P10'
$reportPath = Join-Path $docsRoot 'P10.2BN-AdminWebFeatureRouteCoverageAudit.Report.md'

if (-not (Test-Path -Path $appPath -PathType Leaf)) {
    throw ('Required App.tsx was not found: {0}' -f $appPath)
}
if (-not (Test-Path -Path $featuresRoot -PathType Container)) {
    throw ('Required features folder was not found: {0}' -f $featuresRoot)
}
if (-not (Test-Path -Path $docsRoot -PathType Container)) {
    New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
}

$appContent = Get-Content -Path $appPath -Raw
$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2BN - Admin Web Feature Route Coverage Audit')
[void]$report.Add('')
[void]$report.Add(('Generated: {0:yyyy-MM-dd HH:mm:ss}' -f (Get-Date)))
[void]$report.Add('')
[void]$report.Add('## Scope')
[void]$report.Add('')
[void]$report.Add('- Reads local canonical Admin Web only.')
[void]$report.Add('- Does not rewrite source files.')
[void]$report.Add('- Reports route/import/page coverage for follow-up cleanup.')
[void]$report.Add('')

$importStatements = New-Object 'System.Collections.Generic.List[string]'
$segments = @($appContent -split ';')
foreach ($segment in $segments) {
    $trimmed = $segment.Trim()
    if ($trimmed.StartsWith('import ')) {
        [void]$importStatements.Add(($trimmed + ';'))
    }
}

$imports = New-Object 'System.Collections.Generic.List[object]'
foreach ($importStatement in $importStatements) {
    $sourceMatch = [regex]::Match($importStatement, 'from\s+[''\"]([^''\"]+)[''\"]')
    if ($sourceMatch.Success) {
        $sourceValue = $sourceMatch.Groups[1].Value
        $nameValue = ''
        $namedMatch = [regex]::Match($importStatement, '^import\s+\{\s*([^}]+?)\s*\}\s+from')
        if ($namedMatch.Success) {
            $nameParts = @($namedMatch.Groups[1].Value -split ',')
            if ($nameParts.Length -gt 0) {
                $nameValue = $nameParts[0].Trim()
            }
        }
        else {
            $defaultMatch = [regex]::Match($importStatement, '^import\s+([^\s\{]+)\s+from')
            if ($defaultMatch.Success) {
                $nameValue = $defaultMatch.Groups[1].Value.Trim()
            }
        }
        [void]$imports.Add([pscustomobject]@{
            Name = $nameValue
            Source = $sourceValue
            Statement = $importStatement
        })
    }
}

$routeMatches = @([regex]::Matches($appContent, '<Route\s+path=[''\"]([^''\"]+)[''\"][^>]*element=\{<([A-Za-z0-9_]+)'))
$routes = New-Object 'System.Collections.Generic.List[object]'
foreach ($routeMatch in $routeMatches) {
    [void]$routes.Add([pscustomobject]@{
        Path = $routeMatch.Groups[1].Value
        Element = $routeMatch.Groups[2].Value
    })
}

$pageFiles = New-Object 'System.Collections.Generic.List[object]'
$featurePageFiles = @(Get-ChildItem -Path $featuresRoot -Recurse -File -Filter '*.tsx' | Where-Object { $_.FullName -like '*\pages\*' })
foreach ($pageFile in $featurePageFiles) {
    $relativePath = $pageFile.FullName.Substring($sourceRoot.Length).TrimStart('\') -replace '\\','/'
    $baseName = [System.IO.Path]::GetFileNameWithoutExtension($pageFile.Name)
    [void]$pageFiles.Add([pscustomobject]@{
        Name = $baseName
        RelativePath = $relativePath
    })
}

[void]$report.Add('## Counts')
[void]$report.Add('')
[void]$report.Add(('- App imports discovered: {0}' -f $imports.Count))
[void]$report.Add(('- Route entries discovered: {0}' -f $routes.Count))
[void]$report.Add(('- Feature page files discovered: {0}' -f $pageFiles.Count))
[void]$report.Add('')

[void]$report.Add('## Duplicate route paths')
[void]$report.Add('')
$duplicateRouteGroups = @($routes | Group-Object -Property Path | Where-Object { $_.Count -gt 1 })
if ($duplicateRouteGroups.Length -eq 0) {
    [void]$report.Add('- None detected.')
}
else {
    foreach ($group in $duplicateRouteGroups) {
        [void]$report.Add(('- `{0}` appears {1} times.' -f $group.Name, $group.Count))
    }
}
[void]$report.Add('')

[void]$report.Add('## Imports ending in .tsx')
[void]$report.Add('')
$extensionImports = @($imports | Where-Object { $_.Source.EndsWith('.tsx') })
if ($extensionImports.Length -eq 0) {
    [void]$report.Add('- None detected.')
}
else {
    foreach ($item in $extensionImports) {
        [void]$report.Add(('- `{0}` from `{1}`' -f $item.Name, $item.Source))
    }
}
[void]$report.Add('')

[void]$report.Add('## Reference or apps imports from App.tsx')
[void]$report.Add('')
$referenceImports = @($imports | Where-Object { $_.Source -like '*reference*' -or $_.Source -like '*apps*' })
if ($referenceImports.Length -eq 0) {
    [void]$report.Add('- None detected.')
}
else {
    foreach ($item in $referenceImports) {
        [void]$report.Add(('- `{0}` from `{1}`' -f $item.Name, $item.Source))
    }
}
[void]$report.Add('')

[void]$report.Add('## Routes by path')
[void]$report.Add('')
foreach ($route in @($routes | Sort-Object -Property Path, Element)) {
    [void]$report.Add(('- `{0}` -> `{1}`' -f $route.Path, $route.Element))
}
[void]$report.Add('')

[void]$report.Add('## Feature page files not directly routed by component name')
[void]$report.Add('')
$routeElements = @($routes | ForEach-Object { $_.Element })
$hiddenPages = @($pageFiles | Where-Object { $routeElements -notcontains $_.Name } | Sort-Object -Property RelativePath)
if ($hiddenPages.Length -eq 0) {
    [void]$report.Add('- None detected by component filename convention.')
}
else {
    foreach ($page in $hiddenPages) {
        [void]$report.Add(('- `{0}`' -f $page.RelativePath))
    }
}
[void]$report.Add('')

[void]$report.Add('## Imported page components not used by routes')
[void]$report.Add('')
$pageImports = @($imports | Where-Object { $_.Source -like './features/*/pages/*' -or $_.Source -like './features/*/*/pages/*' -or $_.Source -like './features/*/*/*/pages/*' })
$unusedPageImports = @($pageImports | Where-Object { $routeElements -notcontains $_.Name } | Sort-Object -Property Name)
if ($unusedPageImports.Length -eq 0) {
    [void]$report.Add('- None detected.')
}
else {
    foreach ($item in $unusedPageImports) {
        [void]$report.Add(('- `{0}` from `{1}`' -f $item.Name, $item.Source))
    }
}
[void]$report.Add('')

Set-Content -Path $reportPath -Value $report -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2BN Admin Web feature route coverage audit applied.'
