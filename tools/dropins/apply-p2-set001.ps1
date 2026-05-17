$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set001-blob-storage-abstractions"

Write-Host "Applying P2 Set 001 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Storage\CloudStorageLocation.cs",
    "src\Migration.ControlPlane\Storage\ICloudStoragePathResolver.cs",
    "src\Migration.ControlPlane\Storage\CloudStoragePathResolver.cs",
    "src\Migration.ControlPlane\Storage\CloudStorageRegistrationExtensions.cs",
    "src\Migration.Admin.Api\Endpoints\CloudStoragePlanEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\cloudStorageLocations.ts",
    "docs\cloud-roadmap-cleanup\P2_SET_001_BLOB_STORAGE_ABSTRACTIONS.md"
)

foreach ($relative in $files) {
    $source = Join-Path $payloadRoot $relative
    $target = Join-Path $repoRoot $relative
    $targetDirectory = Split-Path $target -Parent

    if (!(Test-Path $source)) {
        throw "Drop-in package is missing expected file: $source"
    }

    if (!(Test-Path $targetDirectory)) {
        New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
    }

    Copy-Item $source $target -Force
    Write-Host "Verified $relative"
}

$programPath = Join-Path $repoRoot "src\Migration.Admin.Api\Program.cs"
$program = Get-Content $programPath -Raw

if ($program -notmatch "AddCloudStoragePathResolution") {
    if ($program -match "builder\.Services\.AddMigrationAdminApiAuthentication\(builder\.Configuration, builder\.Environment\);") {
        $program = $program -replace "builder\.Services\.AddMigrationAdminApiAuthentication\(builder\.Configuration, builder\.Environment\);", "builder.Services.AddMigrationAdminApiAuthentication(builder.Configuration, builder.Environment);`r`nbuilder.Services.AddCloudStoragePathResolution(builder.Configuration);"
        Write-Host "Patched Program.cs cloud storage service registration."
    }
    elseif ($program -match "builder\.Services\.AddMigrationAdminApiRuntime\(builder\.Configuration\);") {
        $program = $program -replace "builder\.Services\.AddMigrationAdminApiRuntime\(builder\.Configuration\);", "builder.Services.AddMigrationAdminApiRuntime(builder.Configuration);`r`nbuilder.Services.AddCloudStoragePathResolution(builder.Configuration);"
        Write-Host "Patched Program.cs cloud storage service registration after runtime registration."
    }
    else {
        throw "Could not find service registration anchor in Program.cs. No partial patch was written."
    }
}

if ($program -notmatch "MapCloudStoragePlanEndpoints") {
    if ($program -match "api\.MapCloudPlatformEndpoints\(\);") {
        $program = $program -replace "api\.MapCloudPlatformEndpoints\(\);", "api.MapCloudPlatformEndpoints();`r`napi.MapCloudStoragePlanEndpoints();"
        Write-Host "Patched Program.cs cloud storage endpoint mapping."
    }
    else {
        throw "Could not find cloud endpoint mapping anchor in Program.cs. No partial patch was written."
    }
}

if ($program -notmatch "using Migration\.ControlPlane\.Storage;") {
    $program = "using Migration.ControlPlane.Storage;`r`n" + $program
    Write-Host "Patched Program.cs using Migration.ControlPlane.Storage;"
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 001 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "  dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj"
Write-Host "Then verify:"
Write-Host "  Invoke-RestMethod http://localhost:5173/api/cloud/storage/locations"
