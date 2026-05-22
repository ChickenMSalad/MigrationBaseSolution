param(
    [switch]$Apply
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Write-Step([string]$Message) {
    Write-Host "[P4.1] $Message" -ForegroundColor Cyan
}

function Require-Path([string]$Path, [string]$Message) {
    if (-not (Test-Path -LiteralPath $Path)) {
        throw $Message
    }
}

function Copy-Directory([string]$Source, [string]$Destination) {
    if (Test-Path -LiteralPath $Destination) {
        throw "Destination already exists: $Destination. Review manually before applying P4.1."
    }

    if ($Apply) {
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $Destination) | Out-Null
        Copy-Item -Path $Source -Destination $Destination -Recurse -Force
    }

    Write-Step "$($(if ($Apply) { 'Copied' } else { 'WOULD copy' })) $Source -> $Destination"
}

function Ensure-PackageVersion([xml]$Xml, [string]$PackageName, [string]$Version) {
    $ns = $Xml.DocumentElement.NamespaceURI
    $nodes = $Xml.GetElementsByTagName('PackageVersion')
    foreach ($node in $nodes) {
        if ($node.Include -eq $PackageName) {
            return $false
        }
    }

    $itemGroup = $null
    foreach ($group in $Xml.Project.ItemGroup) {
        if ($null -ne $group.PackageVersion) {
            $itemGroup = $group
            break
        }
    }

    if ($null -eq $itemGroup) {
        $itemGroup = $Xml.CreateElement('ItemGroup', $ns)
        [void]$Xml.Project.AppendChild($itemGroup)
    }

    $packageVersion = $Xml.CreateElement('PackageVersion', $ns)
    $include = $Xml.CreateAttribute('Include')
    $include.Value = $PackageName
    [void]$packageVersion.Attributes.Append($include)

    $versionAttr = $Xml.CreateAttribute('Version')
    $versionAttr.Value = $Version
    [void]$packageVersion.Attributes.Append($versionAttr)

    [void]$itemGroup.AppendChild($packageVersion)
    return $true
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$payloadRoot = Join-Path $repoRoot 'payload'
$solutionPath = Join-Path $repoRoot 'MigrationBaseSolution.sln'
$corePath = Join-Path $repoRoot 'src\Core'
$manifestsPath = Join-Path $repoRoot 'src\Manifests'
$packagesPath = Join-Path $repoRoot 'Directory.Packages.props'
$sqlProjectSource = Join-Path $payloadRoot 'src\Core\Migration.Infrastructure.Sql'
$sqlProjectDestination = Join-Path $repoRoot 'src\Core\Migration.Infrastructure.Sql'
$sqlSchemaSource = Join-Path $payloadRoot 'database\sql\operational'
$sqlSchemaDestination = Join-Path $repoRoot 'database\sql\operational'

Write-Step "Repo root: $repoRoot"
Require-Path $solutionPath 'MigrationBaseSolution.sln was not found at repo root.'
Require-Path $corePath 'Expected physical Core folder was not found at src/Core.'
Require-Path $manifestsPath 'Expected physical Manifests folder was not found at src/Manifests.'
Require-Path (Join-Path $corePath 'Migration.Domain\Migration.Domain.csproj') 'Expected src/Core/Migration.Domain/Migration.Domain.csproj.'
Require-Path (Join-Path $corePath 'Migration.Application\Migration.Application.csproj') 'Expected src/Core/Migration.Application/Migration.Application.csproj.'
Require-Path (Join-Path $corePath 'Migration.Infrastructure\Migration.Infrastructure.csproj') 'Expected src/Core/Migration.Infrastructure/Migration.Infrastructure.csproj.'
Require-Path (Join-Path $corePath 'Migration.Shared\Migration.Shared.csproj') 'Expected src/Core/Migration.Shared/Migration.Shared.csproj.'
Require-Path $packagesPath 'Directory.Packages.props was not found at repo root.'
Require-Path $sqlProjectSource 'Payload SQL infrastructure project was not found.'
Require-Path $sqlSchemaSource 'Payload SQL schema folder was not found.'

Copy-Directory $sqlProjectSource $sqlProjectDestination

if (-not (Test-Path -LiteralPath $sqlSchemaDestination)) {
    if ($Apply) {
        New-Item -ItemType Directory -Force -Path $sqlSchemaDestination | Out-Null
    }
    Write-Step "$($(if ($Apply) { 'Created' } else { 'WOULD create' })) $sqlSchemaDestination"
}

Get-ChildItem -LiteralPath $sqlSchemaSource -File | ForEach-Object {
    $target = Join-Path $sqlSchemaDestination $_.Name
    if (Test-Path -LiteralPath $target) {
        Write-Step "Skipped existing schema file: $target"
    }
    else {
        if ($Apply) {
            Copy-Item -LiteralPath $_.FullName -Destination $target -Force
        }
        Write-Step "$($(if ($Apply) { 'Copied' } else { 'WOULD copy' })) $($_.FullName) -> $target"
    }
}

[xml]$packages = Get-Content -LiteralPath $packagesPath -Raw
$packageUpdates = @(
    @{ Name = 'Dapper'; Version = '2.1.66' },
    @{ Name = 'Microsoft.Data.SqlClient'; Version = '5.2.2' },
    @{ Name = 'Microsoft.Extensions.Configuration.Abstractions'; Version = '8.0.0' },
    @{ Name = 'Microsoft.Extensions.DependencyInjection.Abstractions'; Version = '8.0.2' },
    @{ Name = 'Microsoft.Extensions.Options'; Version = '8.0.2' },
    @{ Name = 'Microsoft.Extensions.Options.ConfigurationExtensions'; Version = '8.0.0' }
)

$changedPackages = $false
foreach ($package in $packageUpdates) {
    $added = Ensure-PackageVersion -Xml $packages -PackageName $package.Name -Version $package.Version
    if ($added) {
        $changedPackages = $true
        Write-Step "$($(if ($Apply) { 'Added' } else { 'WOULD add' })) PackageVersion $($package.Name) $($package.Version)"
    }
    else {
        Write-Step "PackageVersion already exists: $($package.Name)"
    }
}

if ($changedPackages -and $Apply) {
    $settings = New-Object System.Xml.XmlWriterSettings
    $settings.Indent = $true
    $settings.NewLineChars = "`r`n"
    $writer = [System.Xml.XmlWriter]::Create($packagesPath, $settings)
    try {
        $packages.Save($writer)
    }
    finally {
        $writer.Dispose()
    }
}

$projectPath = '.\src\Core\Migration.Infrastructure.Sql\Migration.Infrastructure.Sql.csproj'
if ($Apply) {
    dotnet sln $solutionPath add $projectPath --solution-folder Core
}
else {
    Write-Step "WOULD run: dotnet sln MigrationBaseSolution.sln add $projectPath --solution-folder Core"
}

Write-Step "Complete. Next: dotnet restore; dotnet build"
