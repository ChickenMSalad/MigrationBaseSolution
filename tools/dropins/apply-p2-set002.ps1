$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set002-blob-storage-provider-contracts"

Write-Host "Applying P2 Set 002 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Storage\ICloudBinaryStorageProvider.cs",
    "src\Migration.ControlPlane\Storage\CloudBinaryStorageProviderCapabilities.cs",
    "src\Migration.ControlPlane\Storage\NullCloudBinaryStorageProvider.cs",
    "src\Migration.ControlPlane\Storage\CloudBinaryStorageRegistrationExtensions.cs",
    "src\Migration.ControlPlane\Storage\CloudStorageObjectReference.cs",
    "docs\cloud-roadmap-cleanup\P2_SET_002_BLOB_STORAGE_PROVIDER_CONTRACTS.md"
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

if ($program -notmatch "AddCloudBinaryStorage") {
    if ($program -match "AddCloudStoragePathResolution\(builder\.Configuration\);") {
        $program = $program -replace "AddCloudStoragePathResolution\(builder\.Configuration\);", "AddCloudStoragePathResolution(builder.Configuration);`r`nbuilder.Services.AddCloudBinaryStorage();"
    }
    else {
        throw "Could not find cloud storage registration anchor."
    }
}

if ($program -notmatch "using Migration\.ControlPlane\.Storage;") {
    $program = "using Migration.ControlPlane.Storage;`r`n" + $program
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 002 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
