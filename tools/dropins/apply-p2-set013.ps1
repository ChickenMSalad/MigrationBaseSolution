$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set013-queue-message-contracts"

Write-Host "Applying P2 Set 013 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Queues\QueueMessageContracts.cs",
    "src\Migration.ControlPlane\Queues\QueueMessageEnvelopeFactory.cs",
    "src\Migration.Admin.Api\Endpoints\QueueContractDiagnosticsEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\queueContracts.ts",
    "tools\test\smoke-queue-contracts.ps1",
    "docs\cloud-roadmap-cleanup\P2_SET_013_QUEUE_MESSAGE_CONTRACTS.md"
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

$programPath = Join-Path $repoRoot "src\Migration.Admin.Api\Program.cs"
$program = Get-Content $programPath -Raw

if ($program -notmatch "MapQueueContractDiagnosticsEndpoints") {
    $program = $program.Replace(
        "api.MapQueueProviderPlanEndpoints();",
        "api.MapQueueProviderPlanEndpoints();`r`napi.MapQueueContractDiagnosticsEndpoints();")

    Set-Content -Path $programPath -Value $program -Encoding UTF8
    Write-Host "Patched Program.cs queue contract endpoints."
}

Write-Host ""
Write-Host "P2 Set 013 applied."
