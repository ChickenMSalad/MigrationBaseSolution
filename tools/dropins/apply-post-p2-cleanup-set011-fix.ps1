$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\post-p2-cleanup-set011-fix"

Write-Host "Applying Post-P2 Cleanup Set 011 fix from $repoRoot"

$docSource = Join-Path $payloadRoot "docs\post-p2-cleanup\POST_P2_CLEANUP_SET_011_FIX.md"
$docTarget = Join-Path $repoRoot "docs\post-p2-cleanup\POST_P2_CLEANUP_SET_011_FIX.md"

if (!(Test-Path (Split-Path $docTarget -Parent))) {
    New-Item -ItemType Directory -Path (Split-Path $docTarget -Parent) -Force | Out-Null
}

Copy-Item $docSource $docTarget -Force
Write-Host "Verified docs\post-p2-cleanup\POST_P2_CLEANUP_SET_011_FIX.md"

$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiCloudStartupExtensions.cs"

if (!(Test-Path $startupPath)) {
    throw "AdminApiCloudStartupExtensions.cs was not found at $startupPath"
}

$content = [System.IO.File]::ReadAllText($startupPath)

if ([string]::IsNullOrWhiteSpace($content)) {
    throw "AdminApiCloudStartupExtensions.cs is empty or unreadable."
}

# Remove duplicate using block if this fix is rerun.
$content = $content -replace '(?s)^using Migration\.Admin\.Api\.Endpoints;.*?using Migration\.ControlPlane\.Telemetry;\s*\r?\n\r?\n', ''

$usings = @"
using Migration.Admin.Api.Endpoints;
using Migration.ControlPlane.Auth;
using Migration.ControlPlane.Audit;
using Migration.ControlPlane.Credentials;
using Migration.ControlPlane.Operations;
using Migration.ControlPlane.Queues;
using Migration.ControlPlane.Storage;
using Migration.ControlPlane.Telemetry;

"@

if ($content -notmatch '^using Migration\.Admin\.Api\.Endpoints;') {
    $content = $usings + $content
}

[System.IO.File]::WriteAllText($startupPath, $content, [System.Text.UTF8Encoding]::new($false))

Write-Host "Patched src\Migration.Admin.Api\Registration\AdminApiCloudStartupExtensions.cs usings."
Write-Host ""
Write-Host "Post-P2 Cleanup Set 011 fix applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
