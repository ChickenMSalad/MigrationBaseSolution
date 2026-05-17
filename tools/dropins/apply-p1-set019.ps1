$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p1-set019-frontend-auth-bootstrap-plan"

Write-Host "Applying P1 Set 019 from $repoRoot"

$files = @(
    "src\Admin\Migration.Admin.Web\src\auth\frontendAuthConfig.ts",
    "src\Admin\Migration.Admin.Web\src\auth\index.ts",
    "src\Admin\Migration.Admin.Web\src\api\authReadiness.ts",
    "src\Admin\Migration.Admin.Web\.env.auth.example",
    "docs\azure\FRONTEND_AUTH_BOOTSTRAP.md",
    "docs\cloud-roadmap-cleanup\P1_SET_019_FRONTEND_AUTH_BOOTSTRAP.md"
)

foreach ($relative in $files) {
    $source = Join-Path $payloadRoot $relative
    $target = Join-Path $repoRoot $relative
    $targetDirectory = Split-Path $target -Parent

    if (!(Test-Path $source)) {
        throw "Drop-in package is missing expected file: $source"
    }

    if (!(Test-Path $targetDirectory)) {
        New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
    }

    Copy-Item $source $target -Force
    Write-Host "Verified $relative"
}

Write-Host ""
Write-Host "P1 Set 019 applied."
Write-Host "Run:"
Write-Host "  cd .\src\Admin\Migration.Admin.Web"
Write-Host "  npm run build"
