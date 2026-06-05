Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$sourceRoot = Join-Path $adminWebRoot 'src'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.3K-Repair-AdminWebCloudPublishBuildRepair.md'

if (-not (Test-Path -LiteralPath $adminWebRoot)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}

$targetFiles = @(
    'features\governance\mappingBuilder\pages\MappingBuilder.tsx',
    'features\governance\taxonomyBuilder\pages\TaxonomyBuilder.tsx'
)

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.3K Repair - Admin Web Cloud Publish Build Repair')
[void]$report.Add('')
[void]$report.Add('Purpose: repair builder page Card prop usage so the production cloud publish package can build.')
[void]$report.Add('')

foreach ($relativePath in $targetFiles) {
    $filePath = Join-Path $sourceRoot $relativePath
    if (-not (Test-Path -LiteralPath $filePath)) {
        throw ('Required builder page was not found: {0}' -f $filePath)
    }

    $content = Get-Content -LiteralPath $filePath -Raw
    $updated = $content.Replace(' message={message}', ' description={message}')

    if ($updated -ne $content) {
        Set-Content -LiteralPath $filePath -Value $updated -Encoding UTF8
        Write-Host ('Updated Card message prop in {0}' -f $filePath)
        [void]$report.Add(('- Updated `{0}`: replaced `message={{message}}` with `description={{message}}`.' -f $relativePath.Replace('\','/')))
    }
    else {
        Write-Host ('No Card message prop update needed in {0}' -f $filePath)
        [void]$report.Add(('- No update needed for `{0}`.' -f $relativePath.Replace('\','/')))
    }
}

[void]$report.Add('')
[void]$report.Add('No cloud resource names, credentials, or deployment targets were changed.')
Set-Content -LiteralPath $reportPath -Value $report.ToArray() -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.3K Repair Admin Web cloud publish build repair applied.'
