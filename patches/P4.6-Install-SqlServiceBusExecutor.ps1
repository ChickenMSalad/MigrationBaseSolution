[CmdletBinding()]
param(
    [switch] $Apply,
    [switch] $WhatIf
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Step {
    param([string] $Message)
    Write-Host "[P4.6] $Message"
}

function Get-RepoRoot {
    $scriptRoot = Split-Path -Parent $PSCommandPath
    return (Resolve-Path (Join-Path $scriptRoot '..')).Path
}

function Ensure-Directory {
    param([string] $Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        if ($Apply) {
            New-Item -ItemType Directory -Path $Path | Out-Null
            Write-Step "Created $Path"
        } else {
            Write-Step "WOULD create $Path"
        }
    }
}

function Copy-DirectorySafe {
    param([string] $Source, [string] $Destination)

    if (-not (Test-Path -LiteralPath $Source)) {
        throw "Payload source not found: $Source"
    }

    if (Test-Path -LiteralPath $Destination) {
        throw "Destination already exists. Refusing to overwrite: $Destination"
    }

    if ($Apply) {
        $parent = Split-Path -Parent $Destination
        Ensure-Directory $parent
        Copy-Item -Path $Source -Destination $Destination -Recurse
        Write-Step "Copied $Source -> $Destination"
    } else {
        Write-Step "WOULD copy $Source -> $Destination"
    }
}

function Load-XmlDocument {
    param([string] $Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "XML file not found: $Path"
    }

    [xml] $xml = Get-Content -LiteralPath $Path -Raw
    return $xml
}

function Save-XmlDocument {
    param([xml] $Xml, [string] $Path)
    $settings = New-Object System.Xml.XmlWriterSettings
    $settings.Indent = $true
    $settings.OmitXmlDeclaration = $false
    $writer = [System.Xml.XmlWriter]::Create($Path, $settings)
    try {
        $Xml.Save($writer)
    } finally {
        $writer.Close()
    }
}

function Ensure-PackageVersion {
    param([xml] $Xml, [string] $PropsPath, [string] $Name, [string] $Version)

    $existing = @()
    foreach ($itemGroup in @($Xml.Project.ItemGroup)) {
        if ($itemGroup.PSObject.Properties.Name -contains 'PackageVersion') {
            foreach ($packageVersion in @($itemGroup.PackageVersion)) {
                if ($packageVersion.Include -eq $Name) {
                    $existing += $packageVersion
                }
            }
        }
    }

    if ($existing.Count -gt 0) {
        Write-Step "PackageVersion already exists: $Name"
        return
    }

    if (-not ($Xml.Project.PSObject.Properties.Name -contains 'ItemGroup')) {
        $itemGroup = $Xml.CreateElement('ItemGroup')
        [void] $Xml.Project.AppendChild($itemGroup)
    } else {
        $itemGroup = @($Xml.Project.ItemGroup)[0]
    }

    if ($Apply) {
        $node = $Xml.CreateElement('PackageVersion')
        $include = $Xml.CreateAttribute('Include')
        $include.Value = $Name
        [void] $node.Attributes.Append($include)
        $versionAttribute = $Xml.CreateAttribute('Version')
        $versionAttribute.Value = $Version
        [void] $node.Attributes.Append($versionAttribute)
        [void] $itemGroup.AppendChild($node)
        Write-Step "Added PackageVersion $Name $Version"
    } else {
        Write-Step "WOULD add PackageVersion $Name $Version"
    }
}

function Ensure-SolutionProject {
    param([string] $RepoRoot, [string] $SolutionPath, [string] $ProjectPath)

    $relativeProject = '.\' + (Resolve-Path -LiteralPath $ProjectPath -Relative).TrimStart('.', '\', '/')
    $solutionList = & dotnet sln $SolutionPath list
    $normalizedProject = $ProjectPath.Replace('\', '/').ToLowerInvariant()
    $alreadyListed = $false

    foreach ($line in $solutionList) {
        $candidate = (Join-Path $RepoRoot $line.Trim()).Replace('\', '/').ToLowerInvariant()
        if ($candidate -eq $normalizedProject) {
            $alreadyListed = $true
            break
        }
    }

    if ($alreadyListed) {
        Write-Step "Solution already contains $ProjectPath"
        return
    }

    if ($Apply) {
        & dotnet sln $SolutionPath add $relativeProject --solution-folder Workers
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet sln add failed for $relativeProject"
        }
    } else {
        Write-Step "WOULD run: dotnet sln MigrationBaseSolution.sln add $relativeProject --solution-folder Workers"
    }
}

$repoRoot = Get-RepoRoot
Set-Location $repoRoot
Write-Step "Repo root: $repoRoot"

$payloadRoot = Join-Path $repoRoot 'payload'
$solutionPath = Join-Path $repoRoot 'MigrationBaseSolution.sln'
$propsPath = Join-Path $repoRoot 'Directory.Packages.props'
$projectSource = Join-Path $payloadRoot 'src\Workers\Migration.Workers.ServiceBusExecutor'
$projectDestination = Join-Path $repoRoot 'src\Workers\Migration.Workers.ServiceBusExecutor'
$configSource = Join-Path $payloadRoot 'config-samples\appsettings.SqlServiceBusExecutor.sample.json'
$configDestination = Join-Path $repoRoot 'config-samples\appsettings.SqlServiceBusExecutor.sample.json'
$docSource = Join-Path $payloadRoot 'docs\cloud-roadmap-cleanup\P4_SET_006_SQL_SERVICE_BUS_EXECUTOR.md'
$docDestination = Join-Path $repoRoot 'docs\cloud-roadmap-cleanup\P4_SET_006_SQL_SERVICE_BUS_EXECUTOR.md'

if (-not (Test-Path -LiteralPath (Join-Path $repoRoot 'src\Core\Migration.Application\Migration.Application.csproj'))) {
    throw 'Expected src\Core\Migration.Application\Migration.Application.csproj was not found.'
}

if (-not (Test-Path -LiteralPath (Join-Path $repoRoot 'src\Core\Migration.Infrastructure.Sql\Migration.Infrastructure.Sql.csproj'))) {
    throw 'Expected P4.1 project src\Core\Migration.Infrastructure.Sql\Migration.Infrastructure.Sql.csproj was not found.'
}

Copy-DirectorySafe $projectSource $projectDestination

Ensure-Directory (Split-Path -Parent $configDestination)
if (Test-Path -LiteralPath $configDestination) {
    Write-Step "Sample config already exists: $configDestination"
} elseif ($Apply) {
    Copy-Item -Path $configSource -Destination $configDestination
    Write-Step "Copied config sample"
} else {
    Write-Step "WOULD copy $configSource -> $configDestination"
}

Ensure-Directory (Split-Path -Parent $docDestination)
if (Test-Path -LiteralPath $docDestination) {
    Write-Step "Documentation already exists: $docDestination"
} elseif ($Apply) {
    Copy-Item -Path $docSource -Destination $docDestination
    Write-Step "Copied documentation"
} else {
    Write-Step "WOULD copy $docSource -> $docDestination"
}

$propsXml = Load-XmlDocument $propsPath
Ensure-PackageVersion $propsXml $propsPath 'Azure.Messaging.ServiceBus' '7.20.1'
Ensure-PackageVersion $propsXml $propsPath 'Microsoft.Extensions.Configuration.Binder' '9.0.5'
Ensure-PackageVersion $propsXml $propsPath 'Microsoft.Extensions.Hosting' '9.0.5'
Ensure-PackageVersion $propsXml $propsPath 'Microsoft.Extensions.Options.ConfigurationExtensions' '9.0.5'
if ($Apply) {
    Save-XmlDocument $propsXml $propsPath
}

if ($Apply) {
    $projectPath = Join-Path $projectDestination 'Migration.Workers.ServiceBusExecutor.csproj'
    Ensure-SolutionProject $repoRoot $solutionPath $projectPath
} else {
    Write-Step "WOULD run: dotnet sln MigrationBaseSolution.sln add .\src\Workers\Migration.Workers.ServiceBusExecutor\Migration.Workers.ServiceBusExecutor.csproj --solution-folder Workers"
}

Write-Step 'Complete. Next: ./patches/P4.6-Validate-SqlServiceBusExecutor.ps1; dotnet restore; dotnet build'
