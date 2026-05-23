Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$slnPath = Join-Path $repoRoot 'MigrationBaseSolution.sln'
$projects = @(
    'src\Core\MigrationBase.Core\MigrationBase.Core.csproj',
    'src\Core\Migration.Core.Azure\Migration.Core.Azure.csproj'
)

if (-not (Test-Path -LiteralPath $slnPath)) {
    throw "Solution file not found: $slnPath"
}

foreach ($relativeProject in $projects) {
    $projectPath = Join-Path $repoRoot $relativeProject
    if (-not (Test-Path -LiteralPath $projectPath)) {
        throw "Project file not found: $projectPath"
    }

    Write-Host "Ensuring solution project is under Core: $relativeProject"
    & dotnet sln $slnPath remove $projectPath 2>$null | Out-Null
    & dotnet sln $slnPath add $projectPath --solution-folder Core
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet sln add failed for $projectPath"
    }
}

Write-Host 'P5.1.7 core project solution alignment complete.'
