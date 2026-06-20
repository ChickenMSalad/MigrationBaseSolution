[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RepoRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repo = [System.IO.Path]::GetFullPath($RepoRoot)
if (-not (Test-Path -LiteralPath $repo)) {
    throw ('RepoRoot does not exist: ' + $repo)
}

$watchPath = Join-Path $repo 'scripts\Watch-P7RunProgress.ps1'
if (-not (Test-Path -LiteralPath $watchPath)) {
    throw ('Watch-P7RunProgress.ps1 not found: ' + $watchPath)
}

$content = Get-Content -LiteralPath $watchPath -Raw
if ($null -eq $content) {
    throw 'Watch-P7RunProgress.ps1 was unreadable.'
}

$required = @(
    'api/runtime/dashboard/runs',
    'Export-Csv',
    'ConvertFrom-Json',
    'Set-StrictMode -Version Latest',
    'UseBasicParsing'
)

foreach ($marker in $required) {
    if ($content.IndexOf($marker, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('Missing required marker: ' + $marker)
    }
}

$forbidden = @(
    'TrimStart("\\")',
    'ValidateNotNullOrEmpty',
    'Ã',
    'â'
)

foreach ($marker in $forbidden) {
    if ($content.IndexOf($marker, [System.StringComparison]::Ordinal) -ge 0) {
        throw ('Forbidden marker found: ' + $marker)
    }
}

$tokens = $null
$parseErrors = $null
[System.Management.Automation.Language.Parser]::ParseFile($watchPath, [ref]$tokens, [ref]$parseErrors) | Out-Null
if ($null -ne $parseErrors -and $parseErrors.Count -gt 0) {
    $messages = @($parseErrors | ForEach-Object { $_.Message }) -join '; '
    throw ('PowerShell parser errors found: ' + $messages)
}

Write-Host 'P7 P1 run progress watch validation passed.'
