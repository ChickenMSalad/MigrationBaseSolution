Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $scriptRoot))
$adminWebRoot = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web')
$docPath = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10', 'P10.2CI-AdminWebDeploymentContractVerification.md')
$artifactRoot = [System.IO.Path]::Combine($repoRoot, 'artifacts', 'p10', 'P10.2CI')
$runPath = [System.IO.Path]::Combine($scriptRoot, 'Run-P10.2CI-AdminWebDeploymentContractCheck.ps1')

if (-not (Test-Path -Path $adminWebRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}

if (-not (Test-Path -Path $docPath -PathType Leaf)) {
    throw ('Documentation file was not found: {0}' -f $docPath)
}

if (-not (Test-Path -Path $runPath -PathType Leaf)) {
    throw ('Deployment contract runner was not found: {0}' -f $runPath)
}

if (-not (Test-Path -Path $artifactRoot -PathType Container)) {
    New-Item -Path $artifactRoot -ItemType Directory -Force | Out-Null
}

$summaryPath = [System.IO.Path]::Combine($artifactRoot, 'apply-summary.md')
$lines = New-Object 'System.Collections.Generic.List[string]'
[void]$lines.Add('# P10.2CI - Admin Web Deployment Contract Verification')
[void]$lines.Add('')
[void]$lines.Add(('Repository root: `{0}`' -f $repoRoot))
[void]$lines.Add(('Admin Web root: `{0}`' -f $adminWebRoot))
[void]$lines.Add(('Documentation: `{0}`' -f $docPath))
[void]$lines.Add(('Runner: `{0}`' -f $runPath))
[void]$lines.Add('')
[void]$lines.Add('Apply completed. Run the deployment contract check after production build output exists.')
[System.IO.File]::WriteAllLines($summaryPath, $lines, [System.Text.UTF8Encoding]::new($false))

Write-Host ('Wrote apply summary: {0}' -f $summaryPath)
Write-Host 'P10.2CI Admin Web deployment contract verification applied.'
