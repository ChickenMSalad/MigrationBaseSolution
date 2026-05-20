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

if ($startup -notmatch "MapOperationalGlobalActivityQueryEndpoints\(") {
    if ($startup -match "api\.MapOperationalGlobalActivityFeedEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalActivityFeedEndpoints\(\);", "api.MapOperationalGlobalActivityFeedEndpoints();`r`n        api.MapOperationalGlobalActivityQueryEndpoints();"
    }
    else {
        throw "Could not locate global activity feed endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped global operational activity query endpoint."
}
else {
    Write-Host "Global operational activity query endpoint already mapped."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IOperationalGlobalActivityQueryService") {
    if ($registration -match "services\.AddScoped<IOperationalGlobalActivityFeedService, OperationalGlobalActivityFeedService>\(\);") {
        $registration = $registration -replace "services\.AddScoped<IOperationalGlobalActivityFeedService, OperationalGlobalActivityFeedService>\(\);", "services.AddScoped<IOperationalGlobalActivityFeedService, OperationalGlobalActivityFeedService>();`r`n        services.AddScoped<IOperationalGlobalActivityQueryService, OperationalGlobalActivityQueryService>();"
    }
    else {
        throw "Could not locate global activity feed service insertion point."
    }

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered global operational activity query service."
}
else {
    Write-Host "Global operational activity query service already registered."
}
