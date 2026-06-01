[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string] $ConfigurationPath,

    [Parameter(Mandatory = $false)]
    [string] $OutputPath
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Resolve-ScriptRoot {
    if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) {
        return $PSScriptRoot
    }

    if (-not [string]::IsNullOrWhiteSpace($PSCommandPath)) {
        return (Split-Path -Parent $PSCommandPath)
    }

    throw 'Unable to resolve script root.'
}

function Join-RepoPath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $RepoRoot,

        [Parameter(Mandatory = $true)]
        [string] $RelativePath
    )

    $normalized = $RelativePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar)
    return [System.IO.Path]::Combine($RepoRoot, $normalized)
}

function Read-TextFile {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw ('Required file is missing: {0}' -f $Path)
    }

    return Get-Content -LiteralPath $Path -Raw
}

$scriptRoot = Resolve-ScriptRoot
$repoRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)

if ([string]::IsNullOrWhiteSpace($ConfigurationPath)) {
    $ConfigurationPath = [System.IO.Path]::Combine($repoRoot, 'config-samples', 'p10-admin-web-canonical-nav-and-feature-structure-review.sample.json')
}
elseif (-not [System.IO.Path]::IsPathRooted($ConfigurationPath)) {
    $ConfigurationPath = [System.IO.Path]::Combine($repoRoot, $ConfigurationPath)
}

$config = Get-Content -LiteralPath $ConfigurationPath -Raw | ConvertFrom-Json

foreach ($propertyName in @('canonicalAdminUiPath', 'featureSourcePath', 'featureFamilies')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('Configuration is missing property: {0}' -f $propertyName)
    }
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    if ($null -ne $config.PSObject.Properties['outputPath'] -and -not [string]::IsNullOrWhiteSpace([string]$config.outputPath)) {
        $OutputPath = [string]$config.outputPath
    }
    else {
        $OutputPath = 'artifacts/p10/admin-web-canonical-nav-and-feature-structure-review.md'
    }
}

if (-not [System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = [System.IO.Path]::Combine($repoRoot, $OutputPath)
}

$outputDirectory = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory) -and -not (Test-Path -LiteralPath $outputDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

$canonicalRoot = Join-RepoPath -RepoRoot $repoRoot -RelativePath ([string]$config.canonicalAdminUiPath)
$sourceRoot = Join-RepoPath -RepoRoot $repoRoot -RelativePath ([string]$config.featureSourcePath)

if (-not (Test-Path -LiteralPath $canonicalRoot -PathType Container)) {
    throw ('Canonical Admin UI path not found: {0}' -f $canonicalRoot)
}
if (-not (Test-Path -LiteralPath $sourceRoot -PathType Container)) {
    throw ('Feature-source Admin UI path not found: {0}' -f $sourceRoot)
}

$appPath = [System.IO.Path]::Combine($canonicalRoot, 'src', 'App.tsx')
$layoutPath = [System.IO.Path]::Combine($canonicalRoot, 'src', 'components', 'Layout.tsx')
$appText = Read-TextFile -Path $appPath
$layoutText = Read-TextFile -Path $layoutPath

$routeMatches = [regex]::Matches($appText, 'path\s*=\s*"([^"]+)"')
$routes = @()
foreach ($match in $routeMatches) {
    $routes += [string]$match.Groups[1].Value
}

$navMatches = [regex]::Matches($layoutText, 'to:\s*"([^"]+)"')
$navRoutes = @()
foreach ($match in $navMatches) {
    $navRoutes += [string]$match.Groups[1].Value
}

$duplicateNavRoutes = @()
foreach ($group in ($navRoutes | Group-Object | Where-Object { $_.Count -gt 1 })) {
    $duplicateNavRoutes += [pscustomobject]@{
        Route = [string]$group.Name
        Count = [int]$group.Count
    }
}

$pagesPath = [System.IO.Path]::Combine($canonicalRoot, 'src', 'pages')
$apiPath = [System.IO.Path]::Combine($canonicalRoot, 'src', 'api')
$typesPath = [System.IO.Path]::Combine($canonicalRoot, 'src', 'types')
$featuresPath = [System.IO.Path]::Combine($canonicalRoot, 'src', 'features')

$pageFiles = @()
if (Test-Path -LiteralPath $pagesPath -PathType Container) {
    $pageFiles = @(Get-ChildItem -LiteralPath $pagesPath -Filter '*.tsx' -File | Sort-Object Name)
}

$apiFiles = @()
if (Test-Path -LiteralPath $apiPath -PathType Container) {
    $apiFiles = @(Get-ChildItem -LiteralPath $apiPath -Filter '*.ts' -File | Sort-Object Name)
}

$typeFiles = @()
if (Test-Path -LiteralPath $typesPath -PathType Container) {
    $typeFiles = @(Get-ChildItem -LiteralPath $typesPath -Filter '*.ts' -File | Sort-Object Name)
}

$featureSourceFolders = @()
$sourceFeatureRoot = [System.IO.Path]::Combine($sourceRoot, 'src', 'features')
if (Test-Path -LiteralPath $sourceFeatureRoot -PathType Container) {
    $featureSourceFolders = @(Get-ChildItem -LiteralPath $sourceFeatureRoot -Directory | Sort-Object Name)
}

$report = New-Object System.Collections.Generic.List[string]
$report.Add('# P10.2AA Admin Web Canonical Navigation and Feature Structure Review')
$report.Add('')
$report.Add(('- Generated UTC: {0}' -f ([DateTimeOffset]::UtcNow.ToString('O'))))
$report.Add(('- Canonical UI: `{0}`' -f [string]$config.canonicalAdminUiPath))
$report.Add(('- Feature-source UI: `{0}`' -f [string]$config.featureSourcePath))
$report.Add('')

$report.Add('## Navigation duplicate check')
$report.Add('')
if (@($duplicateNavRoutes).Count -eq 0) {
    $report.Add('- No duplicate navigation routes detected in `Layout.tsx`.')
}
else {
    foreach ($item in $duplicateNavRoutes) {
        $report.Add(('- Duplicate nav route `{0}` appears {1} times.' -f $item.Route, $item.Count))
    }
}
$report.Add('')

$report.Add('## Route coverage')
$report.Add('')
$report.Add(('- Routes discovered in `App.tsx`: {0}' -f @($routes).Count))
$report.Add(('- Navigation entries discovered in `Layout.tsx`: {0}' -f @($navRoutes).Count))
$report.Add('')

$report.Add('## Flat canonical files to review')
$report.Add('')
$report.Add(('### Pages in `src/pages` ({0})' -f @($pageFiles).Count))
foreach ($file in $pageFiles) {
    $report.Add(('- `{0}`' -f $file.Name))
}
$report.Add('')
$report.Add(('### API clients in `src/api` ({0})' -f @($apiFiles).Count))
foreach ($file in $apiFiles) {
    $report.Add(('- `{0}`' -f $file.Name))
}
$report.Add('')
$report.Add(('### Type files in `src/types` ({0})' -f @($typeFiles).Count))
foreach ($file in $typeFiles) {
    $report.Add(('- `{0}`' -f $file.Name))
}
$report.Add('')

$report.Add('## Feature-source folders still present')
$report.Add('')
foreach ($folder in $featureSourceFolders) {
    $report.Add(('- `{0}`' -f $folder.Name))
}
$report.Add('')

$report.Add('## Recommended canonical feature grouping')
$report.Add('')
foreach ($family in @($config.featureFamilies)) {
    $name = [string]$family.name
    $destination = [string]$family.destination
    $report.Add(('### {0}' -f $name))
    $report.Add('')
    $report.Add(('- Destination: `{0}`' -f $destination))
    $report.Add('')
    if ($null -ne $family.PSObject.Properties['pageNames']) {
        foreach ($pageName in @($family.pageNames)) {
            $report.Add(('- `{0}`' -f [string]$pageName))
        }
    }
    $report.Add('')
}

$report.Add('## Next recommended implementation set')
$report.Add('')
$report.Add('Start with a single feature-family move into `src/features/operations`, then update imports/routes and rebuild.')
$report.Add('Do not move all pages at once.')
$report.Add('')

Set-Content -LiteralPath $OutputPath -Value $report -Encoding UTF8
Write-Host ('P10.2AA report written to {0}' -f $OutputPath)
