$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")

Write-Host "Applying P2 Set 008 CPM corrective patch from $repoRoot"

$controlPlaneProjectPath = Join-Path $repoRoot "src\Migration.ControlPlane\Migration.ControlPlane.csproj"
$packagesPropsPath = Join-Path $repoRoot "Directory.Packages.props"

if (!(Test-Path $controlPlaneProjectPath)) {
    throw "Expected project file not found: $controlPlaneProjectPath"
}

if (!(Test-Path $packagesPropsPath)) {
    throw "Expected central package file not found: $packagesPropsPath"
}

$project = Get-Content $controlPlaneProjectPath -Raw

# Remove inline versions from PackageReference items. Central package management requires
# versions to live in Directory.Packages.props.
$project = $project -replace '<PackageReference Include="Azure\.Identity" Version="[^"]+"\s*/>', '<PackageReference Include="Azure.Identity" />'
$project = $project -replace '<PackageReference Include="Azure\.Storage\.Blobs" Version="[^"]+"\s*/>', '<PackageReference Include="Azure.Storage.Blobs" />'

if ($project -notmatch '<PackageReference Include="Azure\.Identity"\s*/>') {
    if ($project -match '</Project>') {
        $project = $project -replace '</Project>', '  <ItemGroup>' + "`r`n" + '    <PackageReference Include="Azure.Identity" />' + "`r`n" + '    <PackageReference Include="Azure.Storage.Blobs" />' + "`r`n" + '  </ItemGroup>' + "`r`n" + '</Project>'
    }
    else {
        throw "Could not patch Migration.ControlPlane.csproj."
    }
}

Set-Content -Path $controlPlaneProjectPath -Value $project -Encoding UTF8
Write-Host "Patched src\Migration.ControlPlane\Migration.ControlPlane.csproj"

$props = Get-Content $packagesPropsPath -Raw

function Add-PackageVersionIfMissing {
    param(
        [string]$Content,
        [string]$PackageName,
        [string]$Version
    )

    if ($Content -match "<PackageVersion Include=`"$([regex]::Escape($PackageName))`"") {
        return $Content
    }

    if ($Content -match '</ItemGroup>') {
        return [regex]::Replace(
            $Content,
            '</ItemGroup>',
            "    <PackageVersion Include=`"$PackageName`" Version=`"$Version`" />`r`n  </ItemGroup>",
            1)
    }

    if ($Content -match '</Project>') {
        return $Content -replace '</Project>', "  <ItemGroup>`r`n    <PackageVersion Include=`"$PackageName`" Version=`"$Version`" />`r`n  </ItemGroup>`r`n</Project>"
    }

    throw "Could not patch Directory.Packages.props."
}

$props = Add-PackageVersionIfMissing -Content $props -PackageName "Azure.Identity" -Version "1.13.2"
$props = Add-PackageVersionIfMissing -Content $props -PackageName "Azure.Storage.Blobs" -Version "12.24.0"

Set-Content -Path $packagesPropsPath -Value $props -Encoding UTF8
Write-Host "Patched Directory.Packages.props"

Write-Host ""
Write-Host "P2 Set 008 CPM corrective patch applied."
Write-Host "Run:"
Write-Host "  dotnet restore .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "  dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj"
