Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Resolve-RepoRoot {
    $scriptRoot = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
        $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    }

    $candidate = Resolve-Path -LiteralPath (Join-Path $scriptRoot '..\..\..')
    return $candidate.ProviderPath
}

$repoRoot = Resolve-RepoRoot
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$docPath = Join-Path $repoRoot 'docs\P10\P10.2CH-AdminWebPreviewSmokeHarness.md'
$runnerPath = Join-Path $repoRoot 'tools\p10\P10.2CH\Run-P10.2CH-AdminWebPreviewSmoke.ps1'

$requiredFiles = @(
    [pscustomobject]@{ Label = 'Admin Web package'; Path = (Join-Path $adminWebRoot 'package.json') },
    [pscustomobject]@{ Label = 'Preview smoke runner'; Path = $runnerPath },
    [pscustomobject]@{ Label = 'P10.2CH report'; Path = $docPath }
)

foreach ($item in $requiredFiles) {
    if (-not (Test-Path -LiteralPath $item.Path -PathType Leaf)) {
        throw ('Missing {0}: {1}' -f $item.Label, $item.Path)
    }
}

$reportText = Get-Content -LiteralPath $docPath -Raw
if ($reportText -notmatch 'Admin Web Preview Smoke Harness') {
    throw ('P10.2CH report does not contain the expected title: {0}' -f $docPath)
}

$runnerText = Get-Content -LiteralPath $runnerPath -Raw
if ($runnerText -notmatch 'npm run build') {
    throw ('Runner does not contain the expected build invocation: {0}' -f $runnerPath)
}
if ($runnerText -notmatch 'npm run preview') {
    throw ('Runner does not contain the expected preview invocation: {0}' -f $runnerPath)
}

Write-Host 'P10.2CH Admin Web preview smoke harness validation passed.'
