[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-PathExists {
    param([Parameter(Mandatory=$true)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Required path missing: $Path"
    }
}

function Assert-FileContains {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$Text
    )
    $content = Get-Content -LiteralPath $Path -Raw
    if ($content -notmatch [regex]::Escape($Text)) {
        throw ("Expected text not found in {0}: {1}" -f $Path, $Text)
    }
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

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$adminApiRoot = Join-Path $repoRoot 'src\Core\Migration.Admin.Api'
$adminApiProject = Join-Path $adminApiRoot 'Migration.Admin.Api.csproj'
$programPath = Join-Path $adminApiRoot 'Program.cs'
$endpointPath = Join-Path $adminApiRoot 'Endpoints\Operational\SqlBackbone\SqlOperationalBackboneEndpointExtensions.cs'
$registrationPath = Join-Path $adminApiRoot 'Registration\AdminApiSqlOperationalBackboneRegistrationExtensions.cs'

Assert-PathExists -Path $adminApiProject
Assert-PathExists -Path $programPath
Assert-PathExists -Path $endpointPath
Assert-PathExists -Path $registrationPath
Assert-FileContains -Path $programPath -Text 'app.MapSqlOperationalBackboneEndpoints();'
Assert-FileContains -Path $programPath -Text 'builder.Services.AddAdminApiSqlOperationalBackbone(builder.Configuration);'

[xml]$projectXml = Get-Content -LiteralPath $adminApiProject -Raw
$foundReference = $false
foreach ($itemGroup in @(Get-XmlChildNodesByName -Node $projectXml.Project -Name 'ItemGroup')) {
    foreach ($reference in @(Get-XmlChildNodesByName -Node $itemGroup -Name 'ProjectReference')) {
        $include = $reference.GetAttribute('Include')
        if ($include -eq '..\Migration.Infrastructure.Sql\Migration.Infrastructure.Sql.csproj') {
            $foundReference = $true
        }
    }

    foreach ($packageReference in @(Get-XmlChildNodesByName -Node $itemGroup -Name 'PackageReference')) {
        if ($packageReference.PSObject.Properties.Name -contains 'Version') {
            throw "Inline PackageReference Version found in $adminApiProject"
        }
        if ($packageReference.HasAttribute('Version')) {
            throw "Inline PackageReference Version attribute found in $adminApiProject"
        }
    }
}

if (-not $foundReference) {
    throw "Expected ProjectReference missing from $adminApiProject"
}

Write-Host '[P4.2] Validation passed.' -ForegroundColor Green
