$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set003-local-binary-storage-provider"

Write-Host "Applying P2 Set 003 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Storage\LocalFileSystemCloudBinaryStorageProvider.cs",
    "src\Migration.ControlPlane\Storage\CloudBinaryStorageRegistrationExtensions.cs",
    "src\Migration.Admin.Api\Endpoints\CloudBinaryStorageProbeEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\cloudBinaryStorage.ts",
    "docs\cloud-roadmap-cleanup\P2_SET_003_LOCAL_BINARY_STORAGE_PROVIDER.md"
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

$program = $program -replace "builder\.Services\.AddCloudBinaryStorage\(\);", "builder.Services.AddCloudBinaryStorage(builder.Configuration);"

if ($program -notmatch "MapCloudBinaryStorageProbeEndpoints") {
    if ($program -match "api\.MapCloudStoragePlanEndpoints\(\);") {
        $program = $program -replace "api\.MapCloudStoragePlanEndpoints\(\);", "api.MapCloudStoragePlanEndpoints();`r`napi.MapCloudBinaryStorageProbeEndpoints();"
        Write-Host "Patched Program.cs cloud binary storage probe endpoints."
    }
    elseif ($program -match "api\.MapCloudPlatformEndpoints\(\);") {
        $program = $program -replace "api\.MapCloudPlatformEndpoints\(\);", "api.MapCloudPlatformEndpoints();`r`napi.MapCloudBinaryStorageProbeEndpoints();"
        Write-Host "Patched Program.cs cloud binary storage probe endpoints after cloud platform."
    }
    else {
        throw "Could not find cloud endpoint mapping anchor in Program.cs."
    }
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 003 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "  dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj"
Write-Host "Then verify:"
Write-Host "  Invoke-RestMethod http://localhost:5173/api/cloud/storage/provider"
Write-Host "  Invoke-RestMethod -Method Post http://localhost:5173/api/cloud/storage/probe"
