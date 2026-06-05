Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$repoRoot = $repoRoot.Path

$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
if (-not (Test-Path -LiteralPath $adminWebRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}

$pagePaths = New-Object 'System.Collections.Generic.List[string]'
[void]$pagePaths.Add((Join-Path $adminWebRoot 'src\features\governance\mappingBuilder\pages\MappingBuilder.tsx'))
[void]$pagePaths.Add((Join-Path $adminWebRoot 'src\features\governance\taxonomyBuilder\pages\TaxonomyBuilder.tsx'))

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.3K Repair2 - Admin Web Cloud Publish Build Repair')
[void]$report.Add('')
[void]$report.Add('Fixes restored builder page `LoadingError` prop usage for production publish builds.')
[void]$report.Add('')

foreach ($pagePath in $pagePaths) {
    if (-not (Test-Path -LiteralPath $pagePath -PathType Leaf)) {
        throw ('Expected builder page was not found: {0}' -f $pagePath)
    }

    $content = Get-Content -LiteralPath $pagePath -Raw
    $updated = $content.Replace('<LoadingError description={message} />', '<LoadingError message={message} />')

    if ($updated -ne $content) {
        Set-Content -LiteralPath $pagePath -Value $updated -Encoding UTF8
        [void]$report.Add(('- Updated LoadingError prop usage: {0}' -f $pagePath))
    }
    else {
        if ($content.Contains('<LoadingError message={message} />')) {
            [void]$report.Add(('- LoadingError prop usage already correct: {0}' -f $pagePath))
        }
        else {
            throw ('Expected LoadingError message/description pattern was not found in {0}' -f $pagePath)
        }
    }
}

$docsRoot = Join-Path $repoRoot 'docs\P10'
if (-not (Test-Path -LiteralPath $docsRoot -PathType Container)) {
    New-Item -ItemType Directory -Path $docsRoot | Out-Null
}

$reportPath = Join-Path $docsRoot 'P10.3K-Repair2-AdminWebCloudPublishBuildRepair.md'
Set-Content -LiteralPath $reportPath -Value $report.ToArray() -Encoding UTF8

Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.3K Repair2 Admin Web cloud publish build repair applied.'
