$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set017-queue-receive-contracts"

Write-Host "Applying P2 Set 017 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Queues\IQueueReceiveProvider.cs",
    "src\Migration.ControlPlane\Queues\InMemoryQueueReceiveProvider.cs",
    "src\Migration.ControlPlane\Queues\NullQueueReceiveProvider.cs",
    "src\Migration.ControlPlane\Queues\AzureQueueReceiveProvider.cs",
    "src\Migration.ControlPlane\Queues\QueueReceiveRegistrationExtensions.cs",
    "src\Migration.Admin.Api\Endpoints\QueueReceiveDiagnosticsEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\queueReceive.ts",
    "tools\test\smoke-queue-receive.ps1",
    "tools\test\smoke-queue-receive.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_017_QUEUE_RECEIVE_CONTRACTS.md"
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

if ($program -notmatch "AddQueueReceiveProvider") {
    if ($program -match "builder\.Services\.AddQueueDispatchProvider\(builder\.Configuration\);") {
        $program = $program.Replace(
            "builder.Services.AddQueueDispatchProvider(builder.Configuration);",
            "builder.Services.AddQueueDispatchProvider(builder.Configuration);`r`nbuilder.Services.AddQueueReceiveProvider(builder.Configuration);")
        Write-Host "Patched Program.cs queue receive provider registration."
    }
    else {
        throw "Could not find queue dispatch registration anchor in Program.cs."
    }
}

if ($program -notmatch "MapQueueReceiveDiagnosticsEndpoints") {
    if ($program -match "api\.MapQueueDispatchDiagnosticsEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapQueueDispatchDiagnosticsEndpoints();",
            "api.MapQueueDispatchDiagnosticsEndpoints();`r`napi.MapQueueReceiveDiagnosticsEndpoints();")
        Write-Host "Patched Program.cs queue receive endpoint mapping."
    }
    else {
        throw "Could not find queue dispatch endpoint mapping anchor in Program.cs."
    }
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 017 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "Then validate after starting Admin API:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-queue-receive.ps1 -BaseUrl http://localhost:5173 -AllowUnconfigured"
