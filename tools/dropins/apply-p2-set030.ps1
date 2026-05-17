$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set030-audit-persistence-contracts"

Write-Host "Applying P2 Set 030 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Audit\AuditPersistenceContracts.cs",
    "src\Migration.ControlPlane\Audit\IAuditPersistenceProvider.cs",
    "src\Migration.ControlPlane\Audit\InMemoryAuditPersistenceProvider.cs",
    "src\Migration.ControlPlane\Audit\AuditRecordFactory.cs",
    "src\Migration.ControlPlane\Audit\AuditPersistenceRegistrationExtensions.cs",
    "src\Migration.Admin.Api\Endpoints\AuditPersistenceEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\auditPersistence.ts",
    "tools\test\smoke-audit-persistence.ps1",
    "tools\test\smoke-audit-persistence.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_030_AUDIT_PERSISTENCE_CONTRACTS.md"
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

if ($program -notmatch "AddAuditPersistence") {
    if ($program -match "builder\.Services\.AddQueueExecutionReadiness\(\);") {
        $program = $program.Replace(
            "builder.Services.AddQueueExecutionReadiness();",
            "builder.Services.AddQueueExecutionReadiness();`r`nbuilder.Services.AddAuditPersistence(builder.Configuration);")
        Write-Host "Patched Program.cs audit persistence registration."
    }
    else {
        throw "Could not find service registration anchor in Program.cs."
    }
}

if ($program -notmatch "MapAuditPersistenceEndpoints") {
    if ($program -match "api\.MapAuditEventContractEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapAuditEventContractEndpoints();",
            "api.MapAuditEventContractEndpoints();`r`napi.MapAuditPersistenceEndpoints();")
        Write-Host "Patched Program.cs audit persistence endpoints."
    }
    else {
        throw "Could not find audit endpoint mapping anchor in Program.cs."
    }
}

if ($program -notmatch "using Migration\.ControlPlane\.Audit;") {
    $program = "using Migration.ControlPlane.Audit;`r`n" + $program
    Write-Host "Patched Program.cs using Migration.ControlPlane.Audit;"
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 030 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "Then validate after starting Admin API:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-audit-persistence.ps1 -BaseUrl http://localhost:5173"
