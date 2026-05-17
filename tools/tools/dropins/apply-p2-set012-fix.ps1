$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")

Write-Host "Applying P2 Set 012 corrective patch from $repoRoot"

$projectPath = Join-Path $repoRoot "src\Migration.ControlPlane\Migration.ControlPlane.csproj"
$packagesPropsPath = Join-Path $repoRoot "Directory.Packages.props"
$programPath = Join-Path $repoRoot "src\Migration.Admin.Api\Program.cs"

if (!(Test-Path $projectPath)) {
    throw "Missing project file: $projectPath"
}

if (!(Test-Path $packagesPropsPath)) {
    throw "Missing Directory.Packages.props: $packagesPropsPath"
}

if (!(Test-Path $programPath)) {
    throw "Missing Program.cs: $programPath"
}

$project = Get-Content $projectPath -Raw

if ($project -notmatch 'PackageReference Include="Azure\.Security\.KeyVault\.Secrets"') {
    if ($project -match 'PackageReference Include="Azure\.Identity"') {
        $project = [regex]::Replace(
            $project,
            '(<PackageReference Include="Azure\.Identity"\s*/>)',
            "`$1`r`n    <PackageReference Include=`"Azure.Security.KeyVault.Secrets`" />",
            1)
    }
    elseif ($project -match '</Project>') {
        $project = $project.Replace(
            '</Project>',
            "  <ItemGroup>`r`n    <PackageReference Include=`"Azure.Security.KeyVault.Secrets`" />`r`n  </ItemGroup>`r`n</Project>")
    }
    else {
        throw "Could not patch Migration.ControlPlane.csproj."
    }

    Set-Content -Path $projectPath -Value $project -Encoding UTF8
    Write-Host "Patched Migration.ControlPlane.csproj"
}
else {
    Write-Host "Migration.ControlPlane.csproj already references Azure.Security.KeyVault.Secrets."
}

$props = Get-Content $packagesPropsPath -Raw

if ($props -notmatch 'PackageVersion Include="Azure\.Security\.KeyVault\.Secrets"') {
    if ($props -match '</ItemGroup>') {
        $props = [regex]::Replace(
            $props,
            '</ItemGroup>',
            "    <PackageVersion Include=`"Azure.Security.KeyVault.Secrets`" Version=`"4.7.0`" />`r`n  </ItemGroup>",
            1)
    }
    elseif ($props -match '</Project>') {
        $props = $props.Replace(
            '</Project>',
            "  <ItemGroup>`r`n    <PackageVersion Include=`"Azure.Security.KeyVault.Secrets`" Version=`"4.7.0`" />`r`n  </ItemGroup>`r`n</Project>")
    }
    else {
        throw "Could not patch Directory.Packages.props."
    }

    Set-Content -Path $packagesPropsPath -Value $props -Encoding UTF8
    Write-Host "Patched Directory.Packages.props"
}
else {
    Write-Host "Directory.Packages.props already has Azure.Security.KeyVault.Secrets version."
}

$program = Get-Content $programPath -Raw

if ($program -notmatch 'AddCloudCredentialValueProvider') {
    if ($program -match 'builder\.Services\.AddCloudCredentialPlanning\(builder\.Configuration\);') {
        $program = $program.Replace(
            'builder.Services.AddCloudCredentialPlanning(builder.Configuration);',
            "builder.Services.AddCloudCredentialPlanning(builder.Configuration);`r`nbuilder.Services.AddCloudCredentialValueProvider(builder.Configuration);")
        Write-Host "Patched Program.cs credential value provider registration."
    }
    else {
        throw "Could not find AddCloudCredentialPlanning registration anchor in Program.cs."
    }
}
else {
    Write-Host "Program.cs already registers credential value provider."
}

if ($program -notmatch 'MapCloudCredentialValueProbeEndpoints') {
    if ($program -match 'api\.MapCloudCredentialDiagnosticsEndpoints\(\);') {
        $program = $program.Replace(
            'api.MapCloudCredentialDiagnosticsEndpoints();',
            "api.MapCloudCredentialDiagnosticsEndpoints();`r`napi.MapCloudCredentialValueProbeEndpoints();")
        Write-Host "Patched Program.cs credential value probe endpoint mapping."
    }
    else {
        throw "Could not find credential diagnostics endpoint mapping anchor in Program.cs."
    }
}
else {
    Write-Host "Program.cs already maps credential value probe endpoints."
}

if ($program -notmatch 'using Migration\.ControlPlane\.Credentials;') {
    $program = "using Migration.ControlPlane.Credentials;`r`n" + $program
    Write-Host "Patched Program.cs using Migration.ControlPlane.Credentials;"
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 012 corrective patch applied."
Write-Host "Run:"
Write-Host "  dotnet restore .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "  dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj"
