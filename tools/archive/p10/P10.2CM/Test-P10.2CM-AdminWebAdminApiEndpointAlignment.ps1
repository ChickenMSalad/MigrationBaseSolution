Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')

$runnerPath = Join-Path $repoRoot 'tools\p10\P10.2CM\Run-P10.2CM-AdminWebAdminApiEndpointAlignment.ps1'
$docPath = Join-Path $repoRoot 'docs\P10\P10.2CM-AdminWebAdminApiEndpointAlignment.md'
$artifactPath = Join-Path $repoRoot 'artifacts\p10\P10.2CM\admin-web-admin-api-endpoint-alignment.md'
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$adminWebSourceRoot = Join-Path $adminWebRoot 'src'

if (-not (Test-Path -LiteralPath $runnerPath -PathType Leaf)) {
    throw ('Missing runner: {0}' -f $runnerPath)
}

if (-not (Test-Path -LiteralPath $docPath -PathType Leaf)) {
    throw ('Missing documentation report: {0}' -f $docPath)
}

if (-not (Test-Path -LiteralPath $artifactPath -PathType Leaf)) {
    throw ('Missing artifact report: {0}' -f $artifactPath)
}

if (-not (Test-Path -LiteralPath $adminWebSourceRoot -PathType Container)) {
    throw ('Missing Admin Web source root: {0}' -f $adminWebSourceRoot)
}

$docText = Get-Content -LiteralPath $docPath -Raw
if ($docText -notlike '*Admin Web endpoint-like references*') {
    throw 'Endpoint alignment report is missing the Admin Web endpoint section.'
}

if ($docText -notlike '*Admin API/server route-like declarations*') {
    throw 'Endpoint alignment report is missing the Admin API/server route section.'
}

if ($docText -notlike '*Alignment notes*') {
    throw 'Endpoint alignment report is missing alignment notes.'
}

$sourceFiles = @(Get-ChildItem -LiteralPath $adminWebSourceRoot -Recurse -File -Include '*.ts','*.tsx' | Where-Object {
    $_.FullName -notmatch '\\node_modules\\' -and
    $_.FullName -notmatch '\\dist\\' -and
    $_.FullName -notmatch '\\reference\\'
})

foreach ($file in $sourceFiles) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    if ($content -match 'from\s+[''"].*\.tsx[''"]') {
        throw ('Extension-bearing TSX import found in compiled Admin Web source: {0}' -f $file.FullName)
    }
    if ($content -match 'from\s+[''"].*reference/') {
        throw ('Compiled Admin Web source imports reference material: {0}' -f $file.FullName)
    }
}

Write-Host 'P10.2CM Admin Web Admin API endpoint alignment test passed.'
