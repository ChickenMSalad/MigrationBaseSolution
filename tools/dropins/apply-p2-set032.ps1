$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set032-audit-event-writer"

Write-Host "Applying P2 Set 032 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Audit\IAuditEventWriter.cs",
    "src\Migration.ControlPlane\Audit\AuditEventWriter.cs",
    "src\Migration.ControlPlane\Audit\AuditEventWriterRegistrationExtensions.cs",
    "src\Migration.Admin.Api\Endpoints\AuditEventWriterEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\auditEventWriter.ts",
    "tools\test\smoke-audit-event-writer.ps1",
    "tools\test\smoke-audit-event-writer.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_032_AUDIT_EVENT_WRITER.md"
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

if ($program -notmatch "AddAuditEventWriter") {
    if ($program -match "builder\.Services\.AddAuditPersistence\(builder\.Configuration\);") {
        $program = $program.Replace(
            "builder.Services.AddAuditPersistence(builder.Configuration);",
            "builder.Services.AddAuditPersistence(builder.Configuration);`r`nbuilder.Services.AddAuditEventWriter();")
        Write-Host "Patched Program.cs audit event writer registration."
    }
    else {
        throw "Could not find audit persistence registration anchor in Program.cs."
    }
}

if ($program -notmatch "MapAuditEventWriterEndpoints") {
    if ($program -match "api\.MapAuditArtifactPersistenceEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapAuditArtifactPersistenceEndpoints();",
            "api.MapAuditArtifactPersistenceEndpoints();`r`napi.MapAuditEventWriterEndpoints();")
        Write-Host "Patched Program.cs audit event writer endpoints."
    }
    elseif ($program -match "api\.MapAuditPersistenceEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapAuditPersistenceEndpoints();",
            "api.MapAuditPersistenceEndpoints();`r`napi.MapAuditEventWriterEndpoints();")
        Write-Host "Patched Program.cs audit event writer endpoints."
    }
    else {
        throw "Could not find audit endpoint mapping anchor in Program.cs."
    }
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 032 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "Then validate after starting Admin API:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-audit-event-writer.ps1 -BaseUrl http://localhost:5173"
