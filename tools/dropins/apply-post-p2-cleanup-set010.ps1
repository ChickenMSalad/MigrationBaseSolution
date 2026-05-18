$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\post-p2-cleanup-set010-ready-for-p3"

Write-Host "Applying Post-P2 Cleanup Set 010 from $repoRoot"

$files = @(
    "docs\post-p2-cleanup\POST_P2_CLEANUP_SET_010_READY_FOR_P3.md",
    "docs\p3-planning\P3_STARTING_BASELINE.md"
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
Write-Host "Post-P2 Cleanup Set 010 applied."
Write-Host "No build required."
Write-Host "Before P3, run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\maintenance\validate-post-p2-cleanup.ps1"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\validate-p2-completion.ps1 -BaseUrl http://localhost:5173 -AllowUnconfigured"
