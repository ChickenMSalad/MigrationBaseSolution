Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$repoRootPath = $repoRoot.Path

$requiredFiles = @(
    'docs\P10\P10.2CD-AdminWebAdminApiConnectivitySmoke.md',
    'tools\p10\P10.2CD\Apply-P10.2CD-AdminWebAdminApiConnectivitySmoke.ps1',
    'tools\p10\P10.2CD\Test-P10.2CD-AdminWebAdminApiConnectivitySmoke.ps1',
    'tools\p10\P10.2CD\Run-P10.2CD-AdminApiConnectivitySmoke.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $path = Join-Path $repoRootPath $relativePath
    if (-not (Test-Path -Path $path -PathType Leaf)) {
        throw ('Required file was not found: {0}' -f $path)
    }
}

$adminWebRoot = Join-Path $repoRootPath 'src\Admin\Migration.Admin.Web'
$adminApiRoot = Join-Path $repoRootPath 'src\Core\Migration.Admin.Api'
$viteConfig = Join-Path $adminWebRoot 'vite.config.ts'
$packageJson = Join-Path $adminWebRoot 'package.json'
$adminApiProgram = Join-Path $adminApiRoot 'Program.cs'

if (-not (Test-Path -Path $viteConfig -PathType Leaf)) {
    throw ('vite.config.ts was not found: {0}' -f $viteConfig)
}

if (-not (Test-Path -Path $packageJson -PathType Leaf)) {
    throw ('package.json was not found: {0}' -f $packageJson)
}

if (-not (Test-Path -Path $adminApiProgram -PathType Leaf)) {
    throw ('Admin API Program.cs was not found: {0}' -f $adminApiProgram)
}

$viteText = Get-Content -Path $viteConfig -Raw
if ($viteText -notlike '*VITE_ADMIN_API_PROXY_TARGET*') {
    throw 'vite.config.ts does not reference VITE_ADMIN_API_PROXY_TARGET.'
}
if ($viteText -notlike '*/api*') {
    throw 'vite.config.ts does not include an /api proxy posture.'
}

$packageText = Get-Content -Path $packageJson -Raw
if ($packageText -notlike '*"dev"*') {
    throw 'package.json does not include a dev script.'
}
if ($packageText -notlike '*"build"*') {
    throw 'package.json does not include a build script.'
}

Write-Host 'P10.2CD Admin Web Admin API connectivity smoke validation passed.'
