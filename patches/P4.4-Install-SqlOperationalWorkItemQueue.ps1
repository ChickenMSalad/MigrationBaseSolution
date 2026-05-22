[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [switch]$Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-P44([string]$Message) {
    Write-Host "[P4.4] $Message"
}

function Get-RepoRoot {
    $current = (Get-Location).Path
    while ($true) {
        if (Test-Path -LiteralPath (Join-Path $current 'MigrationBaseSolution.sln')) {
            return $current
        }

        $parent = Split-Path -Parent $current
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $current) {
            throw 'Could not locate MigrationBaseSolution.sln. Run this script from the repo root or a child folder.'
        }

        $current = $parent
    }
}

function Copy-PayloadFile([string]$RepoRoot, [string]$RelativePath) {
    $source = Join-Path $RepoRoot (Join-Path 'payload' $RelativePath)
    $target = Join-Path $RepoRoot $RelativePath

    if (-not (Test-Path -LiteralPath $source)) {
        throw "Payload file not found: $source"
    }

    $targetDirectory = Split-Path -Parent $target
    if (-not (Test-Path -LiteralPath $targetDirectory)) {
        if ($Apply) {
            New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
        } else {
            Write-P44 "WOULD create directory $targetDirectory"
        }
    }

    if ($Apply) {
        Copy-Item -LiteralPath $source -Destination $target -Force
        Write-P44 "Copied $RelativePath"
    } else {
        Write-P44 "WOULD copy $source -> $target"
    }
}

function Load-XmlDocument([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "XML file not found: $Path"
    }

    [xml]$document = Get-Content -LiteralPath $Path -Raw
    return $document
}

function Save-XmlDocument($Document, [string]$Path) {
    $settings = New-Object System.Xml.XmlWriterSettings
    $settings.Indent = $true
    $settings.OmitXmlDeclaration = $false
    $writer = [System.Xml.XmlWriter]::Create($Path, $settings)
    try {
        $Document.Save($writer)
    }
    finally {
        $writer.Close()
    }
}

function Ensure-ProjectReference([string]$ProjectPath, [string]$ReferenceInclude) {
    $project = Load-XmlDocument $ProjectPath
    $root = $project.Project
    $exists = $false

    foreach ($itemGroup in @($root.ItemGroup)) {
        if ($itemGroup.PSObject.Properties.Name -contains 'ProjectReference') {
            foreach ($reference in @($itemGroup.ProjectReference)) {
                if ($reference.PSObject.Properties.Name -contains 'Include') {
                    if ($reference.Include -eq $ReferenceInclude) {
                        $exists = $true
                    }
                }
            }
        }
    }

    if ($exists) {
        Write-P44 "ProjectReference already exists in $ProjectPath -> $ReferenceInclude"
        return
    }

    if (-not $Apply) {
        Write-P44 "WOULD add ProjectReference $ReferenceInclude to $ProjectPath"
        return
    }

    $itemGroup = $project.CreateElement('ItemGroup')
    $reference = $project.CreateElement('ProjectReference')
    $include = $project.CreateAttribute('Include')
    $include.Value = $ReferenceInclude
    [void]$reference.Attributes.Append($include)
    [void]$itemGroup.AppendChild($reference)
    [void]$root.AppendChild($itemGroup)
    Save-XmlDocument $project $ProjectPath
    Write-P44 "Added ProjectReference $ReferenceInclude to $ProjectPath"
}

function Add-LineBeforeAppRun([string]$ProgramPath, [string]$Line) {
    if (-not (Test-Path -LiteralPath $ProgramPath)) {
        throw "Program.cs not found: $ProgramPath"
    }

    $content = Get-Content -LiteralPath $ProgramPath -Raw
    if ($content.Contains($Line)) {
        Write-P44 "Program.cs already contains: $Line"
        return
    }

    if (-not $content.Contains('app.Run();')) {
        throw "Could not find app.Run(); in $ProgramPath"
    }

    if (-not $Apply) {
        Write-P44 "WOULD add Program.cs line: $Line"
        return
    }

    $updated = $content.Replace('app.Run();', "$Line`r`napp.Run();")
    Set-Content -LiteralPath $ProgramPath -Value $updated -Encoding UTF8
    Write-P44 "Updated Program.cs: $Line"
}

function Add-LineAfterBuilderCreation([string]$ProgramPath, [string]$Line) {
    if (-not (Test-Path -LiteralPath $ProgramPath)) {
        throw "Program.cs not found: $ProgramPath"
    }

    $content = Get-Content -LiteralPath $ProgramPath -Raw
    if ($content.Contains($Line)) {
        Write-P44 "Program.cs already contains: $Line"
        return
    }

    $anchors = @(
        'var builder = WebApplication.CreateBuilder(args);',
        'WebApplicationBuilder builder = WebApplication.CreateBuilder(args);'
    )

    $anchor = $null
    foreach ($candidate in $anchors) {
        if ($content.Contains($candidate)) {
            $anchor = $candidate
            break
        }
    }

    if ($null -eq $anchor) {
        throw "Could not find WebApplication builder creation in $ProgramPath"
    }

    if (-not $Apply) {
        Write-P44 "WOULD add Program.cs line: $Line"
        return
    }

    $updated = $content.Replace($anchor, "$anchor`r`n$Line")
    Set-Content -LiteralPath $ProgramPath -Value $updated -Encoding UTF8
    Write-P44 "Updated Program.cs: $Line"
}

$repoRoot = Get-RepoRoot
Write-P44 "Repo root: $repoRoot"

$requiredProjects = @(
    'src/Core/Migration.Application/Migration.Application.csproj',
    'src/Core/Migration.Infrastructure.Sql/Migration.Infrastructure.Sql.csproj',
    'src/Core/Migration.Admin.Api/Migration.Admin.Api.csproj'
)

foreach ($relativeProject in $requiredProjects) {
    $path = Join-Path $repoRoot $relativeProject
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required project not found: $path"
    }
}

$payloadFiles = @(
    'src/Core/Migration.Application/Operational/WorkItems/OperationalWorkItemQueueContracts.cs',
    'src/Core/Migration.Infrastructure.Sql/Operational/WorkItems/SqlOperationalWorkItemQueueOptions.cs',
    'src/Core/Migration.Infrastructure.Sql/Operational/WorkItems/SqlOperationalWorkItemQueue.cs',
    'src/Core/Migration.Infrastructure.Sql/Operational/WorkItems/SqlOperationalWorkItemQueueServiceCollectionExtensions.cs',
    'src/Core/Migration.Admin.Api/Endpoints/Operational/SqlBackbone/SqlOperationalWorkItemQueueEndpointExtensions.cs',
    'database/sql/operational/003_create_operational_work_item_queue.sql'
)

foreach ($file in $payloadFiles) {
    Copy-PayloadFile -RepoRoot $repoRoot -RelativePath $file
}

$infrastructureSqlProject = Join-Path $repoRoot 'src/Core/Migration.Infrastructure.Sql/Migration.Infrastructure.Sql.csproj'
$adminApiProject = Join-Path $repoRoot 'src/Core/Migration.Admin.Api/Migration.Admin.Api.csproj'
$programPath = Join-Path $repoRoot 'src/Core/Migration.Admin.Api/Program.cs'

Ensure-ProjectReference -ProjectPath $infrastructureSqlProject -ReferenceInclude '..\Migration.Application\Migration.Application.csproj'
Ensure-ProjectReference -ProjectPath $adminApiProject -ReferenceInclude '..\Migration.Application\Migration.Application.csproj'
Ensure-ProjectReference -ProjectPath $adminApiProject -ReferenceInclude '..\Migration.Infrastructure.Sql\Migration.Infrastructure.Sql.csproj'

Add-LineAfterBuilderCreation -ProgramPath $programPath -Line 'builder.Services.AddSqlOperationalWorkItemQueue();'
Add-LineBeforeAppRun -ProgramPath $programPath -Line 'app.MapSqlOperationalWorkItemQueueEndpoints();'

if (-not $Apply) {
    Write-P44 'Preview complete. Re-run with -Apply to install.'
} else {
    Write-P44 'Install complete. Next: ./patches/P4.4-Validate-SqlOperationalWorkItemQueue.ps1; dotnet restore; dotnet build'
}
