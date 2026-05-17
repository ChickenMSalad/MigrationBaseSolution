$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set022-queue-failure-handler"

Write-Host "Applying P2 Set 022 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Queues\IQueueFailureHandler.cs",
    "src\Migration.ControlPlane\Queues\QueueFailureHandler.cs",
    "src\Migration.ControlPlane\Queues\QueueFailureHandlerRegistrationExtensions.cs",
    "src\Migration.Admin.Api\Endpoints\QueueFailureHandlerEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\queueFailureHandler.ts",
    "tools\test\smoke-queue-failure-handler.ps1",
    "tools\test\smoke-queue-failure-handler.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_022_QUEUE_FAILURE_HANDLER.md"
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

if ($program -notmatch "AddQueueFailureHandling") {
    if ($program -match "builder\.Services\.AddQueueReceiveProvider\(builder\.Configuration\);") {
        $program = $program.Replace(
            "builder.Services.AddQueueReceiveProvider(builder.Configuration);",
            "builder.Services.AddQueueReceiveProvider(builder.Configuration);`r`nbuilder.Services.AddQueueFailureHandling();")
        Write-Host "Patched Program.cs queue failure handling registration."
    }
    elseif ($program -match "builder\.Services\.AddQueueDispatchProvider\(builder\.Configuration\);") {
        $program = $program.Replace(
            "builder.Services.AddQueueDispatchProvider(builder.Configuration);",
            "builder.Services.AddQueueDispatchProvider(builder.Configuration);`r`nbuilder.Services.AddQueueFailureHandling();")
        Write-Host "Patched Program.cs queue failure handling registration."
    }
    else {
        throw "Could not find queue service registration anchor in Program.cs."
    }
}

if ($program -notmatch "MapQueueFailureHandlerEndpoints") {
    if ($program -match "api\.MapQueueFailureArtifactEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapQueueFailureArtifactEndpoints();",
            "api.MapQueueFailureArtifactEndpoints();`r`napi.MapQueueFailureHandlerEndpoints();")
        Write-Host "Patched Program.cs queue failure handler endpoints."
    }
    else {
        throw "Could not find queue failure artifact endpoint mapping anchor in Program.cs."
    }
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 022 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "Then validate after starting Admin API:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-queue-failure-handler.ps1 -BaseUrl http://localhost:5173"
