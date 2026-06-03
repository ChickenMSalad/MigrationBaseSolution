param(
    [string]$Configuration = 'production',
    [string]$ArtifactName = 'admin-web-static-site.zip'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.3K'
$summaryPath = Join-Path $artifactRoot 'admin-web-cloud-publish-package.summary.md'
$distRoot = Join-Path $adminWebRoot 'dist'
$zipPath = Join-Path $artifactRoot $ArtifactName

if (-not (Test-Path -LiteralPath $adminWebRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}
if (-not (Test-Path -LiteralPath (Join-Path $adminWebRoot 'package.json') -PathType Leaf)) {
    throw 'Admin Web package.json is missing.'
}
if (-not (Test-Path -LiteralPath $artifactRoot -PathType Container)) {
    New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
}

$npm = Get-Command npm.cmd -ErrorAction SilentlyContinue
if ($null -eq $npm) {
    $npm = Get-Command npm -ErrorAction SilentlyContinue
}
if ($null -eq $npm) {
    throw 'npm was not found on PATH.'
}

Push-Location $adminWebRoot
try {
    Write-Host 'Restoring Admin Web npm dependencies...'
    & $npm.Source install
    if ($LASTEXITCODE -ne 0) {
        throw ('npm install failed with exit code {0}.' -f $LASTEXITCODE)
    }

    Write-Host 'Building Admin Web production bundle...'
    & $npm.Source run build
    if ($LASTEXITCODE -ne 0) {
        throw ('npm run build failed with exit code {0}.' -f $LASTEXITCODE)
    }
}
finally {
    Pop-Location
}

if (-not (Test-Path -LiteralPath $distRoot -PathType Container)) {
    throw ('Expected dist folder was not found: {0}' -f $distRoot)
}
if (-not (Test-Path -LiteralPath (Join-Path $distRoot 'index.html') -PathType Leaf)) {
    throw 'dist/index.html was not found after build.'
}

if (Test-Path -LiteralPath $zipPath -PathType Leaf) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -Path (Join-Path $distRoot '*') -DestinationPath $zipPath -Force
if (-not (Test-Path -LiteralPath $zipPath -PathType Leaf)) {
    throw ('Expected publish artifact was not created: {0}' -f $zipPath)
}

$assetCount = 0
$assetRoot = Join-Path $distRoot 'assets'
if (Test-Path -LiteralPath $assetRoot -PathType Container) {
    $assetCount = @(Get-ChildItem -LiteralPath $assetRoot -File -Recurse).Count
}

$summary = New-Object 'System.Collections.Generic.List[string]'
[void]$summary.Add('# P10.3K - Admin Web Cloud Publish Package')
[void]$summary.Add('')
[void]$summary.Add(('Generated UTC: `{0}`' -f ([DateTime]::UtcNow.ToString('o'))))
[void]$summary.Add(('Configuration: `{0}`' -f $Configuration))
[void]$summary.Add(('Admin Web root: `{0}`' -f $adminWebRoot))
[void]$summary.Add(('Dist root: `{0}`' -f $distRoot))
[void]$summary.Add(('Publish artifact: `{0}`' -f $zipPath))
[void]$summary.Add(('Asset count: `{0}`' -f $assetCount))
[void]$summary.Add('')
[void]$summary.Add('## Publish Guidance')
[void]$summary.Add('')
[void]$summary.Add('- Upload the contents of the ZIP to the selected static web host.')
[void]$summary.Add('- Configure the production Admin API URL for the selected environment.')
[void]$summary.Add('- Verify SPA fallback/routing on the host after publish.')
[void]$summary.Add('- Run the P10.3A through P10.3E acceptance checks against the cloud URL after publish.')

Set-Content -LiteralPath $summaryPath -Value $summary.ToArray() -Encoding UTF8
Write-Host ('Wrote publish artifact: {0}' -f $zipPath)
Write-Host ('Wrote summary: {0}' -f $summaryPath)
