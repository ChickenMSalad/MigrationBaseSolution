$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set021-queue-failure-artifacts"

Write-Host "Applying P2 Set 021 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Queues\QueueFailureArtifactContracts.cs",
    "src\Migration.ControlPlane\Queues\QueueFailureArtifactPlanner.cs",
    "src\Migration.Admin.Api\Endpoints\QueueFailureArtifactEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\queueFailureArtifacts.ts",
    "tools\test\smoke-queue-failure-artifact.ps1",
    "tools\test\smoke-queue-failure-artifact.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_021_QUEUE_FAILURE_ARTIFACTS.md"
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

if ($program -notmatch "MapQueueFailureArtifactEndpoints") {
    if ($program -match "api\.MapQueuePoisonHandlingEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapQueuePoisonHandlingEndpoints();",
            "api.MapQueuePoisonHandlingEndpoints();`r`napi.MapQueueFailureArtifactEndpoints();")
        Write-Host "Patched Program.cs queue failure artifact endpoints."
    }
    else {
        throw "Could not find queue poison handling endpoint mapping anchor in Program.cs."
    }
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 021 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "Then validate after starting Admin API:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-queue-failure-artifact.ps1 -BaseUrl http://localhost:5173"
