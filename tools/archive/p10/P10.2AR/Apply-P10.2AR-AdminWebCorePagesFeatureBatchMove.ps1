Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $current = (Get-Location).Path
    while ($true) {
        if (Test-Path -Path (Join-Path $current '.git') -PathType Container) {
            return $current
        }

        $parent = Split-Path -Path $current -Parent
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $current) {
            throw 'Unable to locate repository root. Run this script from inside the MigrationBaseSolution repository.'
        }

        $current = $parent
    }
}

function Join-RepoPath {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string[]]$Segments
    )

    $path = $Root
    foreach ($segment in $Segments) {
        $path = Join-Path -Path $path -ChildPath $segment
    }

    return $path
}

function Ensure-Directory {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -Path $Path -PathType Container)) {
        New-Item -Path $Path -ItemType Directory -Force | Out-Null
    }
}

function Read-TextFile {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Required file was not found: {0}' -f $Path)
    }

    return [System.IO.File]::ReadAllText($Path)
}

function Write-TextFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Content
    )

    [System.IO.File]::WriteAllText($Path, $Content, (New-Object -TypeName System.Text.UTF8Encoding -ArgumentList $false))
}

function Normalize-PageImports {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label
    )

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        return
    }

    $content = Read-TextFile -Path $Path
    $updated = $content

    $updated = $updated -replace "from '((\.\./)+)api/", "from '../../../../api/"
    $updated = $updated -replace 'from "((\.\./)+)api/', 'from "../../../../api/'
    $updated = $updated -replace "from '((\.\./)+)types/", "from '../../../../types/"
    $updated = $updated -replace 'from "((\.\./)+)types/', 'from "../../../../types/'
    $updated = $updated -replace "from '((\.\./)+)components/", "from '../../../../components/"
    $updated = $updated -replace 'from "((\.\./)+)components/', 'from "../../../../components/'
    $updated = $updated -replace "from '((\.\./)+)auth/", "from '../../../../auth/"
    $updated = $updated -replace 'from "((\.\./)+)auth/', 'from "../../../../auth/'
    $updated = $updated -replace "from '((\.\./)+)lib/", "from '../../../../lib/"
    $updated = $updated -replace 'from "((\.\./)+)lib/', 'from "../../../../lib/'
    $updated = $updated -replace "from '((\.\./)+)styles/", "from '../../../../styles/"
    $updated = $updated -replace 'from "((\.\./)+)styles/', 'from "../../../../styles/'

    if ($updated -ne $content) {
        Write-TextFile -Path $Path -Content $updated
        Write-Host ('Normalized imports for {0}: {1}' -f $Label, $Path)
    }
    else {
        Write-Host ('No import normalization needed for {0}: {1}' -f $Label, $Path)
    }
}

function Update-AppImport {
    param(
        [Parameter(Mandatory = $true)][string]$AppPath,
        [Parameter(Mandatory = $true)][string]$PageName,
        [Parameter(Mandatory = $true)][string]$FeatureImport
    )

    if (-not (Test-Path -Path $AppPath -PathType Leaf)) {
        throw ('App.tsx was not found: {0}' -f $AppPath)
    }

    $content = Read-TextFile -Path $AppPath
    $updated = $content
    $escaped = [System.Text.RegularExpressions.Regex]::Escape($PageName)
    $pattern = 'from\s+([''"])\./pages/' + $escaped + '\1'
    $replacement = ('from ''{0}''' -f $FeatureImport)
    $updated = [System.Text.RegularExpressions.Regex]::Replace($updated, $pattern, $replacement)

    if ($updated -ne $content) {
        Write-TextFile -Path $AppPath -Content $updated
        Write-Host ('Updated App.tsx import for {0}: {1}' -f $PageName, $FeatureImport)
    }
    else {
        Write-Host ('No App.tsx flat import found for {0}; no route import update needed.' -f $PageName)
    }
}

$repoRoot = Get-RepoRoot
$adminSrc = Join-RepoPath -Root $repoRoot -Segments @('src', 'Admin', 'Migration.Admin.Web', 'src')
$pagesRoot = Join-Path -Path $adminSrc -ChildPath 'pages'
$appPath = Join-Path -Path $adminSrc -ChildPath 'App.tsx'

if (-not (Test-Path -Path $adminSrc -PathType Container)) {
    throw ('Canonical Admin Web src folder was not found: {0}' -f $adminSrc)
}

$moveSpecs = @(
    [pscustomobject]@{ Label = 'Artifacts'; PageName = 'Artifacts'; SourceFile = 'Artifacts.tsx'; TargetSegments = @('features', 'platform', 'artifacts', 'pages'); FeatureImport = './features/platform/artifacts/pages/Artifacts' },
    [pscustomobject]@{ Label = 'Connectors'; PageName = 'Connectors'; SourceFile = 'Connectors.tsx'; TargetSegments = @('features', 'connectors', 'catalog', 'pages'); FeatureImport = './features/connectors/catalog/pages/Connectors' },
    [pscustomobject]@{ Label = 'Credentials'; PageName = 'Credentials'; SourceFile = 'Credentials.tsx'; TargetSegments = @('features', 'security', 'credentials', 'pages'); FeatureImport = './features/security/credentials/pages/Credentials' },
    [pscustomobject]@{ Label = 'Credentials styles'; PageName = ''; SourceFile = 'Credentials.css'; TargetSegments = @('features', 'security', 'credentials', 'pages'); FeatureImport = '' },
    [pscustomobject]@{ Label = 'Dashboard'; PageName = 'Dashboard'; SourceFile = 'Dashboard.tsx'; TargetSegments = @('features', 'platform', 'dashboard', 'pages'); FeatureImport = './features/platform/dashboard/pages/Dashboard' },
    [pscustomobject]@{ Label = 'Manifest Builder'; PageName = 'ManifestBuilder'; SourceFile = 'ManifestBuilder.tsx'; TargetSegments = @('features', 'governance', 'manifestBuilder', 'pages'); FeatureImport = './features/governance/manifestBuilder/pages/ManifestBuilder' },
    [pscustomobject]@{ Label = 'Mapping Builder'; PageName = 'MappingBuilder'; SourceFile = 'MappingBuilder.tsx'; TargetSegments = @('features', 'governance', 'mappingBuilder', 'pages'); FeatureImport = './features/governance/mappingBuilder/pages/MappingBuilder' },
    [pscustomobject]@{ Label = 'Preflight'; PageName = 'Preflight'; SourceFile = 'Preflight.tsx'; TargetSegments = @('features', 'operations', 'preflight', 'pages'); FeatureImport = './features/operations/preflight/pages/Preflight' },
    [pscustomobject]@{ Label = 'Projects'; PageName = 'Projects'; SourceFile = 'Projects.tsx'; TargetSegments = @('features', 'platform', 'projects', 'pages'); FeatureImport = './features/platform/projects/pages/Projects' },
    [pscustomobject]@{ Label = 'Project Detail'; PageName = 'ProjectDetail'; SourceFile = 'ProjectDetail.tsx'; TargetSegments = @('features', 'platform', 'projects', 'pages'); FeatureImport = './features/platform/projects/pages/ProjectDetail' },
    [pscustomobject]@{ Label = 'Runs'; PageName = 'Runs'; SourceFile = 'Runs.tsx'; TargetSegments = @('features', 'operations', 'runs', 'pages'); FeatureImport = './features/operations/runs/pages/Runs' },
    [pscustomobject]@{ Label = 'Run Detail'; PageName = 'RunDetail'; SourceFile = 'RunDetail.tsx'; TargetSegments = @('features', 'operations', 'runs', 'pages'); FeatureImport = './features/operations/runs/pages/RunDetail' },
    [pscustomobject]@{ Label = 'Taxonomy Builder'; PageName = 'TaxonomyBuilder'; SourceFile = 'TaxonomyBuilder.tsx'; TargetSegments = @('features', 'governance', 'taxonomyBuilder', 'pages'); FeatureImport = './features/governance/taxonomyBuilder/pages/TaxonomyBuilder' }
)

foreach ($spec in $moveSpecs) {
    $source = Join-Path -Path $pagesRoot -ChildPath $spec.SourceFile
    $targetDirectory = Join-RepoPath -Root $adminSrc -Segments $spec.TargetSegments
    $target = Join-Path -Path $targetDirectory -ChildPath $spec.SourceFile

    $sourceExists = Test-Path -Path $source -PathType Leaf
    $targetExists = Test-Path -Path $target -PathType Leaf

    if ($sourceExists -and $targetExists) {
        throw ('Both source and target exist for {0}. Resolve duplicate before continuing. Source={1} Target={2}' -f $spec.Label, $source, $target)
    }

    if ($sourceExists) {
        Ensure-Directory -Path $targetDirectory
        Move-Item -Path $source -Destination $target
        Write-Host ('Moved {0}: {1}' -f $spec.Label, $target)
    }
    elseif ($targetExists) {
        Write-Host ('Already moved {0}: {1}' -f $spec.Label, $target)
    }
    else {
        Write-Host ('Skipped {0}; source was not present and target was not present.' -f $spec.Label)
        continue
    }

    if ($spec.SourceFile -like '*.tsx') {
        Normalize-PageImports -Path $target -Label $spec.Label
    }

    if (-not [string]::IsNullOrWhiteSpace($spec.PageName)) {
        Update-AppImport -AppPath $appPath -PageName $spec.PageName -FeatureImport $spec.FeatureImport
    }
}

Write-Host 'P10.2AR Admin Web core pages feature batch move applied.'
