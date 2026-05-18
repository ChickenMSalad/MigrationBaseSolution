$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\post-p2-cleanup-set008-program-local-helpers"

Write-Host "Applying Post-P2 Cleanup Set 008 from $repoRoot"

$docSource = Join-Path $payloadRoot "docs\post-p2-cleanup\POST_P2_CLEANUP_SET_008_PROGRAM_LOCAL_HELPERS.md"
$docTarget = Join-Path $repoRoot "docs\post-p2-cleanup\POST_P2_CLEANUP_SET_008_PROGRAM_LOCAL_HELPERS.md"

if (!(Test-Path (Split-Path $docTarget -Parent))) {
    New-Item -ItemType Directory -Path (Split-Path $docTarget -Parent) -Force | Out-Null
}

Copy-Item $docSource $docTarget -Force
Write-Host "Verified docs\post-p2-cleanup\POST_P2_CLEANUP_SET_008_PROGRAM_LOCAL_HELPERS.md"

$programPath = Join-Path $repoRoot "src\Migration.Admin.Api\Program.cs"

if (!(Test-Path $programPath)) {
    throw "Program.cs was not found at $programPath"
}

$program = [System.IO.File]::ReadAllText($programPath)

if ([string]::IsNullOrWhiteSpace($program)) {
    throw "Program.cs is empty or unreadable. Restore Program.cs before continuing."
}

if ($program.Contains("static void AddMigrationAdminApiCloudServices(") -or
    $program.Contains("static void MapMigrationAdminApiCloudEndpoints(")) {
    Write-Host "Program.cs already appears to contain Set 008 local helper functions. No changes made."
    exit 0
}

$serviceStart = "builder.Services.AddCloudStoragePathResolution(builder.Configuration);"
$serviceEnd = "builder.Services.AddP2ReadinessReport();"
$endpointStart = "api.MapCloudPlatformEndpoints();"
$endpointEnd = "api.MapP2ReadinessReportEndpoints();"

$serviceStartIndex = $program.IndexOf($serviceStart, [StringComparison]::Ordinal)
if ($serviceStartIndex -lt 0) {
    throw "Could not find service block start marker: $serviceStart"
}

$serviceEndIndex = $program.IndexOf($serviceEnd, $serviceStartIndex, [StringComparison]::Ordinal)
if ($serviceEndIndex -lt 0) {
    throw "Could not find service block end marker: $serviceEnd"
}
$serviceEndIndex += $serviceEnd.Length
while ($serviceEndIndex -lt $program.Length -and ($program[$serviceEndIndex] -eq "`r" -or $program[$serviceEndIndex] -eq "`n")) {
    $serviceEndIndex++
}
$serviceBlock = $program.Substring($serviceStartIndex, $serviceEndIndex - $serviceStartIndex)

$endpointStartIndex = $program.IndexOf($endpointStart, [StringComparison]::Ordinal)
if ($endpointStartIndex -lt 0) {
    throw "Could not find endpoint block start marker: $endpointStart"
}

$endpointEndIndex = $program.IndexOf($endpointEnd, $endpointStartIndex, [StringComparison]::Ordinal)
if ($endpointEndIndex -lt 0) {
    throw "Could not find endpoint block end marker: $endpointEnd"
}
$endpointEndIndex += $endpointEnd.Length
while ($endpointEndIndex -lt $program.Length -and ($program[$endpointEndIndex] -eq "`r" -or $program[$endpointEndIndex] -eq "`n")) {
    $endpointEndIndex++
}
$endpointBlock = $program.Substring($endpointStartIndex, $endpointEndIndex - $endpointStartIndex)

$serviceBody = $serviceBlock.TrimEnd()
$serviceBody = $serviceBody -replace "builder\.Services\.", "services."
$serviceBody = $serviceBody -replace "builder\.Configuration", "configuration"

$endpointBody = $endpointBlock.TrimEnd()

# Replace endpoint block first.
$program = $program.Remove($endpointStartIndex, $endpointEndIndex - $endpointStartIndex)
$program = $program.Insert($endpointStartIndex, "MapMigrationAdminApiCloudEndpoints(api);`r`n")

# Recompute and replace service block.
$serviceStartIndex = $program.IndexOf($serviceStart, [StringComparison]::Ordinal)
if ($serviceStartIndex -lt 0) {
    throw "Could not refind service block start after endpoint replacement."
}
$serviceEndIndex = $program.IndexOf($serviceEnd, $serviceStartIndex, [StringComparison]::Ordinal)
if ($serviceEndIndex -lt 0) {
    throw "Could not refind service block end after endpoint replacement."
}
$serviceEndIndex += $serviceEnd.Length
while ($serviceEndIndex -lt $program.Length -and ($program[$serviceEndIndex] -eq "`r" -or $program[$serviceEndIndex] -eq "`n")) {
    $serviceEndIndex++
}

$program = $program.Remove($serviceStartIndex, $serviceEndIndex - $serviceStartIndex)
$program = $program.Insert($serviceStartIndex, "AddMigrationAdminApiCloudServices(builder.Services, builder.Configuration);`r`n")

$serviceLines = ($serviceBody -split "`r?`n") | ForEach-Object { "    $_" }
$endpointLines = ($endpointBody -split "`r?`n") | ForEach-Object { "    $_" }

$helperLines = @()
$helperLines += ""
$helperLines += "static void AddMigrationAdminApiCloudServices("
$helperLines += "    Microsoft.Extensions.DependencyInjection.IServiceCollection services,"
$helperLines += "    Microsoft.Extensions.Configuration.IConfiguration configuration)"
$helperLines += "{"
$helperLines += $serviceLines
$helperLines += "}"
$helperLines += ""
$helperLines += "static void MapMigrationAdminApiCloudEndpoints("
$helperLines += "    Microsoft.AspNetCore.Routing.RouteGroupBuilder api)"
$helperLines += "{"
$helperLines += $endpointLines
$helperLines += "}"

$program = $program.TrimEnd() + "`r`n" + ($helperLines -join "`r`n") + "`r`n"

[System.IO.File]::WriteAllText($programPath, $program, [System.Text.UTF8Encoding]::new($false))

Write-Host "Consolidated Program.cs cloud service and endpoint blocks into local helpers."
Write-Host ""
Write-Host "Post-P2 Cleanup Set 008 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "Then start Admin API and run:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\validate-p2-completion.ps1 -BaseUrl http://localhost:5173 -AllowUnconfigured"
