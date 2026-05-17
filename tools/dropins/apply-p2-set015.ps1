$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set015-queue-dispatch-contracts"

Write-Host "Applying P2 Set 015 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Queues\IQueueDispatchProvider.cs",
    "src\Migration.ControlPlane\Queues\InMemoryQueueDispatchProvider.cs",
    "src\Migration.ControlPlane\Queues\NullQueueDispatchProvider.cs",
    "src\Migration.ControlPlane\Queues\QueueDispatchRegistrationExtensions.cs",
    "src\Migration.Admin.Api\Endpoints\QueueDispatchDiagnosticsEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\queueDispatch.ts",
    "tools\test\smoke-queue-dispatch.ps1",
    "tools\test\smoke-queue-dispatch.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_015_QUEUE_DISPATCH_CONTRACTS.md"
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

if ($program -notmatch "AddQueueDispatchProvider") {
    if ($program -match "builder\.Services\.AddCloudCredentialValueProvider\(builder\.Configuration\);") {
        $program = $program.Replace(
            "builder.Services.AddCloudCredentialValueProvider(builder.Configuration);",
            "builder.Services.AddCloudCredentialValueProvider(builder.Configuration);`r`nbuilder.Services.AddQueueDispatchProvider(builder.Configuration);")
        Write-Host "Patched Program.cs queue dispatch provider registration."
    }
    else {
        throw "Could not find service registration anchor in Program.cs."
    }
}

if ($program -notmatch "MapQueueDispatchDiagnosticsEndpoints") {
    if ($program -match "api\.MapQueueIdempotencyEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapQueueIdempotencyEndpoints();",
            "api.MapQueueIdempotencyEndpoints();`r`napi.MapQueueDispatchDiagnosticsEndpoints();")
        Write-Host "Patched Program.cs queue dispatch endpoint mapping."
    }
    elseif ($program -match "api\.MapQueueContractDiagnosticsEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapQueueContractDiagnosticsEndpoints();",
            "api.MapQueueContractDiagnosticsEndpoints();`r`napi.MapQueueDispatchDiagnosticsEndpoints();")
        Write-Host "Patched Program.cs queue dispatch endpoint mapping."
    }
    else {
        throw "Could not find queue endpoint mapping anchor in Program.cs."
    }
}

if ($program -notmatch "using Migration\.ControlPlane\.Queues;") {
    $program = "using Migration.ControlPlane.Queues;`r`n" + $program
    Write-Host "Patched Program.cs using Migration.ControlPlane.Queues;"
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 015 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "Then validate after starting Admin API:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-queue-dispatch.ps1 -BaseUrl http://localhost:5173"
