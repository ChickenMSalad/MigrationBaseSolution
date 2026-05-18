$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\post-p2-cleanup-set001-docs-tools-inventory"

Write-Host "Applying Post-P2 Cleanup Set 001 from $repoRoot"

$files = @(
    "docs\post-p2-cleanup\P2_DOCS_TOOLS_EXPECTED_MANIFEST.json",
    "docs\post-p2-cleanup\P2_DOCS_TOOLS_CLEANUP_GUIDE.md",
    "docs\cloud-roadmap-cleanup\P2_COMPLETION_CHECKPOINT.md",
    "docs\cloud-roadmap-cleanup\P3_RECOMMENDED_PLAN.md",
    "docs\cloud-roadmap-cleanup\P2_SET_053_COMPLETION_CHECKPOINT.md",
    "tools\test\validate-p2-completion.ps1",
    "tools\test\validate-p2-completion.cmd",
    "tools\test\validate-full-p2-stack.ps1",
    "tools\test\validate-full-p2-stack.cmd",
    "tools\maintenance\audit-p2-docs-tools.ps1"
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

    if (!(Test-Path $target)) {
        Copy-Item $source $target -Force
        Write-Host "Restored missing $relative"
    }
    else {
        Write-Host "Already present $relative"
    }
}

Write-Host ""
Write-Host "Post-P2 Cleanup Set 001 applied."
Write-Host "Run inventory:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\maintenance\audit-p2-docs-tools.ps1 -WriteCleanupCommands"
Write-Host ""
Write-Host "Then review:"
Write-Host "  docs\post-p2-cleanup\P2_DOCS_TOOLS_INVENTORY_REPORT.md"
Write-Host "  docs\post-p2-cleanup\p2-cleanup-git-rm-commands.ps1"
