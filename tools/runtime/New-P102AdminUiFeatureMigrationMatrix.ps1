[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $ConfigurationPath,

    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $OutputDirectory
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
        [ValidateNotNullOrEmpty()]
        [string] $Root,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $RelativePath
    )

    $normalized = $RelativePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar).Replace('\\', [System.IO.Path]::DirectorySeparatorChar)
    return [System.IO.Path]::Combine($Root, $normalized)
}

function Test-PathHasIgnoredSegment {
    param(
        [Parameter(Mandatory = $true)]
        [System.IO.FileInfo] $File
    )

    $segments = @($File.FullName.Split([System.IO.Path]::DirectorySeparatorChar, [System.StringSplitOptions]::RemoveEmptyEntries))
    foreach ($segment in $segments) {
        if ($segment -in @('node_modules', 'dist', 'build', '.git', '.vite', '.react-router')) {
            return $true
        }
    }

    return $false
}

function ConvertTo-RelativeRepoPath {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $RepoRoot,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $FullName
    )

    $rootWithSeparator = $RepoRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    if ($FullName.StartsWith($rootWithSeparator, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $FullName.Substring($rootWithSeparator.Length).Replace([System.IO.Path]::DirectorySeparatorChar, '/')
    }

    return $FullName.Replace([System.IO.Path]::DirectorySeparatorChar, '/')
}

$scriptRoot = Resolve-ScriptRoot
$repoRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)

if ([string]::IsNullOrWhiteSpace($ConfigurationPath)) {
    $ConfigurationPath = [System.IO.Path]::Combine($repoRoot, 'config-samples', 'p10-admin-ui-feature-migration-matrix.sample.json')
}
elseif (-not [System.IO.Path]::IsPathRooted($ConfigurationPath)) {
    $ConfigurationPath = [System.IO.Path]::Combine($repoRoot, $ConfigurationPath)
}

if (-not (Test-Path -LiteralPath $ConfigurationPath -PathType Leaf)) {
    throw ('Configuration file not found: {0}' -f $ConfigurationPath)
}

$config = Get-Content -LiteralPath $ConfigurationPath -Raw | ConvertFrom-Json
foreach ($propertyName in @('canonicalAdminUiPath', 'featureSourcePath', 'featureFamilies')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('Configuration is missing property: {0}' -f $propertyName)
    }
}

$canonicalPath = [string]$config.canonicalAdminUiPath
$sourcePath = [string]$config.featureSourcePath

if ($canonicalPath -ne 'src/Admin/Migration.Admin.Web') {
    throw 'canonicalAdminUiPath must remain src/Admin/Migration.Admin.Web.'
}

if ($sourcePath -ne 'apps/migration-admin-ui') {
    throw 'featureSourcePath must remain apps/migration-admin-ui.'
}

$sourceRoot = Join-RepoPath -Root $repoRoot -RelativePath $sourcePath
$canonicalRoot = Join-RepoPath -Root $repoRoot -RelativePath $canonicalPath

if (-not (Test-Path -LiteralPath $sourceRoot -PathType Container)) {
    throw ('Feature source path not found: {0}' -f $sourceRoot)
}

if (-not (Test-Path -LiteralPath $canonicalRoot -PathType Container)) {
    throw ('Canonical Admin UI path not found: {0}' -f $canonicalRoot)
}

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    if ($null -ne $config.PSObject.Properties['outputDirectory']) {
        $OutputDirectory = [string]$config.outputDirectory
    }
    else {
        $OutputDirectory = 'artifacts/admin-ui-consolidation'
    }
}

if (-not [System.IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory = [System.IO.Path]::Combine($repoRoot, $OutputDirectory)
}

if (-not (Test-Path -LiteralPath $OutputDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
}

$sourceFiles = Get-ChildItem -LiteralPath $sourceRoot -Recurse -File | Where-Object { -not (Test-PathHasIgnoredSegment -File $_) }
$canonicalFiles = Get-ChildItem -LiteralPath $canonicalRoot -Recurse -File | Where-Object { -not (Test-PathHasIgnoredSegment -File $_) }

$families = @()
foreach ($family in @($config.featureFamilies)) {
    $name = [string]$family.name
    $destination = [string]$family.canonicalDestination
    $sourceFragments = @($family.sourceFragments)

    $matched = @()
    foreach ($file in $sourceFiles) {
        $relative = ConvertTo-RelativeRepoPath -RepoRoot $repoRoot -FullName $file.FullName
        foreach ($fragment in $sourceFragments) {
            $normalizedFragment = ([string]$fragment).Replace('\\', '/').Replace('\', '/')
            if ($relative.IndexOf(('apps/migration-admin-ui/' + $normalizedFragment), [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                $matched += $relative
                break
            }
        }
    }

    $families += [pscustomobject]@{
        name = $name
        canonicalDestination = $destination
        sourceFileCount = @($matched).Count
        sourceFiles = @($matched | Sort-Object -Unique)
    }
}

$payload = [pscustomobject]@{
    generatedUtc = [DateTimeOffset]::UtcNow.ToString('o')
    canonicalAdminUiPath = $canonicalPath
    featureSourcePath = $sourcePath
    canonicalFileCount = @($canonicalFiles).Count
    featureSourceFileCount = @($sourceFiles).Count
    featureFamilies = @($families)
}

$jsonPath = [System.IO.Path]::Combine($OutputDirectory, 'p10.2f-admin-ui-feature-migration-matrix.json')
$mdPath = [System.IO.Path]::Combine($OutputDirectory, 'p10.2f-admin-ui-feature-migration-matrix.md')

$payload | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $jsonPath -Encoding UTF8

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('# P10.2F Admin UI Feature Migration Matrix') | Out-Null
$lines.Add('') | Out-Null
$lines.Add(('- Generated UTC: {0}' -f $payload.generatedUtc)) | Out-Null
$lines.Add(('- Canonical Admin UI: `{0}`' -f $canonicalPath)) | Out-Null
$lines.Add(('- Feature source: `{0}`' -f $sourcePath)) | Out-Null
$lines.Add(('- Canonical file count: {0}' -f $payload.canonicalFileCount)) | Out-Null
$lines.Add(('- Feature source file count: {0}' -f $payload.featureSourceFileCount)) | Out-Null
$lines.Add('') | Out-Null
$lines.Add('| Feature family | Source files | Canonical destination |') | Out-Null
$lines.Add('| --- | ---: | --- |') | Out-Null
foreach ($family in $families) {
    $lines.Add(('| {0} | {1} | `{2}` |' -f $family.name, $family.sourceFileCount, $family.canonicalDestination)) | Out-Null
}
$lines.Add('') | Out-Null
$lines.Add('## Source files by family') | Out-Null
foreach ($family in $families) {
    $lines.Add('') | Out-Null
    $lines.Add(('### {0}' -f $family.name)) | Out-Null
    if (@($family.sourceFiles).Count -eq 0) {
        $lines.Add('- No matching source files found.') | Out-Null
    }
    else {
        foreach ($sourceFile in $family.sourceFiles) {
            $lines.Add(('- `{0}`' -f $sourceFile)) | Out-Null
        }
    }
}

$lines | Set-Content -LiteralPath $mdPath -Encoding UTF8

Write-Host ('Wrote Admin UI feature migration matrix: {0}' -f $mdPath)
Write-Host ('Wrote Admin UI feature migration matrix JSON: {0}' -f $jsonPath)
