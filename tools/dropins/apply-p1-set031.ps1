$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p1-set031-github-actions-deploy-scaffold"

Write-Host "Applying P1 Set 031 from $repoRoot"

$files = @(
    ".github\workflows\azure-deployment-scaffold.yml",
    "docs\azure\GITHUB_ACTIONS_AZURE_DEPLOYMENT.md",
    "docs\cloud-roadmap-cleanup\P1_SET_031_GITHUB_ACTIONS_DEPLOYMENT_SCAFFOLD.md"
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
Write-Host "P1 Set 031 applied."
Write-Host "This is GitHub Actions scaffold only. No local build is required."
