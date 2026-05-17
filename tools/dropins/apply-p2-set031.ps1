$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set031-audit-artifact-persistence"

Write-Host "Applying P2 Set 031 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Audit\ArtifactAuditPersistenceOptions.cs",
    "src\Migration.ControlPlane\Audit\ArtifactAuditPersistenceProvider.cs",
    "src\Migration.ControlPlane\Audit\AuditPersistenceRegistrationExtensions.cs",
    "src\Migration.Admin.Api\Endpoints\AuditArtifactPersistenceEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\auditArtifactPersistence.ts",
    "tools\test\smoke-audit-artifact-persistence.ps1",
    "tools\test\smoke-audit-artifact-persistence.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_031_AUDIT_ARTIFACT_PERSISTENCE.md"
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

if ($program -notmatch "MapAuditArtifactPersistenceEndpoints") {
    if ($program -match "api\.MapAuditPersistenceEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapAuditPersistenceEndpoints();",
            "api.MapAuditPersistenceEndpoints();`r`napi.MapAuditArtifactPersistenceEndpoints();")
        Write-Host "Patched Program.cs audit artifact persistence endpoints."
    }
    else {
        throw "Could not find audit persistence endpoint mapping anchor in Program.cs."
    }
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 031 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "Then validate after starting Admin API:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-audit-artifact-persistence.ps1 -BaseUrl http://localhost:5173"
