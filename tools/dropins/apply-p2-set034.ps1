$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set034-cloud-operation-audit-events"

Write-Host "Applying P2 Set 034 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Audit\CloudOperationAuditEventNames.cs",
    "src\Migration.ControlPlane\Audit\CloudOperationAuditEventFactory.cs",
    "src\Migration.Admin.Api\Endpoints\CloudOperationAuditEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\cloudOperationAudit.ts",
    "tools\test\smoke-cloud-operation-audit.ps1",
    "tools\test\smoke-cloud-operation-audit.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_034_CLOUD_OPERATION_AUDIT_EVENTS.md"
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

if ($program -notmatch "MapCloudOperationAuditEndpoints") {
    if ($program -match "api\.MapQueueAuditEventEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapQueueAuditEventEndpoints();",
            "api.MapQueueAuditEventEndpoints();`r`napi.MapCloudOperationAuditEndpoints();")
        Write-Host "Patched Program.cs cloud operation audit endpoints."
    }
    elseif ($program -match "api\.MapAuditEventWriterEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapAuditEventWriterEndpoints();",
            "api.MapAuditEventWriterEndpoints();`r`napi.MapCloudOperationAuditEndpoints();")
        Write-Host "Patched Program.cs cloud operation audit endpoints."
    }
    else {
        throw "Could not find audit endpoint mapping anchor in Program.cs."
    }
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 034 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "Then validate after starting Admin API:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-cloud-operation-audit.ps1 -BaseUrl http://localhost:5173"
