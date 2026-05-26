[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $ResourceGroup,

    [Parameter(Mandatory = $true)]
    [string] $DispatcherApp,

    [Parameter(Mandatory = $true)]
    [string] $ExecutorApp,

    [Parameter(Mandatory = $true)]
    [string] $SqlServer,

    [Parameter(Mandatory = $true)]
    [string] $Database,

    [Parameter(Mandatory = $true)]
    [string] $SqlAdmin,

    [Parameter(Mandatory = $true)]
    [string] $SqlPasswordPlain,

    [Parameter(Mandatory = $true)]
    [Guid] $RunId,

    [Parameter(Mandatory = $true)]
    [string] $ServiceBusNamespace,

    [Parameter(Mandatory = $true)]
    [string] $ServiceBusQueue,

    [Parameter(Mandatory = $false)]
    [string] $PayloadPath
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $current = Get-Location
    while ($null -ne $current) {
        $candidate = Join-Path $current.Path 'MigrationBaseSolution.sln'
        if (Test-Path -LiteralPath $candidate) {
            return $current.Path
        }

        $parent = Split-Path -Parent $current.Path
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $current.Path) {
            break
        }

        $current = Get-Item -LiteralPath $parent
    }

    throw 'Could not locate repo root. Run this script from inside MigrationBaseSolutionRepo.'
}

function Invoke-CheckedProcess {
    param(
        [Parameter(Mandatory = $true)][string] $FilePath,
        [Parameter(Mandatory = $true)][string[]] $Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code $($LASTEXITCODE): $FilePath"
    }
}

$repoRoot = Get-RepoRoot
$publishScript = Join-Path $repoRoot 'tools\runtime\Publish-RuntimeWorker.ps1'
$enqueueScript = Join-Path $repoRoot 'tools\runtime\Invoke-RuntimeSmokeEnqueue.ps1'
$stateScript = Join-Path $repoRoot 'tools\runtime\Test-RuntimeSmokeState.ps1'

foreach ($requiredPath in @($publishScript, $enqueueScript, $stateScript)) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Required script was not found: $requiredPath"
    }
}

Write-Host 'Publishing dispatcher...'
& $publishScript -Role Dispatcher

Write-Host 'Publishing executor...'
& $publishScript -Role Executor

$dispatcherZip = Join-Path $repoRoot 'artifacts\publish\sb-dispatcher.zip'
$executorZip = Join-Path $repoRoot 'artifacts\publish\sb-executor.zip'

if ([string]::IsNullOrWhiteSpace($dispatcherZip) -or -not (Test-Path -LiteralPath $dispatcherZip)) {
    throw 'Dispatcher ZIP path was not produced.'
}
if ([string]::IsNullOrWhiteSpace($executorZip) -or -not (Test-Path -LiteralPath $executorZip)) {
    throw 'Executor ZIP path was not produced.'
}

Write-Host 'Deploying dispatcher...'
Invoke-CheckedProcess -FilePath 'az' -Arguments @(
    'webapp', 'deploy',
    '--resource-group', $ResourceGroup,
    '--name', $DispatcherApp,
    '--src-path', $dispatcherZip,
    '--type', 'zip',
    '--clean', 'true',
    '--restart', 'true'
)

Write-Host 'Deploying executor...'
Invoke-CheckedProcess -FilePath 'az' -Arguments @(
    'webapp', 'deploy',
    '--resource-group', $ResourceGroup,
    '--name', $ExecutorApp,
    '--src-path', $executorZip,
    '--type', 'zip',
    '--clean', 'true',
    '--restart', 'true'
)

Write-Host 'Restarting executor before enqueue...'
Invoke-CheckedProcess -FilePath 'az' -Arguments @('webapp', 'restart', '--resource-group', $ResourceGroup, '--name', $ExecutorApp)

$enqueueArgs = @(
    '-SqlServer', $SqlServer,
    '-Database', $Database,
    '-SqlAdmin', $SqlAdmin,
    '-SqlPasswordPlain', $SqlPasswordPlain,
    '-RunId', $RunId.ToString()
)
if (-not [string]::IsNullOrWhiteSpace($PayloadPath)) {
    $enqueueArgs += @('-PayloadPath', $PayloadPath)
}

Write-Host 'Enqueueing smoke work item...'
& $enqueueScript @enqueueArgs


Write-Host 'Restarting dispatcher to dispatch smoke item...'
Invoke-CheckedProcess -FilePath 'az' -Arguments @('webapp', 'restart', '--resource-group', $ResourceGroup, '--name', $DispatcherApp)

Write-Host 'Smoke state after dispatch:'
& $stateScript `
    -SqlServer $SqlServer `
    -Database $Database `
    -SqlAdmin $SqlAdmin `
    -SqlPasswordPlain $SqlPasswordPlain `
    -ResourceGroup $ResourceGroup `
    -ServiceBusNamespace $ServiceBusNamespace `
    -ServiceBusQueue $ServiceBusQueue


Write-Host 'Deployment smoke command completed. Review executor logs and SQL final status before handoff.'
