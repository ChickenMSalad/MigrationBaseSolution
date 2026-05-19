$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"
$registrationPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiOperationalStoreMirrorRegistrationExtensions.cs"

if (-not (Test-Path $startupPath)) { throw "Could not find $startupPath" }
if (-not (Test-Path $registrationPath)) { throw "Could not find $registrationPath" }

$startup = Get-Content $startupPath -Raw

if ($startup -notmatch "MapOperationalRetentionEndpoints\(") {
    if ($startup -match "api\.MapOperationalMetricsEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalMetricsEndpoints\(\);", "api.MapOperationalMetricsEndpoints();`r`n        api.MapOperationalRetentionEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalRunAutoFinalizationEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalRunAutoFinalizationEndpoints\(\);", "api.MapOperationalRunAutoFinalizationEndpoints();`r`n        api.MapOperationalRetentionEndpoints();"
    }
    else {
        throw "Could not locate operational endpoint mapping section in AdminApiEndpointStartupExtensions.cs"
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational retention endpoints."
}
else {
    Write-Host "Operational retention endpoints already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "OperationalRetentionOptions") {
    if ($registration -match "services\.Configure<OperationalRunAutoFinalizationOptions>") {
        $registration = $registration -replace "(services\.Configure<OperationalRunAutoFinalizationOptions>\(\s*configuration\.GetSection\(OperationalRunAutoFinalizationOptions\.SectionName\)\);)", "`$1`r`n`r`n        services.Configure<OperationalRetentionOptions>(`r`n            configuration.GetSection(OperationalRetentionOptions.SectionName));"
    }
    elseif ($registration -match "services\.Configure<OperationalLeaseExpirationOptions>") {
        $registration = $registration -replace "(services\.Configure<OperationalLeaseExpirationOptions>\(\s*configuration\.GetSection\(OperationalLeaseExpirationOptions\.SectionName\)\);)", "`$1`r`n`r`n        services.Configure<OperationalRetentionOptions>(`r`n            configuration.GetSection(OperationalRetentionOptions.SectionName));"
    }
    else {
        throw "Could not locate options configuration insertion point."
    }
}

if ($registration -notmatch "IOperationalRetentionService") {
    if ($registration -match "services\.AddScoped<IOperationalRunAutoFinalizationService, OperationalRunAutoFinalizationService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalRunAutoFinalizationService, OperationalRunAutoFinalizationService>\(\);", "services.AddScoped<IOperationalRunAutoFinalizationService, OperationalRunAutoFinalizationService>();`r`n        services.AddScoped<IOperationalRetentionService, OperationalRetentionService>();"
    }
    elseif ($registration -match "services\.AddScoped<IOperationalMetricsService, OperationalMetricsService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalMetricsService, OperationalMetricsService>\(\);", "services.AddScoped<IOperationalMetricsService, OperationalMetricsService>();`r`n        services.AddScoped<IOperationalRetentionService, OperationalRetentionService>();"
    }
    else {
        throw "Could not locate service registration insertion point."
    }
}

Set-Content -Path $registrationPath -Value $registration -NoNewline
Write-Host "Registered operational retention options/service."
