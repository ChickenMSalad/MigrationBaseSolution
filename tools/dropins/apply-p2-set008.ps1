$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set008-azure-blob-provider-scaffold"

Write-Host "Applying P2 Set 008 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Storage\AzureBlobStorageOptions.cs",
    "src\Migration.ControlPlane\Storage\AzureBlobCloudBinaryStorageProvider.cs",
    "src\Migration.ControlPlane\Storage\CloudBinaryStorageRegistrationExtensions.cs",
    "docs\cloud-roadmap-cleanup\P2_SET_008_AZURE_BLOB_PROVIDER_SCAFFOLD.md"
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

$projectPath = Join-Path $repoRoot "src\Migration.ControlPlane\Migration.ControlPlane.csproj"
$project = Get-Content $projectPath -Raw

if ($project -notmatch "Azure\.Storage\.Blobs") {
    if ($project -match "</Project>") {
        $itemGroup = @"

  <ItemGroup>
    <PackageReference Include="Azure.Identity" Version="1.13.2" />
    <PackageReference Include="Azure.Storage.Blobs" Version="12.24.0" />
  </ItemGroup>
"@
        $project = $project -replace "</Project>", "$itemGroup`r`n</Project>"
        Set-Content -Path $projectPath -Value $project -Encoding UTF8
        Write-Host "Patched Migration.ControlPlane.csproj with Azure Blob package references."
    }
    else {
        throw "Could not patch Migration.ControlPlane.csproj."
    }
}
else {
    Write-Host "Migration.ControlPlane.csproj already has Azure Blob package references."
}

Write-Host ""
Write-Host "P2 Set 008 applied."
Write-Host "Run:"
Write-Host "  dotnet restore .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "  dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj"
