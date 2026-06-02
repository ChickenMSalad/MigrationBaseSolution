Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$workflowPath = Join-Path $repoRoot '.github\workflows\admin-web-build-validation.yml'
$docPath = Join-Path $repoRoot 'docs\P10\P10.2CJ-AdminWebCiBuildValidationWorkflow.md'
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'

if (-not (Test-Path -Path $workflowPath -PathType Leaf)) {
    throw ('Missing workflow: {0}' -f $workflowPath)
}
if (-not (Test-Path -Path $docPath -PathType Leaf)) {
    throw ('Missing documentation: {0}' -f $docPath)
}
if (-not (Test-Path -Path $adminWebRoot -PathType Container)) {
    throw ('Missing Admin Web root: {0}' -f $adminWebRoot)
}

$workflow = Get-Content -Path $workflowPath -Raw
$required = @(
    'Admin Web Build Validation',
    'src/Admin/Migration.Admin.Web/**',
    'working-directory: src/Admin/Migration.Admin.Web',
    'actions/setup-node@v4',
    "node-version: '22'",
    'cache-dependency-path: src/Admin/Migration.Admin.Web/package-lock.json',
    'npm ci',
    'npm run build'
)
foreach ($text in $required) {
    if ($workflow.IndexOf($text, [StringComparison]::Ordinal) -lt 0) {
        throw ('Workflow missing expected text: {0}' -f $text)
    }
}

$packageJson = Join-Path $adminWebRoot 'package.json'
$packageLock = Join-Path $adminWebRoot 'package-lock.json'
if (-not (Test-Path -Path $packageJson -PathType Leaf)) {
    throw ('Missing Admin Web package.json: {0}' -f $packageJson)
}
if (-not (Test-Path -Path $packageLock -PathType Leaf)) {
    throw ('Missing Admin Web package-lock.json: {0}' -f $packageLock)
}

Write-Host 'P10.2CJ Admin Web CI build validation workflow verified.'
