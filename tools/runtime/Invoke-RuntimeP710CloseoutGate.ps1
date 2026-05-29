[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string] $RepoRoot,

    [Parameter(Mandatory = $false)]
    [string] $ConfigurationPath,

    [Parameter(Mandatory = $false)]
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

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)
}
$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path

if ([string]::IsNullOrWhiteSpace($ConfigurationPath)) {
    $ConfigurationPath = Join-Path $RepoRoot 'config-samples\runtime-p710-closeout-gate.sample.json'
}
if (-not [System.IO.Path]::IsPathRooted($ConfigurationPath)) {
    $ConfigurationPath = Join-Path $RepoRoot $ConfigurationPath
}
if (-not (Test-Path -LiteralPath $ConfigurationPath)) {
    throw ('Configuration file not found: {0}' -f $ConfigurationPath)
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $RepoRoot 'artifacts\runtime-cleanup-closeout\p7.10-closeout-report.md'
}
if (-not [System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = Join-Path $RepoRoot $OutputPath
}
$outputParent = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($outputParent) -and -not (Test-Path -LiteralPath $outputParent)) {
    New-Item -ItemType Directory -Path $outputParent -Force | Out-Null
}

$config = Get-Content -LiteralPath $ConfigurationPath -Raw | ConvertFrom-Json
if ($null -eq $config.PSObject.Properties['validators']) {
    throw 'Closeout configuration is missing validators.'
}

$failOnMissingValidator = $true
if ($null -ne $config.PSObject.Properties['failOnMissingValidator']) {
    $failOnMissingValidator = [bool]$config.failOnMissingValidator
}

$results = @()
foreach ($validator in @($config.validators)) {
    $validatorText = [string]$validator
    if ([string]::IsNullOrWhiteSpace($validatorText)) { continue }

    $validatorPath = $validatorText
    if (-not [System.IO.Path]::IsPathRooted($validatorPath)) {
        $validatorPath = Join-Path $RepoRoot $validatorPath
    }

    if (-not (Test-Path -LiteralPath $validatorPath)) {
        $status = 'Missing'
        $message = 'Validator file was not found.'
    }
    else {
        try {
            & $validatorPath | Out-Null
            $status = 'Passed'
            $message = 'Completed successfully.'
        }
        catch {
            $status = 'Failed'
            $message = $_.Exception.Message
        }
    }

    $results += [pscustomobject]@{
        Validator = $validatorText
        Status = $status
        Message = $message
    }
}

$failed = @($results | Where-Object { $_.Status -ne 'Passed' })
$passedCount = @($results | Where-Object { $_.Status -eq 'Passed' }).Count
$failedCount = @($failed).Count

$lines = @()
$lines += '# P7.10 Runtime Cleanup Closeout Report'
$lines += ''
$lines += ('- Generated UTC: {0}' -f [DateTimeOffset]::UtcNow.ToString('o'))
$lines += ('- Repository root: {0}' -f $RepoRoot)
$lines += ('- Passed: {0}' -f $passedCount)
$lines += ('- Failed: {0}' -f $failedCount)
$lines += ''
$lines += '| Validator | Status | Message |'
$lines += '| --- | --- | --- |'
foreach ($result in $results) {
    $safeMessage = ([string]$result.Message).Replace('|', '\|').Replace([Environment]::NewLine, ' ')
    $lines += ('| `{0}` | {1} | {2} |' -f $result.Validator, $result.Status, $safeMessage)
}

Set-Content -LiteralPath $OutputPath -Value $lines -Encoding UTF8
Write-Host ('P7.10 closeout report written to {0}' -f $OutputPath)

if ($failedCount -gt 0) {
    throw ('P7.10 closeout gate failed. Failed validators: {0}' -f $failedCount)
}
