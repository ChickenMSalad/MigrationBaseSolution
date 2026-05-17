$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p1-set022-ci-validation-scaffold"

Write-Host "Applying P1 Set 022 from $repoRoot"

$files = @(
    ".github\workflows\migration-platform-validation.yml",
    "docs\azure\CI_CD_VALIDATION_PLAN.md",
    "docs\cloud-roadmap-cleanup\P1_SET_022_CI_VALIDATION_SCAFFOLD.md"
)

foreach ($relative in $files) {
    $source = Join-Path $payloadRoot $relative
    $target = Join-Path $repoRoot $relative
    $targetDirectory = Split-Path $target -Parent

    if (!(Test-Path $source)) {
        throw "Drop-in package is missing expected file: $source"
    }

    if (!(Test-Path $targetDirectory)) {
        New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
    }

    Copy-Item $source $target -Force
    Write-Host "Verified $relative"
}

Write-Host ""
Write-Host "P1 Set 022 applied."
Write-Host "Optional local validation:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "  dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj"
Write-Host "  cd .\src\Admin\Migration.Admin.Web; npm run build"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\cloud\validate-cloud-diagnostics.ps1 -SkipHttp -Strict"
