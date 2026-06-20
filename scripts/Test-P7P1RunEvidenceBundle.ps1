param(
    [Parameter(Mandatory = $true)]
    [string]$RepoRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $RepoRoot)) {
    throw 'RepoRoot does not exist.'
}

$scripts = Join-Path $RepoRoot 'scripts'
$invoke = Join-Path $scripts 'Invoke-P7RunEvidenceBundle.ps1'
$install = Join-Path $scripts 'Install-P7P1RunEvidenceBundle.ps1'

if (-not (Test-Path -LiteralPath $invoke)) {
    throw 'Invoke-P7RunEvidenceBundle.ps1 not found.'
}

if (-not (Test-Path -LiteralPath $install)) {
    throw 'Install-P7P1RunEvidenceBundle.ps1 not found.'
}

$content = Get-Content -LiteralPath $invoke -Raw
if ($null -eq $content) {
    throw 'Invoke script was empty or unreadable.'
}

$required = @(
    'Invoke-WebRequest',
    '/api/runtime/dashboard/summary',
    '/api/runtime/dashboard/runs',
    '/api/runtime/dashboard/failures',
    '/api/operational/events/recent',
    '/api/operational/workers/telemetry',
    '/api/operational/execution-sessions/recent',
    'Export-Csv',
    'ConvertTo-Json'
)

foreach ($marker in $required) {
    if ($content.IndexOf($marker, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw ('Missing required marker: ' + $marker)
    }
}

$forbidden = @(
    'ValidateNotNullOrEmpty',
    'TrimStart("\\")',
    'TrimStart(''\\'')',
    'Ã',
    'â'
)

foreach ($marker in $forbidden) {
    if ($content.IndexOf($marker, [System.StringComparison]::Ordinal) -ge 0) {
        throw ('Forbidden marker found: ' + $marker)
    }
}

$replacement = [char]0xfffd
if ($content.IndexOf($replacement) -ge 0) {
    throw 'Replacement character corruption found.'
}

Write-Host 'P7 P1 run evidence bundle validation passed.'
