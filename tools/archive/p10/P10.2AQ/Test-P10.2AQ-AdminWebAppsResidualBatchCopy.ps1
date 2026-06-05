Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $current = Get-Location
    while ($null -ne $current) {
        $gitPath = [System.IO.Path]::Combine($current.Path, '.git')
        $slnPath = [System.IO.Path]::Combine($current.Path, 'MigrationBaseSolution.sln')
        if ((Test-Path -LiteralPath $gitPath) -or (Test-Path -LiteralPath $slnPath -PathType Leaf)) {
            return $current.Path
        }
        $current = $current.Parent
    }
    throw 'Unable to locate repository root. Run this script from inside the MigrationBaseSolution repository.'
}

function Get-RelativePath {
    param(
        [Parameter(Mandatory=$true)][string]$BasePath,
        [Parameter(Mandatory=$true)][string]$FullPath
    )
    $base = [System.IO.Path]::GetFullPath($BasePath).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $full = [System.IO.Path]::GetFullPath($FullPath)
    $baseUri = New-Object System.Uri(($base + [System.IO.Path]::DirectorySeparatorChar))
    $fullUri = New-Object System.Uri($full)
    $relative = $baseUri.MakeRelativeUri($fullUri).ToString()
    return [System.Uri]::UnescapeDataString($relative).Replace('/', [System.IO.Path]::DirectorySeparatorChar)
}

function Assert-PathExists {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$Label,
        [Parameter(Mandatory=$true)][string]$PathType
    )
    if (-not (Test-Path -LiteralPath $Path -PathType $PathType)) {
        throw ('Expected {0} missing: {1}' -f $Label, $Path)
    }
}

$repoRoot = Get-RepoRoot
$appsSourceRoot = [System.IO.Path]::Combine($repoRoot, 'apps', 'migration-admin-ui', 'src')
$adminSourceRoot = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$reportPath = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10', 'P10.2AQ-AdminWebAppsResidualBatchCopy.LocalReport.md')

Assert-PathExists -Path $appsSourceRoot -Label 'reference app source root' -PathType 'Container'
Assert-PathExists -Path $adminSourceRoot -Label 'canonical Admin Web source root' -PathType 'Container'
Assert-PathExists -Path $reportPath -Label 'local report' -PathType 'Leaf'

$families = @(
    [pscustomobject]@{ Name = 'features' },
    [pscustomobject]@{ Name = 'components' },
    [pscustomobject]@{ Name = 'auth' },
    [pscustomobject]@{ Name = 'lib' }
)

foreach ($family in $families) {
    $sourceFamilyRoot = [System.IO.Path]::Combine($appsSourceRoot, $family.Name)
    $targetFamilyRoot = [System.IO.Path]::Combine($adminSourceRoot, $family.Name)
    if (-not (Test-Path -LiteralPath $sourceFamilyRoot -PathType Container)) {
        continue
    }
    Assert-PathExists -Path $targetFamilyRoot -Label ('canonical family ' + $family.Name) -PathType 'Container'
    $sourceFiles = @(Get-ChildItem -LiteralPath $sourceFamilyRoot -Recurse -File | Sort-Object FullName)
    foreach ($sourceFile in $sourceFiles) {
        $relative = Get-RelativePath -BasePath $sourceFamilyRoot -FullPath $sourceFile.FullName
        $targetPath = [System.IO.Path]::Combine($targetFamilyRoot, $relative)
        Assert-PathExists -Path $targetPath -Label ('copied or existing app parity file ' + $family.Name) -PathType 'Leaf'
    }
}

$forbiddenTargets = @(
    [pscustomobject]@{ Path = [System.IO.Path]::Combine($adminSourceRoot, 'package.json') },
    [pscustomobject]@{ Path = [System.IO.Path]::Combine($adminSourceRoot, 'vite.config.ts') },
    [pscustomobject]@{ Path = [System.IO.Path]::Combine($adminSourceRoot, 'tsconfig.json') }
)
foreach ($item in $forbiddenTargets) {
    if (Test-Path -LiteralPath $item.Path -PathType Leaf) {
        throw ('Unexpected copied app-level file under canonical src: {0}' -f $item.Path)
    }
}

Write-Host 'P10.2AQ residual app source batch copy validation passed.'
