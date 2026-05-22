[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Step {
    param([Parameter(Mandatory=$true)][string]$Message)
    Write-Host "[P4.10-VALIDATE] $Message" -ForegroundColor Cyan
}

function Assert-PathExists {
    param([Parameter(Mandatory=$true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Required path not found: $Path"
    }

    Write-Step "Found $Path"
}

function Assert-FileContains {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$Text
    )

    Assert-PathExists -Path $Path
    $content = Get-Content -LiteralPath $Path -Raw
    if (-not $content.Contains($Text)) {
        throw ('Expected text not found in {0}: {1}' -f $Path, $Text)
    }

    Write-Step "Verified text in $Path"
}

function Assert-NoInlinePackageVersions {
    param([Parameter(Mandatory=$true)][string]$ProjectPath)

    [xml]$projectXml = Get-Content -LiteralPath $ProjectPath -Raw
    $projectNode = $projectXml.Project
    if ($null -eq $projectNode) {
        throw "Invalid project XML: $ProjectPath"
    }

    foreach ($itemGroup in @($projectNode.ItemGroup)) {
        if ($null -eq $itemGroup) {
            continue
        }

        if (-not ($itemGroup.PSObject.Properties.Name -contains 'PackageReference')) {
            continue
        }

        foreach ($reference in @($itemGroup.PackageReference)) {
            if ($null -eq $reference) {
                continue
            }

            if ($reference.PSObject.Properties.Name -contains 'Version') {
                throw "Inline package version found in $ProjectPath"
            }
        }
    }

    Write-Step "No inline package versions found in $ProjectPath"
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$applicationProject = Join-Path $repoRoot 'src\Core\Migration.Application\Migration.Application.csproj'
$sqlInfrastructureProject = Join-Path $repoRoot 'src\Core\Migration.Infrastructure.Sql\Migration.Infrastructure.Sql.csproj'
$adminApiProject = Join-Path $repoRoot 'src\Core\Migration.Admin.Api\Migration.Admin.Api.csproj'
$programPath = Join-Path $repoRoot 'src\Core\Migration.Admin.Api\Program.cs'

Assert-PathExists -Path (Join-Path $repoRoot 'src\Core\Migration.Application\Operational\Readiness\OperationalRuntimeReadinessContracts.cs')
Assert-PathExists -Path (Join-Path $repoRoot 'src\Core\Migration.Infrastructure.Sql\Operational\Readiness\SqlOperationalRuntimeReadinessService.cs')
Assert-PathExists -Path (Join-Path $repoRoot 'src\Core\Migration.Admin.Api\Endpoints\Operational\SqlBackbone\SqlOperationalRuntimeReadinessEndpointExtensions.cs')
Assert-PathExists -Path (Join-Path $repoRoot 'src\Core\Migration.Admin.Api\Registration\AdminApiOperationalRuntimeReadinessRegistrationExtensions.cs')
Assert-PathExists -Path (Join-Path $repoRoot 'docs\cloud-roadmap-cleanup\P4_SET_010_OPERATIONAL_RUNTIME_READINESS.md')

Assert-FileContains -Path $programPath -Text 'builder.Services.AddAdminApiOperationalRuntimeReadiness(builder.Configuration);'
Assert-FileContains -Path $programPath -Text 'app.MapSqlOperationalRuntimeReadinessEndpoints();'

Assert-NoInlinePackageVersions -ProjectPath $applicationProject
Assert-NoInlinePackageVersions -ProjectPath $sqlInfrastructureProject
Assert-NoInlinePackageVersions -ProjectPath $adminApiProject

Write-Step 'P4.10 validation passed.'
