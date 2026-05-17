$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set010-blob-switch-templates"

Write-Host "Applying P2 Set 010 from $repoRoot"

$files = @(
    "config\storage\azure-blob.localtest.appsettings.example.json",
    "config\storage\azure-blob.managedidentity.appsettings.example.json",
    "config\storage\README.md",
    "tools\test\smoke-storage-provider-stack.ps1",
    "tools\test\smoke-storage-provider-stack.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_010_BLOB_SWITCH_TEMPLATES.md"
)

foreach ($relative in $files) {
    $source = Join-Path $payloadRoot $relative
    $target = Join-Path $repoRoot $relative
    $targetDirectory = Split-Path $target -Parent

    if (!(Test-Path $source)) {
        throw "Missing file: $source"
    }

    if (!(Test-Path $targetDirectory)) {
        New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
    }

    Copy-Item $source $target -Force
    Write-Host "Verified $relative"
}

Write-Host ""
Write-Host "P2 Set 010 applied."
Write-Host "After starting Admin API, validate local stack with:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-storage-provider-stack.ps1 -BaseUrl http://localhost:5173"
