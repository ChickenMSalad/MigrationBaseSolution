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

function Read-TextFile {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Required file was not found: {0}' -f $Path)
    }

    return [System.IO.File]::ReadAllText($Path)
}

function Assert-NoImportSource {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Pattern,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $content = Read-TextFile -Path $Path
    if ([System.Text.RegularExpressions.Regex]::IsMatch($content, $Pattern, [System.Text.RegularExpressions.RegexOptions]::Multiline)) {
        throw ('Unexpected legacy import source found for {0}: {1}' -f $Label, $Path)
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

$appContent = Read-TextFile -Path $appPath
$movedCount = 0

foreach ($spec in $moveSpecs) {
    $source = Join-Path -Path $pagesRoot -ChildPath $spec.SourceFile
    $targetDirectory = Join-RepoPath -Root $adminSrc -Segments $spec.TargetSegments
    $target = Join-Path -Path $targetDirectory -ChildPath $spec.SourceFile

    $sourceExists = Test-Path -Path $source -PathType Leaf
    $targetExists = Test-Path -Path $target -PathType Leaf

    if ($sourceExists -and $targetExists) {
        throw ('Duplicate source and target remain for {0}. Source={1} Target={2}' -f $spec.Label, $source, $target)
    }

    if ($targetExists) {
        $movedCount++

        if ($spec.SourceFile -like '*.tsx') {
            Assert-NoImportSource -Path $target -Pattern 'from\s+[''"]\.\./(api|types|components|auth|lib|styles)/' -Label $spec.Label
        }

        if (-not [string]::IsNullOrWhiteSpace($spec.PageName)) {
            $flatPattern = 'from\s+[''"]\./pages/' + [System.Text.RegularExpressions.Regex]::Escape($spec.PageName) + '[''"]'
            if ([System.Text.RegularExpressions.Regex]::IsMatch($appContent, $flatPattern)) {
                throw ('App.tsx still imports flat page for {0}' -f $spec.PageName)
            }
        }
    }
}

if ($movedCount -eq 0) {
    throw 'No P10.2AR target files were found. Run the apply script first or verify this package matches the local checkout.'
}

Write-Host ('P10.2AR validation passed. Moved-or-present target files found: {0}' -f $movedCount)
