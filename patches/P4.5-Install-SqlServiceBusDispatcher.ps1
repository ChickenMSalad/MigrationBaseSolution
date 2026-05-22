[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [switch]$Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Step {
    param([string]$Message)
    Write-Host "[P4.5] $Message" -ForegroundColor Cyan
}

function Get-RepoRoot {
    $current = Get-Location
    while ($null -ne $current) {
        if (Test-Path -LiteralPath (Join-Path $current.Path 'MigrationBaseSolution.sln')) {
            return $current.Path
        }

        $parent = Split-Path -Parent $current.Path
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $current.Path) {
            break
        }

        $current = Get-Item -LiteralPath $parent
    }

    throw 'Could not locate MigrationBaseSolution.sln. Run this script from the repository root or a child folder.'
}

function Invoke-Change {
    param(
        [string]$Description,
        [scriptblock]$Action
    )

    if (-not $Apply) {
        Write-Step "WOULD $Description"
        return
    }

    if ($PSCmdlet.ShouldProcess($Description)) {
        & $Action
        Write-Step $Description
    }
}

function Ensure-PackageVersion {
    param(
        [xml]$Xml,
        [string]$PackageName,
        [string]$Version
    )

    $nodes = @($Xml.Project.ItemGroup | ForEach-Object {
        if ($_.PSObject.Properties.Name -contains 'PackageVersion') {
            @($_.PackageVersion)
        }
    } | Where-Object { $null -ne $_ })

    foreach ($node in $nodes) {
        if ($node.Include -eq $PackageName) {
            Write-Step "PackageVersion already exists: $PackageName"
            return $false
        }
    }

    $itemGroup = $null
    foreach ($group in @($Xml.Project.ItemGroup)) {
        if ($group.PSObject.Properties.Name -contains 'PackageVersion') {
            $itemGroup = $group
            break
        }
    }

    if ($null -eq $itemGroup) {
        $itemGroup = $Xml.CreateElement('ItemGroup')
        [void]$Xml.Project.AppendChild($itemGroup)
    }

    $packageVersion = $Xml.CreateElement('PackageVersion')
    $includeAttribute = $Xml.CreateAttribute('Include')
    $includeAttribute.Value = $PackageName
    [void]$packageVersion.Attributes.Append($includeAttribute)

    $versionAttribute = $Xml.CreateAttribute('Version')
    $versionAttribute.Value = $Version
    [void]$packageVersion.Attributes.Append($versionAttribute)

    [void]$itemGroup.AppendChild($packageVersion)
    Write-Step "Added PackageVersion $PackageName $Version"
    return $true
}

$repoRoot = Get-RepoRoot
Set-Location -LiteralPath $repoRoot
Write-Step "Repo root: $repoRoot"

$payloadRoot = Join-Path $repoRoot 'payload'
if (-not (Test-Path -LiteralPath $payloadRoot)) {
    throw "Payload folder not found: $payloadRoot"
}

$workerSource = Join-Path $payloadRoot 'src\Workers\Migration.Workers.ServiceBusDispatcher'
$workerTarget = Join-Path $repoRoot 'src\Workers\Migration.Workers.ServiceBusDispatcher'
$configSource = Join-Path $payloadRoot 'config-samples\appsettings.SqlServiceBusDispatcher.sample.json'
$configTarget = Join-Path $repoRoot 'config-samples\appsettings.SqlServiceBusDispatcher.sample.json'
$docSource = Join-Path $payloadRoot 'docs\cloud-roadmap-cleanup\P4_SET_005_SQL_SERVICE_BUS_DISPATCHER.md'
$docTarget = Join-Path $repoRoot 'docs\cloud-roadmap-cleanup\P4_SET_005_SQL_SERVICE_BUS_DISPATCHER.md'

if (-not (Test-Path -LiteralPath $workerSource)) {
    throw "Worker payload not found: $workerSource"
}

Invoke-Change "copy $workerSource -> $workerTarget" {
    if (Test-Path -LiteralPath $workerTarget) {
        throw "Target already exists: $workerTarget"
    }

    New-Item -ItemType Directory -Path (Split-Path -Parent $workerTarget) -Force | Out-Null
    Copy-Item -LiteralPath $workerSource -Destination $workerTarget -Recurse -Force
}

Invoke-Change "copy dispatcher config sample" {
    New-Item -ItemType Directory -Path (Split-Path -Parent $configTarget) -Force | Out-Null
    Copy-Item -LiteralPath $configSource -Destination $configTarget -Force
}

Invoke-Change "copy dispatcher documentation" {
    New-Item -ItemType Directory -Path (Split-Path -Parent $docTarget) -Force | Out-Null
    Copy-Item -LiteralPath $docSource -Destination $docTarget -Force
}

$directoryPackagesPath = Join-Path $repoRoot 'Directory.Packages.props'
if (-not (Test-Path -LiteralPath $directoryPackagesPath)) {
    throw "Directory.Packages.props not found: $directoryPackagesPath"
}

[xml]$directoryPackages = Get-Content -LiteralPath $directoryPackagesPath -Raw
$changedPackages = $false
$changedPackages = (Ensure-PackageVersion -Xml $directoryPackages -PackageName 'Azure.Messaging.ServiceBus' -Version '7.18.4') -or $changedPackages
$changedPackages = (Ensure-PackageVersion -Xml $directoryPackages -PackageName 'Microsoft.Extensions.Configuration.Binder' -Version '8.0.2') -or $changedPackages
$changedPackages = (Ensure-PackageVersion -Xml $directoryPackages -PackageName 'Microsoft.Extensions.Hosting' -Version '8.0.1') -or $changedPackages

if ($changedPackages) {
    Invoke-Change "update Directory.Packages.props" {
        $directoryPackages.Save($directoryPackagesPath)
    }
}

$slnPath = Join-Path $repoRoot 'MigrationBaseSolution.sln'
$projectRelativePath = '.\src\Workers\Migration.Workers.ServiceBusDispatcher\Migration.Workers.ServiceBusDispatcher.csproj'
$projectPath = Join-Path $repoRoot 'src\Workers\Migration.Workers.ServiceBusDispatcher\Migration.Workers.ServiceBusDispatcher.csproj'

if ($Apply -and -not (Test-Path -LiteralPath $projectPath)) {
    throw "Project file was not copied: $projectPath"
}

if (-not $Apply) {
    Write-Step "WOULD run: dotnet sln MigrationBaseSolution.sln add $projectRelativePath --solution-folder Workers"
}
else {
    $slnContent = Get-Content -LiteralPath $slnPath -Raw
    if ($slnContent -notmatch [regex]::Escape('Migration.Workers.ServiceBusDispatcher.csproj')) {
        & dotnet sln MigrationBaseSolution.sln add $projectRelativePath --solution-folder Workers
        if ($LASTEXITCODE -ne 0) {
            throw 'dotnet sln add failed.'
        }
    }
    else {
        Write-Step 'Solution already contains Migration.Workers.ServiceBusDispatcher.'
    }
}

Write-Step 'Complete. Next: ./patches/P4.5-Validate-SqlServiceBusDispatcher.ps1; dotnet restore; dotnet build'
