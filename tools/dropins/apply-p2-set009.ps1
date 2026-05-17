$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set009-azure-blob-provider-validation"

Write-Host "Applying P2 Set 009 from $repoRoot"

$files = @(
    "src\Migration.Admin.Api\Endpoints\AzureBlobStorageDiagnosticsEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\azureBlobStorageDiagnostics.ts",
    "tools\test\smoke-azure-blob-storage-provider.ps1",
    "tools\test\smoke-azure-blob-storage-provider.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_009_AZURE_BLOB_PROVIDER_VALIDATION.md"
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

if ($program -notmatch "MapAzureBlobStorageDiagnosticsEndpoints") {
    if ($program -match "api\.MapCloudBinaryStorageProbeEndpoints\(\);") {
        $program = $program -replace "api\.MapCloudBinaryStorageProbeEndpoints\(\);", "api.MapCloudBinaryStorageProbeEndpoints();`r`napi.MapAzureBlobStorageDiagnosticsEndpoints();"
        Write-Host "Patched Program.cs Azure Blob diagnostics endpoints."
    }
    elseif ($program -match "api\.MapCloudStoragePlanEndpoints\(\);") {
        $program = $program -replace "api\.MapCloudStoragePlanEndpoints\(\);", "api.MapCloudStoragePlanEndpoints();`r`napi.MapAzureBlobStorageDiagnosticsEndpoints();"
        Write-Host "Patched Program.cs Azure Blob diagnostics endpoints."
    }
    else {
        throw "Could not find storage endpoint mapping anchor."
    }
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 009 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "  dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj"
Write-Host "Then verify after starting Admin API:"
Write-Host "  Invoke-RestMethod http://localhost:5173/api/cloud/storage/azure-blob/diagnostics"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-azure-blob-storage-provider.ps1 -BaseUrl http://localhost:5173"
