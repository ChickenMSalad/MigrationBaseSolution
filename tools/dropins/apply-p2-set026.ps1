$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set026-worker-bootstrap-templates"

Write-Host "Applying P2 Set 026 from $repoRoot"

$files = @(
    "config\worker\queue-executor.dryrun.appsettings.example.json",
    "config\worker\queue-executor.local-inmemory.appsettings.example.json",
    "config\worker\queue-executor.azurequeue.appsettings.example.json",
    "config\worker\README.md",
    "tools\test\validate-worker-bootstrap-config.ps1",
    "tools\test\smoke-worker-bootstrap-templates.ps1",
    "tools\test\smoke-worker-bootstrap-templates.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_026_WORKER_BOOTSTRAP_TEMPLATES.md"
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
Write-Host "P2 Set 026 applied."
Write-Host "Validate:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-worker-bootstrap-templates.ps1"
