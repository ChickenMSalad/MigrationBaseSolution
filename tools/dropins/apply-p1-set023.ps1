$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p1-set023-containerization-scaffold"

Write-Host "Applying P1 Set 023 from $repoRoot"

$files = @(
    "deploy\docker\AdminApi.Dockerfile",
    "deploy\docker\QueueExecutor.Dockerfile",
    "deploy\docker\docker-compose.local.yml",
    "deploy\docker\.dockerignore",
    "deploy\docker\README.md",
    "docs\cloud-roadmap-cleanup\P1_SET_023_CONTAINERIZATION_SCAFFOLD.md"
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
Write-Host "P1 Set 023 applied."
Write-Host "Optional Docker validation:"
Write-Host "  docker build -f deploy/docker/AdminApi.Dockerfile -t migration-admin-api:local ."
Write-Host "  docker build -f deploy/docker/QueueExecutor.Dockerfile -t migration-queue-executor:local ."
