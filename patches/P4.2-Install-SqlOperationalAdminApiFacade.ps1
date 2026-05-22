[CmdletBinding()]
param(
    [switch]$Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Step {
    param([string]$Message)
    Write-Host "[P4.2] $Message" -ForegroundColor Cyan
}

function Get-XmlChildNodesByName {
    param(
        [Parameter(Mandatory=$true)]$Node,
        [Parameter(Mandatory=$true)][string]$Name
    )

    $items = @()
    foreach ($child in @($Node.ChildNodes)) {
        if ($child.Name -eq $Name) {
            $items += $child
        }
    }
    return $items
}

function Add-ProjectReferenceIfMissing {
    param(
        [Parameter(Mandatory=$true)][string]$ProjectPath,
        [Parameter(Mandatory=$true)][string]$ReferencePath
    )

    if (-not (Test-Path -LiteralPath $ProjectPath)) {
        throw "Project file not found: $ProjectPath"
    }

    [xml]$xml = Get-Content -LiteralPath $ProjectPath -Raw
    $project = $xml.Project
    if ($null -eq $project) {
        throw "Invalid project XML: $ProjectPath"
    }

    $existing = $false
    foreach ($itemGroup in @(Get-XmlChildNodesByName -Node $project -Name 'ItemGroup')) {
        foreach ($reference in @(Get-XmlChildNodesByName -Node $itemGroup -Name 'ProjectReference')) {
            $include = $reference.GetAttribute('Include')
            if ($include -eq $ReferencePath) {
                $existing = $true
            }
        }
    }

    if ($existing) {
        Write-Step "ProjectReference already exists: $ReferencePath"
        return
    }

    if (-not $Apply) {
        Write-Step "WOULD add ProjectReference $ReferencePath to $ProjectPath"
        return
    }

    $itemGroup = $xml.CreateElement('ItemGroup')
    $projectReference = $xml.CreateElement('ProjectReference')
    $projectReference.SetAttribute('Include', $ReferencePath)
    [void]$itemGroup.AppendChild($projectReference)
    [void]$project.AppendChild($itemGroup)
    $xml.Save($ProjectPath)
    Write-Step "Added ProjectReference $ReferencePath"
}

function Ensure-Using {
    param(
        [Parameter(Mandatory=$true)][string]$ProgramPath,
        [Parameter(Mandatory=$true)][string]$UsingStatement
    )

    $content = Get-Content -LiteralPath $ProgramPath -Raw
    if ($content -match [regex]::Escape($UsingStatement)) {
        return $content
    }

    return $UsingStatement + [Environment]::NewLine + $content
}

function Ensure-LineBeforeAppRun {
    param(
        [Parameter(Mandatory=$true)][string]$Content,
        [Parameter(Mandatory=$true)][string]$Line
    )

    if ($Content -match [regex]::Escape($Line)) {
        return $Content
    }

    if ($Content -notmatch 'app\.Run\s*\(') {
        throw "Could not find app.Run(...) in Program.cs. Add manually: $Line"
    }

    return [regex]::Replace($Content, 'app\.Run\s*\(', ($Line + [Environment]::NewLine + 'app.Run('), 1)
}

function Ensure-ServiceRegistrationAfterBuilder {
    param(
        [Parameter(Mandatory=$true)][string]$Content,
        [Parameter(Mandatory=$true)][string]$Line
    )

    if ($Content -match [regex]::Escape($Line)) {
        return $Content
    }

    $pattern = 'var\s+builder\s*=\s*WebApplication\.CreateBuilder\(args\)\s*;'
    if ($Content -notmatch $pattern) {
        throw "Could not find WebApplication builder creation in Program.cs. Add manually after builder creation: $Line"
    }

    return [regex]::Replace($Content, $pattern, ('$0' + [Environment]::NewLine + $Line), 1)
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$payloadRoot = Join-Path $repoRoot 'payload'
$adminApiRoot = Join-Path $repoRoot 'src\Core\Migration.Admin.Api'
$adminApiProject = Join-Path $adminApiRoot 'Migration.Admin.Api.csproj'
$sqlInfraProject = Join-Path $repoRoot 'src\Core\Migration.Infrastructure.Sql\Migration.Infrastructure.Sql.csproj'
$programPath = Join-Path $adminApiRoot 'Program.cs'

Write-Step "Repo root: $repoRoot"

if (-not (Test-Path -LiteralPath $adminApiProject)) {
    throw "Expected Admin API project not found: $adminApiProject"
}

if (-not (Test-Path -LiteralPath $sqlInfraProject)) {
    throw "Expected P4.1 SQL infrastructure project not found: $sqlInfraProject"
}

$payloadAdminApiRoot = Join-Path $payloadRoot 'src\Core\Migration.Admin.Api'
if (-not (Test-Path -LiteralPath $payloadAdminApiRoot)) {
    throw "Payload folder not found: $payloadAdminApiRoot"
}

$files = Get-ChildItem -LiteralPath $payloadAdminApiRoot -Recurse -File
foreach ($file in $files) {
    $relative = $file.FullName.Substring($payloadAdminApiRoot.Length).TrimStart('\','/')
    $destination = Join-Path $adminApiRoot $relative
    $destinationDirectory = Split-Path -Parent $destination

    if (-not $Apply) {
        Write-Step "WOULD copy $($file.FullName) -> $destination"
        continue
    }

    if (-not (Test-Path -LiteralPath $destinationDirectory)) {
        New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
    }

    Copy-Item -LiteralPath $file.FullName -Destination $destination -Force
    Write-Step "Copied $relative"
}

Add-ProjectReferenceIfMissing -ProjectPath $adminApiProject -ReferencePath '..\Migration.Infrastructure.Sql\Migration.Infrastructure.Sql.csproj'

if (-not (Test-Path -LiteralPath $programPath)) {
    throw "Program.cs not found: $programPath"
}

if (-not $Apply) {
    Write-Step "WOULD add endpoint/service registration to Program.cs if missing"
} else {
    $program = Get-Content -LiteralPath $programPath -Raw
    $program = Ensure-Using -ProgramPath $programPath -UsingStatement 'using Migration.Admin.Api.Endpoints.Operational.SqlBackbone;'
    Set-Content -LiteralPath $programPath -Value $program -Encoding UTF8

    $program = Get-Content -LiteralPath $programPath -Raw
    $program = Ensure-Using -ProgramPath $programPath -UsingStatement 'using Migration.Admin.Api.Registration;'
    Set-Content -LiteralPath $programPath -Value $program -Encoding UTF8

    $program = Get-Content -LiteralPath $programPath -Raw
    $program = Ensure-ServiceRegistrationAfterBuilder -Content $program -Line 'builder.Services.AddAdminApiSqlOperationalBackbone(builder.Configuration);'
    $program = Ensure-LineBeforeAppRun -Content $program -Line 'app.MapSqlOperationalBackboneEndpoints();'
    Set-Content -LiteralPath $programPath -Value $program -Encoding UTF8
    Write-Step "Updated Program.cs registration"
}

Write-Step "Complete. Next: ./patches/P4.2-Validate-SqlOperationalAdminApiFacade.ps1; dotnet restore; dotnet build"
