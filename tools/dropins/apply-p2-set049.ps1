$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set049-queue-execution-governance"

Write-Host "Applying P2 Set 049 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Operations\QueueExecutionGovernanceContracts.cs",
    "src\Migration.ControlPlane\Operations\IQueueExecutionGovernanceService.cs",
    "src\Migration.ControlPlane\Operations\QueueExecutionGovernanceService.cs",
    "src\Migration.ControlPlane\Operations\QueueExecutionGovernanceRegistrationExtensions.cs",
    "src\Migration.Admin.Api\Endpoints\QueueExecutionGovernanceEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\queueExecutionGovernance.ts",
    "tools\test\smoke-queue-execution-governance.ps1",
    "tools\test\smoke-queue-execution-governance.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_049_QUEUE_EXECUTION_GOVERNANCE.md"
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

if ($program -notmatch "AddQueueExecutionGovernance") {
    if ($program -match "builder\.Services\.AddOperationalMode\(\);") {
        $program = $program.Replace(
            "builder.Services.AddOperationalMode();",
            "builder.Services.AddOperationalMode();`r`nbuilder.Services.AddQueueExecutionGovernance();")
        Write-Host "Patched Program.cs queue execution governance registration."
    }
    else {
        throw "Could not find operational mode registration anchor in Program.cs."
    }
}

if ($program -notmatch "MapQueueExecutionGovernanceEndpoints") {
    if ($program -match "api\.MapOperationalModeEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapOperationalModeEndpoints();",
            "api.MapOperationalModeEndpoints();`r`napi.MapQueueExecutionGovernanceEndpoints();")
        Write-Host "Patched Program.cs queue execution governance endpoints."
    }
    else {
        throw "Could not find operational mode endpoint mapping anchor in Program.cs."
    }
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 049 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "Then validate after starting Admin API:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-queue-execution-governance.ps1 -BaseUrl http://localhost:5173"
