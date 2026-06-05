Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path -Path (Join-Path -Path $PSScriptRoot -ChildPath '..\..\..')).Path
$adminWebRoot = Join-Path -Path $repoRoot -ChildPath 'src\Admin\Migration.Admin.Web'
$packageJson = Join-Path -Path $adminWebRoot -ChildPath 'package.json'
$viteConfig = Join-Path -Path $adminWebRoot -ChildPath 'vite.config.ts'
$envExample = Join-Path -Path $adminWebRoot -ChildPath '.env.local.example'
$reportPath = Join-Path -Path $repoRoot -ChildPath 'docs\P10\P10.2CC-AdminWebLocalSiteSmokeHarness.Report.md'
$runScript = Join-Path -Path $PSScriptRoot -ChildPath 'Run-P10.2CC-AdminWebDevSmoke.ps1'

$requiredFiles = @(
    [pscustomobject]@{ Path = $packageJson; Label = 'Admin Web package.json' },
    [pscustomobject]@{ Path = $viteConfig; Label = 'Admin Web vite.config.ts' },
    [pscustomobject]@{ Path = $envExample; Label = 'Admin Web .env.local.example' },
    [pscustomobject]@{ Path = $reportPath; Label = 'P10.2CC report' },
    [pscustomobject]@{ Path = $runScript; Label = 'P10.2CC smoke runner' }
)

foreach ($item in $requiredFiles) {
    if (-not (Test-Path -Path $item.Path -PathType Leaf)) {
        throw ('Missing {0}: {1}' -f $item.Label, $item.Path)
    }
}

$packageText = Get-Content -Path $packageJson -Raw
$viteText = Get-Content -Path $viteConfig -Raw
$reportText = Get-Content -Path $reportPath -Raw

if ($packageText -notmatch '"dev"\s*:\s*"vite"') {
    throw 'package.json dev script does not match expected Vite command.'
}

if ($packageText -notmatch '"build"\s*:\s*"tsc -b && vite build"') {
    throw 'package.json build script does not match expected TypeScript plus Vite command.'
}

if ($viteText -notmatch 'VITE_ADMIN_API_PROXY_TARGET') {
    throw 'vite.config.ts is missing VITE_ADMIN_API_PROXY_TARGET.'
}

if ($viteText -notmatch 'proxy') {
    throw 'vite.config.ts is missing proxy configuration.'
}

if ($reportText -notmatch 'Admin Web Local Site Smoke Harness Report') {
    throw 'P10.2CC report does not contain expected title.'
}

Write-Host 'P10.2CC Admin Web local site smoke harness validation passed.'
