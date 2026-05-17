$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set006-artifact-storage-bridge"

Write-Host "Applying P2 Set 006 from $repoRoot"

$files = @(
    "src\Migration.Admin.Api\Endpoints\ArtifactStorageBridgeEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\artifactStorageBridge.ts",
    "docs\cloud-roadmap-cleanup\P2_SET_006_ARTIFACT_STORAGE_BRIDGE.md"
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

if ($program -notmatch "MapArtifactStorageBridgeEndpoints") {
    if ($program -match "api\.MapArtifactManifestIndexEndpoints\(\);") {
        $program = $program -replace "api\.MapArtifactManifestIndexEndpoints\(\);", "api.MapArtifactManifestIndexEndpoints();`r`napi.MapArtifactStorageBridgeEndpoints();"
        Write-Host "Patched Program.cs artifact storage bridge endpoints."
    }
    else {
        throw "Could not find artifact manifest index mapping anchor."
    }
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 006 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "  dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj"
