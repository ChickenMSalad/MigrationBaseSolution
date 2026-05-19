$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"
$registrationPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiOperationalStoreMirrorRegistrationExtensions.cs"

if (-not (Test-Path $startupPath)) { throw "Could not find $startupPath" }
if (-not (Test-Path $registrationPath)) { throw "Could not find $registrationPath" }

$startup = Get-Content $startupPath -Raw

if ($startup -notmatch "MapOperationalDispatcherEndpoints\(") {
    if ($startup -match "api\.MapOperationalDispatchEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalDispatchEndpoints\(\);", "api.MapOperationalDispatchEndpoints();`r`n        api.MapOperationalDispatcherEndpoints();"
    }
    else {
        throw "Could not locate api.MapOperationalDispatchEndpoints(); in AdminApiEndpointStartupExtensions.cs"
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational dispatcher endpoints."
}
else {
    Write-Host "Operational dispatcher endpoints already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "OperationalDispatcherOptions") {
    if ($registration -match "services\.Configure<OperationalRunAutoFinalizationOptions>") {
        $registration = $registration -replace "(services\.Configure<OperationalRunAutoFinalizationOptions>\(\s*configuration\.GetSection\(OperationalRunAutoFinalizationOptions\.SectionName\)\);)", "`$1`r`n`r`n        services.Configure<OperationalDispatcherOptions>(`r`n            configuration.GetSection(OperationalDispatcherOptions.SectionName));"
    }
    else {
        throw "Could not locate options configuration insertion point."
    }
}

if ($registration -notmatch "IOperationalDispatcherService") {
    if ($registration -match "services\.AddScoped<IOperationalRunAutoFinalizationService, OperationalRunAutoFinalizationService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalRunAutoFinalizationService, OperationalRunAutoFinalizationService>\(\);", "services.AddScoped<IOperationalRunAutoFinalizationService, OperationalRunAutoFinalizationService>();`r`n        services.AddScoped<IOperationalDispatcherService, OperationalDispatcherService>();"
    }
    else {
        throw "Could not locate service registration insertion point."
    }
}

if ($registration -notmatch "OperationalDispatcherHostedService") {
    $registration = $registration -replace "services\.AddHostedService<OperationalRunAutoFinalizationHostedService>\(\);", "services.AddHostedService<OperationalRunAutoFinalizationHostedService>();`r`n        services.AddHostedService<OperationalDispatcherHostedService>();"
}

Set-Content -Path $registrationPath -Value $registration -NoNewline
Write-Host "Registered operational dispatcher options/service/hosted service."
