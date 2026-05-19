$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"
$registrationPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiOperationalStoreMirrorRegistrationExtensions.cs"

if (-not (Test-Path $startupPath)) { throw "Could not find $startupPath" }
if (-not (Test-Path $registrationPath)) { throw "Could not find $registrationPath" }

$startup = Get-Content $startupPath -Raw

if ($startup -notmatch "MapOperationalDispatcherDiagnosticsEndpoints\(") {
    if ($startup -match "api\.MapOperationalDispatcherEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalDispatcherEndpoints\(\);", "api.MapOperationalDispatcherEndpoints();`r`n        api.MapOperationalDispatcherDiagnosticsEndpoints();"
    }
    elseif ($startup -match "api\.MapOperationalDispatchEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalDispatchEndpoints\(\);", "api.MapOperationalDispatchEndpoints();`r`n        api.MapOperationalDispatcherDiagnosticsEndpoints();"
    }
    else {
        throw "Could not locate operational dispatcher endpoint mapping section."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational dispatcher diagnostics endpoints."
}
else {
    Write-Host "Operational dispatcher diagnostics endpoints already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalDispatcherDiagnosticsService") {
    if ($registration -match "services\.AddScoped<IOperationalDispatcherService, OperationalDispatcherService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalDispatcherService, OperationalDispatcherService>\(\);", "services.AddScoped<IOperationalDispatcherService, OperationalDispatcherService>();`r`n        services.AddScoped<IOperationalDispatcherDiagnosticsService, OperationalDispatcherDiagnosticsService>();"
    }
    else {
        throw "Could not locate dispatcher service registration insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered operational dispatcher diagnostics service."
}
else {
    Write-Host "Operational dispatcher diagnostics service already registered."
}
