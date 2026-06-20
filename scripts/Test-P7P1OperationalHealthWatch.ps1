[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RepoRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repo = [System.IO.Path]::GetFullPath($RepoRoot)
$scriptsDir = Join-Path $repo 'scripts'
$invokePath = Join-Path $scriptsDir 'Invoke-P7OperationalHealthWatch.ps1'
$testPath = Join-Path $scriptsDir 'Test-P7P1OperationalHealthWatch.ps1'

foreach ($path in @($invokePath, $testPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw ('Required script missing: ' + $path)
    }

    $content = Get-Content -LiteralPath $path -Raw
    if ($null -eq $content -or $content.Length -eq 0) {
        throw ('Script is empty: ' + $path)
    }

    $forbidden = @(
        '[ValidateNotNullOrEmpty()]',
        '.TrimStart("\\")',
        'Ã',
        'Â',
        '�'
    )

    foreach ($marker in $forbidden) {
        if ($content.IndexOf($marker, [System.StringComparison]::Ordinal) -ge 0) {
            throw ('Forbidden marker found in ' + $path + ': ' + $marker)
        }
    }
}

$invokeContent = Get-Content -LiteralPath $invokePath -Raw
$requiredMarkers = @(
    'api/runtime/dashboard/summary',
    'api/runtime/dashboard/runs',
    'api/runtime/dashboard/failures',
    'api/operational/workers/telemetry',
    'api/operational/events/recent',
    'Export-Csv'
)

foreach ($marker in $requiredMarkers) {
    if ($invokeContent.IndexOf($marker, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw ('Invoke script missing required marker: ' + $marker)
    }
}

Write-Host 'P7 operational health watch validation passed.'
