$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"
$registrationPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiOperationalStoreMirrorRegistrationExtensions.cs"

if (-not (Test-Path $startupPath)) {
    throw "Could not find $startupPath"
}

if (-not (Test-Path $registrationPath)) {
    throw "Could not find $registrationPath"
}

$startup = Get-Content $startupPath -Raw

if ($startup -notmatch "MapOperationalRunAutoFinalizationEndpoints\(") {
    if ($startup -match "api\.MapOperationalRunFailureFinalizationEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalRunFailureFinalizationEndpoints\(\);", "api.MapOperationalRunFailureFinalizationEndpoints();`r`n        api.MapOperationalRunAutoFinalizationEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalRunCompletionFinalizationEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalRunCompletionFinalizationEndpoints\(\);", "api.MapOperationalRunCompletionFinalizationEndpoints();`r`n        api.MapOperationalRunAutoFinalizationEndpoints();"
    }
    else {
        throw "Could not locate operational run endpoint mapping section in AdminApiEndpointStartupExtensions.cs"
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational run auto-finalization endpoints."
}
else {
    Write-Host "Operational run auto-finalization endpoints already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "OperationalRunAutoFinalizationOptions") {
    $registration = $registration -replace "services\.Configure<OperationalLeaseExpirationOptions>\(\s*configuration\.GetSection\(OperationalLeaseExpirationOptions\.SectionName\)\);", "services.Configure<OperationalLeaseExpirationOptions>(`r`n            configuration.GetSection(OperationalLeaseExpirationOptions.SectionName));`r`n`r`n        services.Configure<OperationalRunAutoFinalizationOptions>(`r`n            configuration.GetSection(OperationalRunAutoFinalizationOptions.SectionName));"
}

if ($registration -notmatch "IOperationalRunAutoFinalizationService") {
    $registration = $registration -replace "services\.AddScoped<IOperationalRunFailureFinalizationService, OperationalRunFailureFinalizationService>\(\);", "services.AddScoped<IOperationalRunFailureFinalizationService, OperationalRunFailureFinalizationService>();`r`n        services.AddScoped<IOperationalRunAutoFinalizationService, OperationalRunAutoFinalizationService>();"

    if ($registration -notmatch "IOperationalRunAutoFinalizationService") {
        throw "Could not insert IOperationalRunAutoFinalizationService registration."
    }
}

if ($registration -notmatch "OperationalRunAutoFinalizationHostedService") {
    $registration = $registration -replace "return services;", "services.AddHostedService<OperationalRunAutoFinalizationHostedService>();`r`n`r`n        return services;"
}

Set-Content -Path $registrationPath -Value $registration -NoNewline

Write-Host "Registered operational run auto-finalization options/service/hosted service."
