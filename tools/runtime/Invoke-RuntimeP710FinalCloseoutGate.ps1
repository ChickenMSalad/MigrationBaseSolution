[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $ConfigurationPath,

    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $OutputPath
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    if (-not [string]::IsNullOrWhiteSpace($PSCommandPath)) {
        $scriptRoot = Split-Path -Parent $PSCommandPath
    }
}
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    throw 'Unable to resolve script root.'
}

$repoRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)

if ([string]::IsNullOrWhiteSpace($ConfigurationPath)) {
    $ConfigurationPath = Join-Path $repoRoot 'config-samples\runtime-p710-final-closeout-gate.sample.json'
}
if (-not [System.IO.Path]::IsPathRooted($ConfigurationPath)) {
    $ConfigurationPath = Join-Path $repoRoot $ConfigurationPath
}
if (-not (Test-Path -LiteralPath $ConfigurationPath)) {
    throw ('Configuration file not found: {0}' -f $ConfigurationPath)
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $repoRoot 'artifacts\runtime-p710-final-closeout'
}
if (-not [System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = Join-Path $repoRoot $OutputPath
}
if (-not (Test-Path -LiteralPath $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

$config = Get-Content -LiteralPath $ConfigurationPath -Raw | ConvertFrom-Json
if ($null -eq $config.PSObject.Properties['validators']) {
    throw 'Configuration is missing validators.'
}

$results = @()
foreach ($validator in @($config.validators)) {
    if ([string]::IsNullOrWhiteSpace([string]$validator)) {
        continue
    }

    $validatorPath = [string]$validator
    if (-not [System.IO.Path]::IsPathRooted($validatorPath)) {
        $validatorPath = Join-Path $repoRoot $validatorPath
    }

    if (-not (Test-Path -LiteralPath $validatorPath)) {
        $results += [pscustomobject]@{
            Validator = [string]$validator
            Status = 'Missing'
            Message = 'Validator file not found.'
        }
        continue
    }

    try {
        & $validatorPath | Out-Null
        $results += [pscustomobject]@{
            Validator = [string]$validator
            Status = 'Passed'
            Message = 'Completed successfully.'
        }
    }
    catch {
        $results += [pscustomobject]@{
            Validator = [string]$validator
            Status = 'Failed'
            Message = $_.Exception.Message
        }
    }
}

$failed = @($results | Where-Object { $_.Status -ne 'Passed' })
$reportPath = Join-Path $OutputPath 'p710-final-closeout-report.md'
$lines = @()
$lines += '# P7.10 Runtime Final Closeout Report'
$lines += ''
$lines += ('- Generated UTC: {0:o}' -f [DateTimeOffset]::UtcNow)
$lines += ('- Repository root: {0}' -f $repoRoot)
$lines += ('- Passed: {0}' -f @($results | Where-Object { $_.Status -eq 'Passed' }).Count)
$lines += ('- Failed/Missing: {0}' -f $failed.Count)
$lines += ''
$lines += '| Validator | Status | Message |'
$lines += '| --- | --- | --- |'
foreach ($result in $results) {
    $message = ([string]$result.Message).Replace('|', '\|').Replace("`r", ' ').Replace("`n", ' ')
    $lines += ('| `{0}` | {1} | {2} |' -f $result.Validator, $result.Status, $message)
}

Set-Content -LiteralPath $reportPath -Value $lines -Encoding UTF8
Write-Host ('P7.10 final closeout report written to {0}' -f $reportPath)

if ($failed.Count -gt 0) {
    throw ('P7.10 final closeout failed. See report: {0}' -f $reportPath)
}
