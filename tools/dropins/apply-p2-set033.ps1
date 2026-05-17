$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set033-queue-audit-events"

Write-Host "Applying P2 Set 033 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Audit\QueueAuditEventNames.cs",
    "src\Migration.ControlPlane\Audit\QueueAuditEventFactory.cs",
    "src\Migration.Admin.Api\Endpoints\QueueAuditEventEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\queueAuditEvents.ts",
    "tools\test\smoke-queue-audit-events.ps1",
    "tools\test\smoke-queue-audit-events.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_033_QUEUE_AUDIT_EVENTS.md"
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

if ($program -notmatch "MapQueueAuditEventEndpoints") {
    if ($program -match "api\.MapAuditEventWriterEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapAuditEventWriterEndpoints();",
            "api.MapAuditEventWriterEndpoints();`r`napi.MapQueueAuditEventEndpoints();")
        Write-Host "Patched Program.cs queue audit event endpoints."
    }
    elseif ($program -match "api\.MapAuditArtifactPersistenceEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapAuditArtifactPersistenceEndpoints();",
            "api.MapAuditArtifactPersistenceEndpoints();`r`napi.MapQueueAuditEventEndpoints();")
        Write-Host "Patched Program.cs queue audit event endpoints."
    }
    else {
        throw "Could not find audit endpoint mapping anchor in Program.cs."
    }
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 033 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "Then validate after starting Admin API:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-queue-audit-events.ps1 -BaseUrl http://localhost:5173"
