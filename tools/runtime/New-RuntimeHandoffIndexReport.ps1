[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,

    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $ConfigurationPath = 'config-samples\runtime-handoff-index.sample.json',

    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $OutputPath = 'artifacts\runtime-handoff\runtime-handoff-index.md'
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Resolve-RepoPath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Root,

        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return [System.IO.Path]::Combine($Root, $Path)
}

$repoFullPath = (Resolve-Path -LiteralPath $RepoRoot).Path
$configFullPath = Resolve-RepoPath -Root $repoFullPath -Path $ConfigurationPath
if (-not (Test-Path -LiteralPath $configFullPath)) {
    throw ('Configuration file not found: {0}' -f $configFullPath)
}

$config = Get-Content -LiteralPath $configFullPath -Raw | ConvertFrom-Json
$outputFullPath = Resolve-RepoPath -Root $repoFullPath -Path $OutputPath
$outputParent = Split-Path -Parent $outputFullPath
if (-not [string]::IsNullOrWhiteSpace($outputParent) -and -not (Test-Path -LiteralPath $outputParent)) {
    New-Item -ItemType Directory -Path $outputParent -Force | Out-Null
}

$lines = New-Object System.Collections.ArrayList
[void] $lines.Add('# Runtime Handoff Index Report')
[void] $lines.Add('')
[void] $lines.Add(('- Generated UTC: {0}' -f [DateTimeOffset]::UtcNow.ToString('o')))
[void] $lines.Add(('- Repository root: {0}' -f $repoFullPath))
[void] $lines.Add('')
[void] $lines.Add('## Required validators')
[void] $lines.Add('')

foreach ($validator in @($config.requiredValidators)) {
    $validatorPath = Resolve-RepoPath -Root $repoFullPath -Path ([string] $validator)
    $status = if (Test-Path -LiteralPath $validatorPath) { 'Present' } else { 'Missing' }
    [void] $lines.Add(('- `{0}` - {1}' -f $validator, $status))
}

[void] $lines.Add('')
[void] $lines.Add('## Required evidence')
[void] $lines.Add('')
foreach ($evidence in @($config.requiredEvidence)) {
    [void] $lines.Add(('- {0}' -f $evidence))
}

Set-Content -LiteralPath $outputFullPath -Value $lines -Encoding UTF8
Write-Host ('Runtime handoff index report written to {0}' -f $outputFullPath)
