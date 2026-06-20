[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RepoRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $RepoRoot)) {
    throw ('RepoRoot does not exist: ' + $RepoRoot)
}

$scriptPath = Join-Path $RepoRoot 'scripts\Invoke-P7RunAnomalyReport.ps1'
if (-not (Test-Path -LiteralPath $scriptPath)) {
    throw ('Missing script: ' + $scriptPath)
}

$content = Get-Content -LiteralPath $scriptPath -Raw

$requiredMarkers = @(
    'Get-JsonEndpoint',
    '/api/runtime/dashboard/summary',
    '/api/runtime/dashboard/runs',
    '/api/runtime/dashboard/failures',
    '/api/operational/workers/telemetry',
    '/api/operational/events/recent',
    '/api/operational/execution-sessions/recent',
    'Export-Csv',
    'ConvertTo-Json'
)

foreach ($marker in $requiredMarkers) {
    if ($content.IndexOf($marker, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('Missing required marker in Invoke-P7RunAnomalyReport.ps1: ' + $marker)
    }
}

$forbiddenMarkers = @(
    [string][char]0xFFFD,
    'Ã',
    'â',
    'ValidateNotNullOrEmpty',
    'TrimStart("\\")',
    'PackageReference Include="Microsoft.Data.SqlClient" Version='
)

foreach ($marker in $forbiddenMarkers) {
    if ($content.IndexOf($marker, [System.StringComparison]::Ordinal) -ge 0) {
        throw ('Forbidden marker found in Invoke-P7RunAnomalyReport.ps1: ' + $marker)
    }
}

Write-Host 'P7 run anomaly report validation passed.'
