Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $scriptRoot = Split-Path -Parent $PSCommandPath
    $root = Resolve-Path (Join-Path $scriptRoot '..\..\..')
    return $root.Path
}

function Join-Parts {
    param([string[]]$Parts)
    $path = $Parts[0]
    for ($i = 1; $i -lt $Parts.Count; $i++) {
        $path = Join-Path $path $Parts[$i]
    }
    return $path
}

function Ensure-Directory {
    param([string]$Path)
    if (-not (Test-Path -Path $Path -PathType Container)) {
        New-Item -Path $Path -ItemType Directory -Force | Out-Null
    }
}

function Read-Text {
    param([string]$Path)
    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Required file was not found: {0}' -f $Path)
    }
    return [System.IO.File]::ReadAllText($Path)
}

function Assert-NoDuplicateMove {
    param([string]$Source, [string]$Target, [string]$Label)
    if ((Test-Path -Path $Source -PathType Leaf) -and (Test-Path -Path $Target -PathType Leaf)) {
        throw ('Duplicate source and target remain for {0}. Source: {1}. Target: {2}' -f $Label, $Source, $Target)
    }
}

function Assert-NoAnchoredLegacyImport {
    param([string]$Path, [string]$ImportSource, [string]$Label)
    if (-not (Test-Path -Path $Path -PathType Leaf)) { return }
    $content = Read-Text -Path $Path
    $escaped = [regex]::Escape($ImportSource)
    $single = "(?m)^\s*import\s+.+?\s+from\s+'$escaped'\s*;?\s*$"
    $double = '(?m)^\s*import\s+.+?\s+from\s+"' + $escaped + '"\s*;?\s*$'
    if (($content -match $single) -or ($content -match $double)) {
        throw ('Unexpected anchored legacy import for {0}: {1} in {2}' -f $Label, $ImportSource, $Path)
    }
}

$repoRoot = Get-RepoRoot
$adminSrc = Join-Parts -Parts @($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
if (-not (Test-Path -Path $adminSrc -PathType Container)) { throw ('Canonical Admin Web src folder was not found: {0}' -f $adminSrc) }
$appPath = Join-Parts -Parts @($adminSrc, 'App.tsx')
if (-not (Test-Path -Path $appPath -PathType Leaf)) { throw ('Canonical Admin Web App.tsx was not found: {0}' -f $appPath) }
$appContent = Read-Text -Path $appPath

$features = @(
    [pscustomobject]@{ Name = 'Artifacts'; Domain = 'operations'; Feature = 'artifacts'; Page = 'Artifacts'; Api = @('artifactsApi', 'artifacts'); Types = @('artifacts'); Css = @() },
    [pscustomobject]@{ Name = 'Connectors'; Domain = 'connectors'; Feature = 'connectors'; Page = 'Connectors'; Api = @('connectorsApi', 'connectors'); Types = @('connectors'); Css = @() },
    [pscustomobject]@{ Name = 'Credentials'; Domain = 'security'; Feature = 'credentials'; Page = 'Credentials'; Api = @('credentialsApi', 'credentials'); Types = @('credentials'); Css = @('Credentials') },
    [pscustomobject]@{ Name = 'Dashboard'; Domain = 'operations'; Feature = 'dashboard'; Page = 'Dashboard'; Api = @('dashboardApi', 'dashboard'); Types = @('dashboard'); Css = @() },
    [pscustomobject]@{ Name = 'Manifest Builder'; Domain = 'operations'; Feature = 'manifestBuilder'; Page = 'ManifestBuilder'; Api = @('manifestBuilderApi', 'manifestBuilder'); Types = @('manifestBuilder'); Css = @() },
    [pscustomobject]@{ Name = 'Mapping Builder'; Domain = 'operations'; Feature = 'mappingBuilder'; Page = 'MappingBuilder'; Api = @('mappingBuilderApi', 'mappingBuilder'); Types = @('mappingBuilder'); Css = @() },
    [pscustomobject]@{ Name = 'Preflight'; Domain = 'operations'; Feature = 'preflight'; Page = 'Preflight'; Api = @('preflightApi', 'preflight'); Types = @('preflight'); Css = @() },
    [pscustomobject]@{ Name = 'Projects'; Domain = 'operations'; Feature = 'projects'; Page = 'Projects'; Api = @('projectsApi', 'projects'); Types = @('projects'); Css = @() },
    [pscustomobject]@{ Name = 'Project Detail'; Domain = 'operations'; Feature = 'projects'; Page = 'ProjectDetail'; Api = @('projectDetailApi', 'projectDetail', 'projectsApi', 'projects'); Types = @('projectDetail', 'projects'); Css = @() },
    [pscustomobject]@{ Name = 'Runs'; Domain = 'operations'; Feature = 'runs'; Page = 'Runs'; Api = @('runsApi', 'runs'); Types = @('runs'); Css = @() },
    [pscustomobject]@{ Name = 'Runtime Run Detail'; Domain = 'operations'; Feature = 'runtimeRunDetail'; Page = 'RuntimeRunDetail'; Api = @('runtimeRunDetailApi', 'runtimeRunDetail'); Types = @('runtimeRunDetail'); Css = @() },
    [pscustomobject]@{ Name = 'Taxonomy Builder'; Domain = 'governance'; Feature = 'taxonomyBuilder'; Page = 'TaxonomyBuilder'; Api = @('taxonomyBuilderApi', 'taxonomyBuilder'); Types = @('taxonomyBuilder'); Css = @() }
)

foreach ($feature in $features) {
    $featureRoot = Join-Parts -Parts @($adminSrc, 'features', $feature.Domain, $feature.Feature)
    $pageSource = Join-Parts -Parts @($adminSrc, 'pages', ($feature.Page + '.tsx'))
    $pageTarget = Join-Parts -Parts @($featureRoot, 'pages', ($feature.Page + '.tsx'))
    Assert-NoDuplicateMove -Source $pageSource -Target $pageTarget -Label ($feature.Name + ' page')
    if (Test-Path -Path $pageTarget -PathType Leaf) {
        Assert-NoAnchoredLegacyImport -Path $pageTarget -ImportSource '../components/Card' -Label ($feature.Name + ' page Card')
        Assert-NoAnchoredLegacyImport -Path $pageTarget -ImportSource '../components/LoadingError' -Label ($feature.Name + ' page LoadingError')
        Assert-NoAnchoredLegacyImport -Path $pageTarget -ImportSource '../components/StatusPill' -Label ($feature.Name + ' page StatusPill')
        Assert-NoAnchoredLegacyImport -Path $pageTarget -ImportSource '../components/EmptyState' -Label ($feature.Name + ' page EmptyState')
    }
    foreach ($cssBase in @($feature.Css)) {
        Assert-NoDuplicateMove -Source (Join-Parts -Parts @($adminSrc, 'pages', ($cssBase + '.css'))) -Target (Join-Parts -Parts @($featureRoot, 'pages', ($cssBase + '.css'))) -Label ($feature.Name + ' css')
    }
    foreach ($apiBase in @($feature.Api)) {
        $apiTarget = Join-Parts -Parts @($featureRoot, 'api', ($apiBase + '.ts'))
        Assert-NoDuplicateMove -Source (Join-Parts -Parts @($adminSrc, 'api', ($apiBase + '.ts'))) -Target $apiTarget -Label ($feature.Name + ' API ' + $apiBase)
        if (Test-Path -Path $apiTarget -PathType Leaf) {
            Assert-NoAnchoredLegacyImport -Path $apiTarget -ImportSource './core/adminApiClient' -Label ($feature.Name + ' API adminApiClient')
            Assert-NoAnchoredLegacyImport -Path $apiTarget -ImportSource './core/client' -Label ($feature.Name + ' API core client')
        }
    }
    foreach ($typeBase in @($feature.Types)) {
        Assert-NoDuplicateMove -Source (Join-Parts -Parts @($adminSrc, 'types', ($typeBase + '.ts'))) -Target (Join-Parts -Parts @($featureRoot, 'types', ($typeBase + '.ts'))) -Label ($feature.Name + ' types ' + $typeBase)
    }
    $flatImportSingle = "from './pages/" + $feature.Page + "'"
    $flatImportDouble = 'from "./pages/' + $feature.Page + '"'
    if ((Test-Path -Path $pageTarget -PathType Leaf) -and (($appContent.Contains($flatImportSingle)) -or ($appContent.Contains($flatImportDouble)))) {
        throw ('App.tsx still has a flat page import for {0}' -f $feature.Page)
    }
}
Write-Host 'P10.2AS Admin Web residual API/types feature batch move validation passed.'
