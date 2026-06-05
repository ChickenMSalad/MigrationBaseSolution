Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$srcRoot = Join-Path $adminWebRoot 'src'
$envExamplePath = Join-Path $adminWebRoot '.env.local.example'
$viteConfigPath = Join-Path $adminWebRoot 'vite.config.ts'
$packageJsonPath = Join-Path $adminWebRoot 'package.json'
$adminApiClientPath = Join-Path $srcRoot 'api\core\adminApiClient.ts'
$docPath = Join-Path $repoRoot 'docs\P10\P10.2CB-AdminWebRuntimeConfigReadiness.md'

$requiredLeaves = @(
    [pscustomobject]@{ Label = 'Admin Web package.json'; Path = $packageJsonPath },
    [pscustomobject]@{ Label = 'Admin Web Vite config'; Path = $viteConfigPath },
    [pscustomobject]@{ Label = 'Admin API client'; Path = $adminApiClientPath },
    [pscustomobject]@{ Label = 'Environment example'; Path = $envExamplePath },
    [pscustomobject]@{ Label = 'P10.2CB documentation'; Path = $docPath }
)

foreach ($item in $requiredLeaves) {
    if (-not (Test-Path -Path $item.Path -PathType Leaf)) {
        throw ('Missing {0}: {1}' -f $item.Label, $item.Path)
    }
}

$envExample = [System.IO.File]::ReadAllText($envExamplePath)
if ($envExample.IndexOf('VITE_ADMIN_API_BASE_URL=', [System.StringComparison]::Ordinal) -lt 0) {
    throw '.env.local.example is missing VITE_ADMIN_API_BASE_URL.'
}
if ($envExample.IndexOf('VITE_ADMIN_API_PROXY_TARGET=', [System.StringComparison]::Ordinal) -lt 0) {
    throw '.env.local.example is missing VITE_ADMIN_API_PROXY_TARGET.'
}

$viteConfig = [System.IO.File]::ReadAllText($viteConfigPath)
if ($viteConfig.IndexOf('VITE_ADMIN_API_PROXY_TARGET', [System.StringComparison]::Ordinal) -lt 0) {
    throw 'vite.config.ts does not reference VITE_ADMIN_API_PROXY_TARGET.'
}
if ($viteConfig.IndexOf('"/api"', [System.StringComparison]::Ordinal) -lt 0 -and $viteConfig.IndexOf("'/api'", [System.StringComparison]::Ordinal) -lt 0) {
    throw 'vite.config.ts does not configure an /api proxy.'
}

$adminApiClient = [System.IO.File]::ReadAllText($adminApiClientPath)
if ($adminApiClient.IndexOf('VITE_ADMIN_API_BASE_URL', [System.StringComparison]::Ordinal) -lt 0) {
    throw 'adminApiClient.ts does not reference VITE_ADMIN_API_BASE_URL.'
}

$packageJson = [System.IO.File]::ReadAllText($packageJsonPath)
if ($packageJson.IndexOf('"build"', [System.StringComparison]::Ordinal) -lt 0 -or $packageJson.IndexOf('vite build', [System.StringComparison]::Ordinal) -lt 0) {
    throw 'package.json does not expose the expected Vite build script.'
}

$compiledSourceFiles = @(Get-ChildItem -Path $srcRoot -Recurse -File -Include *.ts,*.tsx)
foreach ($file in $compiledSourceFiles) {
    $content = [System.IO.File]::ReadAllText($file.FullName)
    if ($content.IndexOf('.tsx''', [System.StringComparison]::Ordinal) -ge 0 -or $content.IndexOf('.tsx"', [System.StringComparison]::Ordinal) -ge 0) {
        throw ('Compiled source contains extension-bearing TSX import: {0}' -f $file.FullName)
    }
    if ($content.IndexOf('reference/apps-migration-admin-ui', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw ('Compiled source imports reference material: {0}' -f $file.FullName)
    }
}

Write-Host 'P10.2CB Admin Web runtime config readiness validation passed.'
