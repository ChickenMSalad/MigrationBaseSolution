$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set005-artifact-manifest-index"

Write-Host "Applying P2 Set 005 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Storage\ArtifactManifestContracts.cs",
    "src\Migration.ControlPlane\Storage\IArtifactManifestIndexService.cs",
    "src\Migration.ControlPlane\Storage\ArtifactManifestIndexService.cs",
    "src\Migration.ControlPlane\Storage\ArtifactManifestIndexRegistrationExtensions.cs",
    "src\Migration.Admin.Api\Endpoints\ArtifactManifestIndexEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\artifactManifestIndex.ts",
    "docs\cloud-roadmap-cleanup\P2_SET_005_ARTIFACT_MANIFEST_INDEX.md"
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

if ($program -notmatch "AddArtifactManifestIndex") {
    if ($program -match "builder\.Services\.AddArtifactStorage\(\);") {
        $program = $program -replace "builder\.Services\.AddArtifactStorage\(\);", "builder.Services.AddArtifactStorage();`r`nbuilder.Services.AddArtifactManifestIndex();"
        Write-Host "Patched Program.cs artifact manifest index service registration."
    }
    else {
        throw "Could not find AddArtifactStorage registration anchor."
    }
}

if ($program -notmatch "MapArtifactManifestIndexEndpoints") {
    if ($program -match "api\.MapArtifactStorageProbeEndpoints\(\);") {
        $program = $program -replace "api\.MapArtifactStorageProbeEndpoints\(\);", "api.MapArtifactStorageProbeEndpoints();`r`napi.MapArtifactManifestIndexEndpoints();"
        Write-Host "Patched Program.cs artifact manifest index endpoints."
    }
    else {
        throw "Could not find artifact storage probe mapping anchor."
    }
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 005 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "  dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj"
Write-Host "Then verify:"
Write-Host "  Invoke-RestMethod http://localhost:5173/api/cloud/artifacts/index"
Write-Host "  Invoke-RestMethod -Method Post http://localhost:5173/api/cloud/artifacts/index/probe"
