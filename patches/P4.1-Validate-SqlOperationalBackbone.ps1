$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$projectPath = Join-Path $repoRoot 'src\Core\Migration.Infrastructure.Sql\Migration.Infrastructure.Sql.csproj'
$schemaPath = Join-Path $repoRoot 'database\sql\operational\001_create_operational_backbone.sql'

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Missing SQL infrastructure project: $projectPath"
}

if (-not (Test-Path -LiteralPath $schemaPath)) {
    throw "Missing SQL operational schema script: $schemaPath"
}

[xml]$project = Get-Content -LiteralPath $projectPath -Raw

$packageReferences = @()

foreach ($itemGroup in @($project.Project.ItemGroup)) {
    if ($itemGroup.PSObject.Properties.Name -contains "PackageReference") {
        $packageReferences += @($itemGroup.PackageReference)
    }
}

foreach ($reference in $packageReferences) {
    if ($reference.PSObject.Properties.Name -contains "Version") {
        throw "Inline package version found in $projectPath"
    }
}

Write-Host '[P4.1] SQL operational backbone files are present and package references use central package management.' -ForegroundColor Green
