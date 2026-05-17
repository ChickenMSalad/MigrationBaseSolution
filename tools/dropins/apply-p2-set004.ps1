$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set004-artifact-storage-contracts"

Write-Host "Applying P2 Set 004 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Storage\ArtifactStorageContracts.cs",
    "src\Migration.ControlPlane\Storage\IArtifactStorageService.cs",
    "src\Migration.ControlPlane\Storage\ArtifactStorageService.cs",
    "src\Migration.ControlPlane\Storage\ArtifactStorageRegistrationExtensions.cs",
    "src\Migration.Admin.Api\Endpoints\ArtifactStorageProbeEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\artifactStorage.ts",
    "docs\cloud-roadmap-cleanup\P2_SET_004_ARTIFACT_STORAGE_CONTRACTS.md"
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

if ($program -notmatch "AddArtifactStorage") {
    if ($program -match "builder\.Services\.AddCloudBinaryStorage\(builder\.Configuration\);") {
        $program = $program -replace "builder\.Services\.AddCloudBinaryStorage\(builder\.Configuration\);", "builder.Services.AddCloudBinaryStorage(builder.Configuration);`r`nbuilder.Services.AddArtifactStorage();"
        Write-Host "Patched Program.cs artifact storage service registration."
    }
    else {
        throw "Could not find AddCloudBinaryStorage registration anchor."
    }
}

if ($program -notmatch "MapArtifactStorageProbeEndpoints") {
    if ($program -match "api\.MapCloudBinaryStorageProbeEndpoints\(\);") {
        $program = $program -replace "api\.MapCloudBinaryStorageProbeEndpoints\(\);", "api.MapCloudBinaryStorageProbeEndpoints();`r`napi.MapArtifactStorageProbeEndpoints();"
        Write-Host "Patched Program.cs artifact storage probe endpoints."
    }
    else {
        throw "Could not find binary storage probe mapping anchor."
    }
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 004 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "  dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj"
Write-Host "Then verify:"
Write-Host "  Invoke-RestMethod ""http://localhost:5173/api/cloud/artifacts/resolve?kind=manifest&artifactId=test&fileName=test.json"""
Write-Host "  Invoke-RestMethod -Method Post http://localhost:5173/api/cloud/artifacts/probe"
