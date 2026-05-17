$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set016-azure-queue-dispatch"

Write-Host "Applying P2 Set 016 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Queues\AzureQueueDispatchOptions.cs",
    "src\Migration.ControlPlane\Queues\AzureQueueDispatchProvider.cs",
    "src\Migration.ControlPlane\Queues\QueueDispatchRegistrationExtensions.cs",
    "tools\test\smoke-azure-queue-dispatch.ps1",
    "tools\test\smoke-azure-queue-dispatch.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_016_AZURE_QUEUE_DISPATCH.md"
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

if ($project -notmatch 'PackageReference Include="Azure\.Storage\.Queues"') {
    if ($project -match 'PackageReference Include="Azure\.Storage\.Blobs"') {
        $project = [regex]::Replace(
            $project,
            '(<PackageReference Include="Azure\.Storage\.Blobs"\s*/>)',
            "`$1`r`n    <PackageReference Include=`"Azure.Storage.Queues`" />",
            1)
    }
    elseif ($project -match '</Project>') {
        $project = $project.Replace(
            '</Project>',
            "  <ItemGroup>`r`n    <PackageReference Include=`"Azure.Storage.Queues`" />`r`n  </ItemGroup>`r`n</Project>")
    }
    else {
        throw "Could not patch Migration.ControlPlane.csproj."
    }

    Set-Content -Path $projectPath -Value $project -Encoding UTF8
    Write-Host "Patched Migration.ControlPlane.csproj with Azure.Storage.Queues."
}

$packagesPropsPath = Join-Path $repoRoot "Directory.Packages.props"
$props = Get-Content $packagesPropsPath -Raw

if ($props -notmatch 'PackageVersion Include="Azure\.Storage\.Queues"') {
    if ($props -match '</ItemGroup>') {
        $props = [regex]::Replace(
            $props,
            '</ItemGroup>',
            "    <PackageVersion Include=`"Azure.Storage.Queues`" Version=`"12.22.0`" />`r`n  </ItemGroup>",
            1)
    }
    elseif ($props -match '</Project>') {
        $props = $props.Replace(
            '</Project>',
            "  <ItemGroup>`r`n    <PackageVersion Include=`"Azure.Storage.Queues`" Version=`"12.22.0`" />`r`n  </ItemGroup>`r`n</Project>")
    }
    else {
        throw "Could not patch Directory.Packages.props."
    }

    Set-Content -Path $packagesPropsPath -Value $props -Encoding UTF8
    Write-Host "Patched Directory.Packages.props with Azure.Storage.Queues."
}

Write-Host ""
Write-Host "P2 Set 016 applied."
Write-Host "Run:"
Write-Host "  dotnet restore .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "Then validate after starting Admin API:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-azure-queue-dispatch.ps1 -BaseUrl http://localhost:5173 -AllowUnconfigured"
