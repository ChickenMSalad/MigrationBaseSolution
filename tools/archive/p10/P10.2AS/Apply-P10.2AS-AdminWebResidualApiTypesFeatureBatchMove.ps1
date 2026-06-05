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

function Move-IfPresent {
    param([string]$Source, [string]$Target, [string]$Label)
    if (Test-Path -Path $Target -PathType Leaf) {
        Write-Host ('Already present {0}: {1}' -f $Label, $Target)
        return $false
    }
    if (-not (Test-Path -Path $Source -PathType Leaf)) {
        Write-Host ('Source not present for {0}: {1}' -f $Label, $Source)
        return $false
    }
    Ensure-Directory -Path (Split-Path -Parent $Target)
    Move-Item -Path $Source -Destination $Target
    Write-Host ('Moved {0}: {1}' -f $Label, $Target)
    return $true
}

function Replace-ImportSource {
    param([string]$Path, [string]$OldSource, [string]$NewSource, [string]$Label)
    if (-not (Test-Path -Path $Path -PathType Leaf)) { return }
    $content = Read-Text -Path $Path
    $escapedOld = [regex]::Escape($OldSource)
    $patternSingle = "(from\s+')$escapedOld(')"
    $patternDouble = '(from\s+")' + $escapedOld + '(")'
    $updated = [regex]::Replace($content, $patternSingle, ('$1' + $NewSource + '$2'))
    $updated = [regex]::Replace($updated, $patternDouble, ('$1' + $NewSource + '$2'))
    if ($updated -ne $content) {
        [System.IO.File]::WriteAllText($Path, $updated)
        Write-Host ('Updated {0}: {1}' -f $Label, $Path)
    }
    else {
        Write-Host ('No import normalization needed for {0}: {1}' -f $Label, $Path)
    }
}

function Normalize-PageImports {
    param([string]$Path, [string]$Label)
    Replace-ImportSource -Path $Path -OldSource '../components/Card' -NewSource '../../../../components/Card' -Label ($Label + ' Card import')
    Replace-ImportSource -Path $Path -OldSource '../components/LoadingError' -NewSource '../../../../components/LoadingError' -Label ($Label + ' LoadingError import')
    Replace-ImportSource -Path $Path -OldSource '../components/StatusPill' -NewSource '../../../../components/StatusPill' -Label ($Label + ' StatusPill import')
    Replace-ImportSource -Path $Path -OldSource '../components/EmptyState' -NewSource '../../../../components/EmptyState' -Label ($Label + ' EmptyState import')
    Replace-ImportSource -Path $Path -OldSource '../styles/tokens' -NewSource '../../../../styles/tokens' -Label ($Label + ' tokens import')
}

function Normalize-ApiImports {
    param([string]$Path, [string]$Label)
    Replace-ImportSource -Path $Path -OldSource './core/adminApiClient' -NewSource '../../../../api/core/adminApiClient' -Label ($Label + ' adminApiClient import')
    Replace-ImportSource -Path $Path -OldSource './core/client' -NewSource '../../../../api/core/client' -Label ($Label + ' core client import')
    Replace-ImportSource -Path $Path -OldSource './adminApiClient' -NewSource '../../../../api/adminApiClient' -Label ($Label + ' root adminApiClient import')
}

$repoRoot = Get-RepoRoot
$adminSrc = Join-Parts -Parts @($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
if (-not (Test-Path -Path $adminSrc -PathType Container)) { throw ('Canonical Admin Web src folder was not found: {0}' -f $adminSrc) }
$appPath = Join-Parts -Parts @($adminSrc, 'App.tsx')
if (-not (Test-Path -Path $appPath -PathType Leaf)) { throw ('Canonical Admin Web App.tsx was not found: {0}' -f $appPath) }

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
    $pagesDir = Join-Path $featureRoot 'pages'
    $apiDir = Join-Path $featureRoot 'api'
    $typesDir = Join-Path $featureRoot 'types'
    Ensure-Directory -Path $pagesDir
    Ensure-Directory -Path $apiDir
    Ensure-Directory -Path $typesDir

    $pageSource = Join-Parts -Parts @($adminSrc, 'pages', ($feature.Page + '.tsx'))
    $pageTarget = Join-Parts -Parts @($pagesDir, ($feature.Page + '.tsx'))
    Move-IfPresent -Source $pageSource -Target $pageTarget -Label ($feature.Name + ' page') | Out-Null
    Normalize-PageImports -Path $pageTarget -Label ($feature.Name + ' page')

    foreach ($cssBase in @($feature.Css)) {
        Move-IfPresent -Source (Join-Parts -Parts @($adminSrc, 'pages', ($cssBase + '.css'))) -Target (Join-Parts -Parts @($pagesDir, ($cssBase + '.css'))) -Label ($feature.Name + ' css') | Out-Null
    }
    foreach ($apiBase in @($feature.Api)) {
        $apiTarget = Join-Parts -Parts @($apiDir, ($apiBase + '.ts'))
        Move-IfPresent -Source (Join-Parts -Parts @($adminSrc, 'api', ($apiBase + '.ts'))) -Target $apiTarget -Label ($feature.Name + ' API ' + $apiBase) | Out-Null
        Normalize-ApiImports -Path $apiTarget -Label ($feature.Name + ' API ' + $apiBase)
    }
    foreach ($typeBase in @($feature.Types)) {
        Move-IfPresent -Source (Join-Parts -Parts @($adminSrc, 'types', ($typeBase + '.ts'))) -Target (Join-Parts -Parts @($typesDir, ($typeBase + '.ts'))) -Label ($feature.Name + ' types ' + $typeBase) | Out-Null
    }
    Replace-ImportSource -Path $appPath -OldSource ('./pages/' + $feature.Page) -NewSource ('./features/' + $feature.Domain + '/' + $feature.Feature + '/pages/' + $feature.Page) -Label ('App.tsx ' + $feature.Page + ' import')
}
Write-Host 'P10.2AS Admin Web residual API/types feature batch move applied.'
