Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $scriptRoot = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
        $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    }

    $current = Resolve-Path -LiteralPath $scriptRoot
    while ($null -ne $current) {
        $candidate = Join-Path -Path $current.Path -ChildPath 'src'
        $adminCandidate = Join-Path -Path $candidate -ChildPath 'Admin'
        if (Test-Path -LiteralPath $adminCandidate -PathType Container) {
            return $current.Path
        }

        $parent = Split-Path -Parent $current.Path
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $current.Path) {
            break
        }

        $current = Resolve-Path -LiteralPath $parent
    }

    throw 'Unable to locate repository root. Run this script from inside the MigrationBaseSolution repository.'
}

function Join-RepoPath {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string[]]$Segments
    )

    $result = $Root
    foreach ($segment in $Segments) {
        $result = Join-Path -Path $result -ChildPath $segment
    }

    return $result
}

function Add-Failure {
    param(
        [Parameter(Mandatory = $true)][System.Collections.Generic.List[string]]$Failures,
        [Parameter(Mandatory = $true)][string]$Message
    )

    $Failures.Add($Message) | Out-Null
}

function Read-RequiredFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][System.Collections.Generic.List[string]]$Failures
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        Add-Failure -Failures $Failures -Message "Missing required file: $Path"
        return ''
    }

    return Get-Content -LiteralPath $Path -Raw
}

$repoRoot = Get-RepoRoot
$adminSrc = Join-RepoPath -Root $repoRoot -Segments @('src','Admin','Migration.Admin.Web','src')
$featureRoot = Join-RepoPath -Root $adminSrc -Segments @('features','operations','failureRetry')

$checks = @(
    [pscustomobject]@{ Path = (Join-RepoPath -Root $featureRoot -Segments @('pages','FailureRetry.tsx')); ShouldExist = $true },
    [pscustomobject]@{ Path = (Join-RepoPath -Root $featureRoot -Segments @('api','failureRetry.ts')); ShouldExist = $true },
    [pscustomobject]@{ Path = (Join-RepoPath -Root $featureRoot -Segments @('types','failureRetry.ts')); ShouldExist = $true },
    [pscustomobject]@{ Path = (Join-RepoPath -Root $adminSrc -Segments @('pages','FailureRetry.tsx')); ShouldExist = $false },
    [pscustomobject]@{ Path = (Join-RepoPath -Root $adminSrc -Segments @('api','failureRetry.ts')); ShouldExist = $false },
    [pscustomobject]@{ Path = (Join-RepoPath -Root $adminSrc -Segments @('types','failureRetry.ts')); ShouldExist = $false }
)

$failures = New-Object 'System.Collections.Generic.List[string]'

foreach ($check in $checks) {
    $exists = Test-Path -LiteralPath $check.Path -PathType Leaf
    if ($check.ShouldExist -and -not $exists) {
        Add-Failure -Failures $failures -Message "Expected file does not exist: $($check.Path)"
    }

    if (-not $check.ShouldExist -and $exists) {
        Add-Failure -Failures $failures -Message "Old flat file still exists: $($check.Path)"
    }
}

$appPath = Join-RepoPath -Root $adminSrc -Segments @('App.tsx')
$pagePath = Join-RepoPath -Root $featureRoot -Segments @('pages','FailureRetry.tsx')
$apiPath = Join-RepoPath -Root $featureRoot -Segments @('api','failureRetry.ts')

$appText = Read-RequiredFile -Path $appPath -Failures $failures
$pageText = Read-RequiredFile -Path $pagePath -Failures $failures
$apiText = Read-RequiredFile -Path $apiPath -Failures $failures

if ($appText -notmatch './features/operations/failureRetry/pages/FailureRetry') {
    Add-Failure -Failures $failures -Message 'App.tsx does not import FailureRetry from the canonical feature folder.'
}

if ($appText -match './pages/FailureRetry') {
    Add-Failure -Failures $failures -Message 'App.tsx still imports FailureRetry from the flat pages folder.'
}

$staleImportChecks = @(
    [pscustomobject]@{ Path = $pagePath; Text = $pageText; Terms = @('../lib/apiClient','../lib/http','../components/') },
    [pscustomobject]@{ Path = $apiPath; Text = $apiText; Terms = @('../lib/apiClient','../lib/http') }
)

foreach ($check in $staleImportChecks) {
    foreach ($term in $check.Terms) {
        if ($check.Text.Contains($term)) {
            Add-Failure -Failures $failures -Message "Stale import/reference '$term' remains in $($check.Path)"
        }
    }
}

if ($failures.Count -gt 0) {
    Write-Host 'P10.2AE validation failed:'
    foreach ($failure in $failures) {
        Write-Host " - $failure"
    }
    exit 1
}

Write-Host 'P10.2AE validation passed.'
