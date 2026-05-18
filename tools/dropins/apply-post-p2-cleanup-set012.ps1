$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\post-p2-cleanup-set012-program-endpoint-grouping"

Write-Host "Applying Post-P2 Cleanup Set 012 from $repoRoot"

$files = @(
    "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs",
    "docs\post-p2-cleanup\POST_P2_CLEANUP_SET_012_PROGRAM_ENDPOINT_GROUPING.md"
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

if (!(Test-Path $programPath)) {
    throw "Program.cs was not found at $programPath"
}

$program = [System.IO.File]::ReadAllText($programPath)

if ([string]::IsNullOrWhiteSpace($program)) {
    throw "Program.cs is empty or unreadable."
}

if ($program.Contains("AdminApiEndpointStartupExtensions.MapMigrationAdminApiRouteGroupEndpoints(api);")) {
    Write-Host "Program.cs already contains grouped route-group endpoint mapping."
}
else {
    $routeStart = "api.MapRunMonitoringEndpoints();"
    $routeEnd = "api.MapConnectorCapabilityEndpoints();"

    $routeStartIndex = $program.IndexOf($routeStart, [StringComparison]::Ordinal)
    if ($routeStartIndex -lt 0) {
        throw "Could not find route-group endpoint block start marker: $routeStart"
    }

    $routeEndIndex = $program.IndexOf($routeEnd, $routeStartIndex, [StringComparison]::Ordinal)
    if ($routeEndIndex -lt 0) {
        throw "Could not find route-group endpoint block end marker: $routeEnd"
    }

    $routeEndIndex += $routeEnd.Length

    while ($routeEndIndex -lt $program.Length -and ($program[$routeEndIndex] -eq "`r" -or $program[$routeEndIndex] -eq "`n")) {
        $routeEndIndex++
    }

    $program = $program.Remove($routeStartIndex, $routeEndIndex - $routeStartIndex)
    $program = $program.Insert($routeStartIndex, "AdminApiEndpointStartupExtensions.MapMigrationAdminApiRouteGroupEndpoints(api);`r`n")

    Write-Host "Grouped /api route-group endpoint mappings in Program.cs."
}

if ($program.Contains("AdminApiEndpointStartupExtensions.MapMigrationAdminApiAppLevelEndpoints(app);")) {
    Write-Host "Program.cs already contains grouped app-level endpoint mapping."
}
else {
    $appBlockPattern = '(?s)// These extensions include their /api route prefix internally\. Keep them on app, not on the /api group\.\s*app\.MapArtifactEndpoints\(\);\s*app\.MapControlPlaneDeleteEndpoints\(\);\s*app\.MapMappingBuilderEndpoints\(\);\s*app\.MapManifestBuilderEndpoints\(\);\s*app\.MapTaxonomyBuilderEndpoints\(\);\s*'

    if ($program -notmatch $appBlockPattern) {
        throw "Could not find app-level endpoint mapping block."
    }

    $program = [regex]::Replace(
        $program,
        $appBlockPattern,
        "AdminApiEndpointStartupExtensions.MapMigrationAdminApiAppLevelEndpoints(app);`r`n",
        1)

    Write-Host "Grouped app-level endpoint mappings in Program.cs."
}

# Remove using directives that Program.cs no longer needs after endpoint/service grouping.
$removeUsings = @(
    "using Migration.ControlPlane.Auth;",
    "using Migration.ControlPlane.Operations;",
    "using Migration.ControlPlane.Telemetry;",
    "using Migration.ControlPlane.Audit;",
    "using Migration.ControlPlane.Queues;",
    "using Migration.ControlPlane.Credentials;",
    "using Migration.ControlPlane.Storage;",
    "using Migration.Admin.Api.Endpoints;"
)

foreach ($usingLine in $removeUsings) {
    $program = $program.Replace($usingLine + "`r`n", "")
    $program = $program.Replace($usingLine + "`n", "")
}

[System.IO.File]::WriteAllText($programPath, $program.TrimEnd() + "`r`n", [System.Text.UTF8Encoding]::new($false))

Write-Host ""
Write-Host "Post-P2 Cleanup Set 012 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "Then start Admin API and run:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\validate-p2-completion.ps1 -BaseUrl http://localhost:5173 -AllowUnconfigured"
