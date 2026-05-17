$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set011-key-vault-credential-contracts"

Write-Host "Applying P2 Set 011 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Credentials\CloudCredentialContracts.cs",
    "src\Migration.ControlPlane\Credentials\ICloudCredentialNameResolver.cs",
    "src\Migration.ControlPlane\Credentials\CloudCredentialNameResolver.cs",
    "src\Migration.ControlPlane\Credentials\CloudCredentialRegistrationExtensions.cs",
    "src\Migration.Admin.Api\Endpoints\CloudCredentialDiagnosticsEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\cloudCredentials.ts",
    "docs\cloud-roadmap-cleanup\P2_SET_011_KEY_VAULT_CREDENTIAL_CONTRACTS.md"
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

if ($program -notmatch "AddCloudCredentialPlanning") {
    if ($program -match "builder\.Services\.AddArtifactManifestIndex\(\);") {
        $program = $program -replace "builder\.Services\.AddArtifactManifestIndex\(\);", "builder.Services.AddArtifactManifestIndex();`r`nbuilder.Services.AddCloudCredentialPlanning(builder.Configuration);"
        Write-Host "Patched Program.cs cloud credential service registration."
    }
    else {
        throw "Could not find service registration anchor."
    }
}

if ($program -notmatch "MapCloudCredentialDiagnosticsEndpoints") {
    if ($program -match "api\.MapCloudPlatformEndpoints\(\);") {
        $program = $program -replace "api\.MapCloudPlatformEndpoints\(\);", "api.MapCloudPlatformEndpoints();`r`napi.MapCloudCredentialDiagnosticsEndpoints();"
        Write-Host "Patched Program.cs cloud credential diagnostics endpoints."
    }
    elseif ($program -match "api\.MapAuthenticationConfigurationEndpoints\(\);") {
        $program = $program -replace "api\.MapAuthenticationConfigurationEndpoints\(\);", "api.MapAuthenticationConfigurationEndpoints();`r`napi.MapCloudCredentialDiagnosticsEndpoints();"
        Write-Host "Patched Program.cs cloud credential diagnostics endpoints."
    }
    else {
        throw "Could not find endpoint mapping anchor."
    }
}

if ($program -notmatch "using Migration\.ControlPlane\.Credentials;") {
    $program = "using Migration.ControlPlane.Credentials;`r`n" + $program
    Write-Host "Patched Program.cs using Migration.ControlPlane.Credentials;"
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 011 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "  dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj"
