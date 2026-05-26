Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if ($PSScriptRoot -and (Test-Path -LiteralPath $PSScriptRoot)) {
        return (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
    }

    return (Get-Location).Path
}

function Assert-FileExists {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw ('Required file is missing: {0}' -f $Path)
    }
}

function Assert-TextContains {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path,

        [Parameter(Mandatory = $true)]
        [string[]] $Terms
    )

    $text = [System.IO.File]::ReadAllText($Path)
    foreach ($term in $Terms) {
        if ($text.IndexOf($term, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
            throw ('File {0} is missing expected term: {1}' -f $Path, $term)
        }
    }
}

function Assert-NoFragilePowerShellPatterns {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Root
    )

$scriptRoots = @(
    (Join-Path $RepoRoot 'tools\runtime')
)

$scripts = @()

foreach ($scriptRoot in $scriptRoots) {
    if (Test-Path -LiteralPath $scriptRoot) {
        $scripts += Get-ChildItem `
            -LiteralPath $scriptRoot `
            -Recurse `
            -File `
            -Filter '*.ps1'
    }
}

$scripts += Get-ChildItem `
    -LiteralPath (Join-Path $RepoRoot 'tools') `
    -File `
    -Filter 'validate-p7.8*.ps1'

$scripts = $scripts |
    Sort-Object FullName -Unique

    foreach ($script in $scripts) {
        $text = [System.IO.File]::ReadAllText($script.FullName)

	$fragileInvocationRegex = '\$' + 'MyInvocation' + '\.' + 'ScriptName'

	if ($text -match $fragileInvocationRegex) {
            throw ('Avoid fragile invocation-root usage in {0}' -f $script.FullName)
        }

        $colonMatches = [System.Text.RegularExpressions.Regex]::Matches($text, '\$[A-Za-z_][A-Za-z0-9_]*:')
        foreach ($match in $colonMatches) {
            $value = $match.Value
            if ($value -notmatch '^\$(script|global|local|private|using|env):$') {
                throw ('Potential fragile PowerShell colon interpolation in {0}: {1}' -f $script.FullName, $value)
            }
        }
    }
}

$repoRoot = Get-RepositoryRoot

$requiredFiles = @(
    'docs/p7/P7.8J-Runtime-Operational-Handoff.md',
    'docs/operations/runtime-deployment-handoff-checklist.md',
    'config-samples/runtime-handoff-checklist.sample.json',
    'tools/validate-p7.8j-runtime-operational-handoff.ps1'
)

foreach ($relativePath in $requiredFiles) {
    Assert-FileExists -Path (Join-Path $repoRoot $relativePath)
}

Assert-TextContains `
    -Path (Join-Path $repoRoot 'docs/p7/P7.8J-Runtime-Operational-Handoff.md') `
    -Terms @('migration.WorkItems', 'migration.ManifestRows', 'bigint', 'handoff evidence', 'No-go conditions')

Assert-TextContains `
    -Path (Join-Path $repoRoot 'docs/operations/runtime-deployment-handoff-checklist.md') `
    -Terms @('Pre-deployment', 'SQL validation', 'App settings validation', 'Smoke verification', 'Handoff')

$configPath = Join-Path $repoRoot 'config-samples/runtime-handoff-checklist.sample.json'
$configText = [System.IO.File]::ReadAllText($configPath)
$configJson = $configText | ConvertFrom-Json

if (-not $configJson.handoffVersion) {
    throw 'runtime-handoff-checklist.sample.json is missing handoffVersion.'
}

if (-not $configJson.canonicalSqlObjects) {
    throw 'runtime-handoff-checklist.sample.json is missing canonicalSqlObjects.'
}

if (-not $configJson.canonicalSettings) {
    throw 'runtime-handoff-checklist.sample.json is missing canonicalSettings.'
}

Assert-NoFragilePowerShellPatterns -Root $repoRoot

Write-Host 'P7.8J runtime operational handoff validation passed.'
