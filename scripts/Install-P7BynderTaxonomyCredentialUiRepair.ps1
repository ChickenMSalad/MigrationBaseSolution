[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RepoRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Require-File {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Description
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw ($Description + ' not found: ' + $Path)
    }
}

function Copy-PayloadFile {
    param(
        [Parameter(Mandatory = $true)][string]$RelativePath
    )

    $packageRoot = Split-Path -Parent $PSScriptRoot
    $source = Join-Path (Join-Path $packageRoot '_payload') $RelativePath
    $target = Join-Path $RepoRoot $RelativePath

    Require-File -Path $source -Description 'Payload file'
    $targetDir = Split-Path -Parent $target
    if (-not (Test-Path -LiteralPath $targetDir -PathType Container)) {
        New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
    }

    if (Test-Path -LiteralPath $target -PathType Leaf) {
        $backup = $target + '.p7-bynder-taxonomy-ui-repair.bak'
        Copy-Item -LiteralPath $target -Destination $backup -Force
        Write-Host ('Backed up ' + $target)
    }

    Copy-Item -LiteralPath $source -Destination $target -Force
    Write-Host ('Installed ' + $RelativePath)
}

if (-not (Test-Path -LiteralPath $RepoRoot -PathType Container)) {
    throw ('RepoRoot does not exist: ' + $RepoRoot)
}

$files = @(
    'src\Core\Migration.Admin.Api\Endpoints\TaxonomyBuilderEndpoints.cs',
    'src\Admin\Migration.Admin.Web\src\features\security\credentials\pages\Credentials.tsx',
    'src\Admin\Migration.Admin.Web\src\features\platform\builders\taxonomy\pages\TaxonomyBuilder.tsx',
    'src\Admin\Migration.Admin.Web\src\features\platform\builders\mapping\pages\MappingBuilder.tsx',
    'src\Admin\Migration.Admin.Web\src\components\LoadingError.tsx',
    'src\Admin\Migration.Admin.Web\src\features\platform\builders\manifest\pages\ManifestBuilder.tsx'
)

foreach ($file in $files) {
    Copy-PayloadFile -RelativePath $file
}

$oldTaxonomyBackup = Join-Path $RepoRoot 'src\Admin\Migration.Admin.Web\src\features\platform\builders\taxonomy\pages\TaxonomyBuilder.tsx.p7-taxonomy-json-post.bak'
if (Test-Path -LiteralPath $oldTaxonomyBackup -PathType Leaf) {
    Remove-Item -LiteralPath $oldTaxonomyBackup -Force
    Write-Host ('Removed stale backup file ' + $oldTaxonomyBackup)
}

Write-Host 'Bynder taxonomy credential/UI repair installed.'
