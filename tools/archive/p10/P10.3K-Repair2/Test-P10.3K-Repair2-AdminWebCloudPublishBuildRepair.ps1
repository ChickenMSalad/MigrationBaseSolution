Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$repoRoot = $repoRoot.Path

$scriptPaths = New-Object 'System.Collections.Generic.List[string]'
[void]$scriptPaths.Add((Join-Path $scriptRoot 'Apply-P10.3K-Repair2-AdminWebCloudPublishBuildRepair.ps1'))
[void]$scriptPaths.Add((Join-Path $scriptRoot 'Test-P10.3K-Repair2-AdminWebCloudPublishBuildRepair.ps1'))

foreach ($scriptPath in $scriptPaths) {
    if (-not (Test-Path -LiteralPath $scriptPath -PathType Leaf)) {
        throw ('Expected script was not found: {0}' -f $scriptPath)
    }

    $content = Get-Content -LiteralPath $scriptPath -Raw
    [void][scriptblock]::Create($content)
}

$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
if (-not (Test-Path -LiteralPath $adminWebRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}

$pagePaths = New-Object 'System.Collections.Generic.List[string]'
[void]$pagePaths.Add((Join-Path $adminWebRoot 'src\features\governance\mappingBuilder\pages\MappingBuilder.tsx'))
[void]$pagePaths.Add((Join-Path $adminWebRoot 'src\features\governance\taxonomyBuilder\pages\TaxonomyBuilder.tsx'))

foreach ($pagePath in $pagePaths) {
    if (-not (Test-Path -LiteralPath $pagePath -PathType Leaf)) {
        throw ('Expected builder page was not found: {0}' -f $pagePath)
    }

    $content = Get-Content -LiteralPath $pagePath -Raw
    if ($content.Contains('<LoadingError description={message} />')) {
        throw ('Builder page still uses unsupported LoadingError description prop: {0}' -f $pagePath)
    }

    if (-not $content.Contains('<LoadingError message={message} />')) {
        throw ('Builder page does not contain expected LoadingError message prop: {0}' -f $pagePath)
    }
}

$reportPath = Join-Path $repoRoot 'docs\P10\P10.3K-Repair2-AdminWebCloudPublishBuildRepair.md'
if (-not (Test-Path -LiteralPath $reportPath -PathType Leaf)) {
    throw ('Expected report was not found: {0}' -f $reportPath)
}

Write-Host 'P10.3K Repair2 Admin Web cloud publish build repair validation passed.'
