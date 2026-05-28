[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string] $RepoRoot,

    [Parameter(Mandatory = $false)]
    [string] $ConfigurationPath,

    [Parameter(Mandatory = $false)]
    [string] $OutputPath,

    [Parameter(Mandatory = $false)]
    [switch] $ContinueOnFailure
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
    $ConfigurationPath = Join-Path $RepoRoot 'config-samples\runtime-cleanup-closeout-gate.sample.json'
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $RepoRoot 'artifacts\runtime-cleanup-closeout\p7.9-closeout-report.md'
}

if (-not (Test-Path -LiteralPath $ConfigurationPath)) {
    throw ('Closeout gate configuration file was not found: {0}' -f $ConfigurationPath)
}

$config = Get-Content -LiteralPath $ConfigurationPath -Raw | ConvertFrom-Json
if ($null -eq $config.PSObject.Properties['validatorScripts']) {
    throw 'Closeout gate configuration is missing validatorScripts.'
}

$validatorScripts = @($config.validatorScripts)
if ($validatorScripts.Count -eq 0) {
    throw 'Closeout gate configuration contains no validator scripts.'
}

$results = @()
$failedCount = 0
$passedCount = 0
$skippedCount = 0

foreach ($relativeScript in $validatorScripts) {
    $scriptPath = Join-Path $RepoRoot $relativeScript
    $status = 'Skipped'
    $message = 'Validator script was not found.'
    $startedUtc = [DateTimeOffset]::UtcNow
    $completedUtc = $startedUtc

    if (Test-Path -LiteralPath $scriptPath) {
        $status = 'Passed'
        $message = 'Completed successfully.'
        try {
            & $scriptPath
        }
        catch {
            $status = 'Failed'
            $message = $_.Exception.Message
        }
        $completedUtc = [DateTimeOffset]::UtcNow
    }

    if ($status -eq 'Passed') { $passedCount++ }
    elseif ($status -eq 'Failed') { $failedCount++ }
    else { $skippedCount++ }

    $results += [pscustomobject]@{
        Script = $relativeScript
        Status = $status
        Message = $message
        StartedUtc = $startedUtc.ToString('o')
        CompletedUtc = $completedUtc.ToString('o')
    }

    if ($status -eq 'Failed' -and -not $ContinueOnFailure) {
        break
    }
}

$outputDirectory = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

$lines = @()
$lines += '# P7.9 Runtime Cleanup Closeout Report'
$lines += ''
$lines += ('- Generated UTC: {0}' -f ([DateTimeOffset]::UtcNow.ToString('o')))
$lines += ('- Repository root: {0}' -f $RepoRoot)
$lines += ('- Passed: {0}' -f $passedCount)
$lines += ('- Failed: {0}' -f $failedCount)
$lines += ('- Skipped: {0}' -f $skippedCount)
$lines += ''
$lines += '| Validator | Status | Message |'
$lines += '| --- | --- | --- |'
foreach ($result in $results) {
    $safeMessage = [string]$result.Message
    $safeMessage = $safeMessage.Replace('|', '\|')
    $lines += ('| `{0}` | {1} | {2} |' -f $result.Script, $result.Status, $safeMessage)
}

Set-Content -LiteralPath $OutputPath -Value $lines -Encoding UTF8
Write-Host ('Runtime cleanup closeout report written to {0}' -f $OutputPath)

if ($failedCount -gt 0) {
    throw ('Runtime cleanup closeout gate failed. Failed validators: {0}' -f $failedCount)
}
