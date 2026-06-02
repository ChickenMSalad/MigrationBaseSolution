Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $scriptRoot))
$adminWebRoot = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web')
$docPath = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10', 'P10.2CI-AdminWebDeploymentContractVerification.md')
$applySummaryPath = [System.IO.Path]::Combine($repoRoot, 'artifacts', 'p10', 'P10.2CI', 'apply-summary.md')
$runPath = [System.IO.Path]::Combine($scriptRoot, 'Run-P10.2CI-AdminWebDeploymentContractCheck.ps1')

$requiredFiles = @(
    [pscustomobject]@{ Label = 'package.json'; Path = [System.IO.Path]::Combine($adminWebRoot, 'package.json') },
    [pscustomobject]@{ Label = 'vite.config.ts'; Path = [System.IO.Path]::Combine($adminWebRoot, 'vite.config.ts') },
    [pscustomobject]@{ Label = 'tsconfig.json'; Path = [System.IO.Path]::Combine($adminWebRoot, 'tsconfig.json') },
    [pscustomobject]@{ Label = 'index.html'; Path = [System.IO.Path]::Combine($adminWebRoot, 'index.html') },
    [pscustomobject]@{ Label = 'documentation'; Path = $docPath },
    [pscustomobject]@{ Label = 'runner'; Path = $runPath },
    [pscustomobject]@{ Label = 'apply summary'; Path = $applySummaryPath }
)

foreach ($item in $requiredFiles) {
    if (-not (Test-Path -Path $item.Path -PathType Leaf)) {
        throw ('Required {0} was not found: {1}' -f $item.Label, $item.Path)
    }
}

$packageText = [System.IO.File]::ReadAllText([System.IO.Path]::Combine($adminWebRoot, 'package.json'))
if ($packageText -notmatch '"build"') {
    throw 'Admin Web package.json does not contain a build script.'
}
if ($packageText -notmatch '"preview"') {
    throw 'Admin Web package.json does not contain a preview script.'
}

$viteText = [System.IO.File]::ReadAllText([System.IO.Path]::Combine($adminWebRoot, 'vite.config.ts'))
if ($viteText -notmatch 'VITE_ADMIN_API_PROXY_TARGET') {
    throw 'Vite config does not reference VITE_ADMIN_API_PROXY_TARGET.'
}
if ($viteText -notmatch 'proxy') {
    throw 'Vite config does not include proxy configuration.'
}

$runnerText = [System.IO.File]::ReadAllText($runPath)
if ($runnerText -notmatch 'npm') {
    throw 'Deployment contract runner does not reference npm.'
}
if ($runnerText -notmatch 'dist') {
    throw 'Deployment contract runner does not reference dist output.'
}

Write-Host 'P10.2CI Admin Web deployment contract verification passed.'
