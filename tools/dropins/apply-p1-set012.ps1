$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p1-set012-resource-naming"

Write-Host "Applying P1 Set 012 from $repoRoot"

$files = @(
    "tools\cloud\generate-azure-resource-names.ps1",
    "tools\cloud\generate-azure-resource-names.cmd",
    "docs\azure\AZURE_RESOURCE_NAMING.md",
    "docs\cloud-roadmap-cleanup\P1_SET_012_AZURE_RESOURCE_NAMING.md"
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
Write-Host "P1 Set 012 applied."
Write-Host "Optional validation:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\cloud\generate-azure-resource-names.ps1"
