[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RepoRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-File {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw ('Required file not found: ' + $Path)
    }
}

function Backup-File {
    param([string]$Path)
    $stamp = Get-Date -Format 'yyyyMMddHHmmss'
    $backup = $Path + '.p7-bynder-taxonomy-compile.' + $stamp + '.bak'
    Copy-Item -LiteralPath $Path -Destination $backup -Force
    Write-Host ('Backed up ' + $Path)
}

function Get-ScriptRootSafe {
    if ($PSScriptRoot) { return $PSScriptRoot }
    if ($MyInvocation.MyCommand.Path) { return Split-Path -Parent $MyInvocation.MyCommand.Path }
    return (Get-Location).Path
}

function Ensure-ProjectReference {
    param(
        [string]$ProjectPath,
        [string]$IncludePath
    )

    [xml]$xml = Get-Content -LiteralPath $ProjectPath -Raw
    $project = $xml.Project
    if ($null -eq $project) { throw ('Invalid project file: ' + $ProjectPath) }

    $existing = @($xml.SelectNodes("//*[local-name()='ProjectReference']")) | Where-Object {
        $include = $_.GetAttribute('Include')
        [string]::Equals($include, $IncludePath, [System.StringComparison]::OrdinalIgnoreCase)
    }

    if ($existing.Count -gt 0) { return }

    $itemGroup = $xml.CreateElement('ItemGroup')
    $reference = $xml.CreateElement('ProjectReference')
    $reference.SetAttribute('Include', $IncludePath)
    [void]$itemGroup.AppendChild($reference)
    [void]$project.AppendChild($itemGroup)
    $xml.Save($ProjectPath)
    Write-Host ('Added ProjectReference: ' + $IncludePath)
}

function Ensure-PackageReference {
    param(
        [string]$ProjectPath,
        [string]$PackageName
    )

    [xml]$xml = Get-Content -LiteralPath $ProjectPath -Raw
    $project = $xml.Project
    if ($null -eq $project) { throw ('Invalid project file: ' + $ProjectPath) }

    $existing = @($xml.SelectNodes("//*[local-name()='PackageReference']")) | Where-Object {
        $include = $_.GetAttribute('Include')
        [string]::Equals($include, $PackageName, [System.StringComparison]::OrdinalIgnoreCase)
    }

    if ($existing.Count -gt 0) { return }

    $groups = @($xml.SelectNodes("//*[local-name()='ItemGroup']"))
    $itemGroup = $null
    foreach ($group in $groups) {
        $refs = @($group.SelectNodes("*[local-name()='PackageReference']"))
        if ($refs.Count -gt 0) { $itemGroup = $group; break }
    }

    if ($null -eq $itemGroup) {
        $itemGroup = $xml.CreateElement('ItemGroup')
        [void]$project.AppendChild($itemGroup)
    }

    $reference = $xml.CreateElement('PackageReference')
    $reference.SetAttribute('Include', $PackageName)
    [void]$itemGroup.AppendChild($reference)
    $xml.Save($ProjectPath)
    Write-Host ('Added PackageReference without inline Version: ' + $PackageName)
}

$repo = (Resolve-Path -LiteralPath $RepoRoot).Path
$scriptRoot = Get-ScriptRootSafe
$packRoot = Split-Path -Parent $scriptRoot

$target = Join-Path $repo 'src\Core\Migration.Admin.Api\Endpoints\TaxonomyBuilderEndpoints.cs'
$source = Join-Path $packRoot '_payload\src\Core\Migration.Admin.Api\Endpoints\TaxonomyBuilderEndpoints.cs'
$project = Join-Path $repo 'src\Core\Migration.Admin.Api\Migration.Admin.Api.csproj'
$bynderProject = '..\..\Connectors\Targets\Migration.Connectors.Targets.Bynder\Migration.Connectors.Targets.Bynder.csproj'

Assert-File -Path $target
Assert-File -Path $source
Assert-File -Path $project

Backup-File -Path $target
Copy-Item -LiteralPath $source -Destination $target -Force
Write-Host 'Installed repaired TaxonomyBuilderEndpoints.cs.'

Ensure-ProjectReference -ProjectPath $project -IncludePath $bynderProject
Ensure-PackageReference -ProjectPath $project -PackageName 'Azure.Identity'
Ensure-PackageReference -ProjectPath $project -PackageName 'Azure.Security.KeyVault.Secrets'

Write-Host 'Bynder taxonomy Key Vault compile repair installed.'
