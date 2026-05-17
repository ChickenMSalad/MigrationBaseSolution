$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set012-key-vault-secret-reader"

Write-Host "Applying P2 Set 012 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Credentials\ICloudCredentialValueProvider.cs",
    "src\Migration.ControlPlane\Credentials\NullCloudCredentialValueProvider.cs",
    "src\Migration.ControlPlane\Credentials\KeyVaultCloudCredentialValueProvider.cs",
    "src\Migration.ControlPlane\Credentials\CloudCredentialValueRegistrationExtensions.cs",
    "src\Migration.Admin.Api\Endpoints\CloudCredentialValueProbeEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\cloudCredentialValues.ts",
    "tools\test\smoke-cloud-credential-provider.ps1",
    "tools\test\smoke-cloud-credential-provider.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_012_KEY_VAULT_SECRET_READER.md"
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

if ($project -notmatch "Azure\.Security\.KeyVault\.Secrets") {
    if ($project -match "<PackageReference Include=`"Azure\.Identity`" />") {
        $project = $project -replace '<PackageReference Include="Azure.Identity" />', '<PackageReference Include="Azure.Identity" />' + "`r`n" + '    <PackageReference Include="Azure.Security.KeyVault.Secrets" />'
    }
    elseif ($project -match "</Project>") {
        $project = $project -replace "</Project>", "  <ItemGroup>`r`n    <PackageReference Include=`"Azure.Security.KeyVault.Secrets`" />`r`n  </ItemGroup>`r`n</Project>"
    }
    else {
        throw "Could not patch Migration.ControlPlane.csproj."
    }

    Set-Content -Path $projectPath -Value $project -Encoding UTF8
    Write-Host "Patched Migration.ControlPlane.csproj with Azure.Security.KeyVault.Secrets."
}

$packagesPropsPath = Join-Path $repoRoot "Directory.Packages.props"
$props = Get-Content $packagesPropsPath -Raw

if ($props -notmatch '<PackageVersion Include="Azure\.Security\.KeyVault\.Secrets"') {
    if ($props -match "</ItemGroup>") {
        $props = [regex]::Replace(
            $props,
            "</ItemGroup>",
            "    <PackageVersion Include=`"Azure.Security.KeyVault.Secrets`" Version=`"4.7.0`" />`r`n  </ItemGroup>",
            1)
    }
    else {
        throw "Could not patch Directory.Packages.props."
    }

    Set-Content -Path $packagesPropsPath -Value $props -Encoding UTF8
    Write-Host "Patched Directory.Packages.props with Azure.Security.KeyVault.Secrets."
}

$programPath = Join-Path $repoRoot "src\Migration.Admin.Api\Program.cs"
$program = Get-Content $programPath -Raw

if ($program -notmatch "AddCloudCredentialValueProvider") {
    if ($program -match "builder\.Services\.AddCloudCredentialPlanning\(builder\.Configuration\);") {
        $program = $program -replace "builder\.Services\.AddCloudCredentialPlanning\(builder\.Configuration\);", "builder.Services.AddCloudCredentialPlanning(builder.Configuration);`r`nbuilder.Services.AddCloudCredentialValueProvider(builder.Configuration);"
        Write-Host "Patched Program.cs credential value provider registration."
    }
    else {
        throw "Could not find AddCloudCredentialPlanning registration anchor."
    }
}

if ($program -notmatch "MapCloudCredentialValueProbeEndpoints") {
    if ($program -match "api\.MapCloudCredentialDiagnosticsEndpoints\(\);") {
        $program = $program -replace "api\.MapCloudCredentialDiagnosticsEndpoints\(\);", "api.MapCloudCredentialDiagnosticsEndpoints();`r`napi.MapCloudCredentialValueProbeEndpoints();"
        Write-Host "Patched Program.cs credential value probe endpoints."
    }
    else {
        throw "Could not find credential diagnostics endpoint mapping anchor."
    }
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 012 applied."
Write-Host "Run:"
Write-Host "  dotnet restore .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "  dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj"
