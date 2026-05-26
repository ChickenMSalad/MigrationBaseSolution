[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string] $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,

    [Parameter(Mandatory = $false)]
    [string] $ConfigurationPath = (Join-Path $RepoRoot 'config-samples\runtime-release-gates.sample.json'),

    [Parameter(Mandatory = $false)]
    [string] $OutputPath = (Join-Path $RepoRoot 'artifacts\runtime-release-gate'),

    [Parameter(Mandatory = $false)]
    [switch] $SkipChildValidators
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Read-JsonFile {
    param([Parameter(Mandatory = $true)] [string] $Path)
    return (Get-Content -LiteralPath $Path -Raw) | ConvertFrom-Json
}

if (-not (Test-Path -LiteralPath $ConfigurationPath)) {
    throw "Release gate configuration not found: $ConfigurationPath"
}

$config = Read-JsonFile -Path $ConfigurationPath

if (-not $SkipChildValidators -and ($config.PSObject.Properties.Name -contains 'requiredScripts')) {
    foreach ($relativeScript in @($config.requiredScripts)) {
        $scriptPath = Join-Path $RepoRoot $relativeScript
        if (-not (Test-Path -LiteralPath $scriptPath)) {
            throw "Required validator script is missing: $relativeScript"
        }

        Write-Host "Running validator: $relativeScript"
        & $scriptPath
    }
}

$reportScript = Join-Path $PSScriptRoot 'New-RuntimeReleaseGateReport.ps1'
$resultOutput = @(& $reportScript -RepoRoot $RepoRoot -ConfigurationPath $ConfigurationPath -OutputPath $OutputPath)
$result = $resultOutput | Where-Object { $_ -ne $null } | Select-Object -First 1
if ($null -eq $result) {
    throw 'Runtime release gate report script did not return a result object.'
}

if ($result.ErrorCount -gt 0) {
    throw "Runtime release gate failed. See report: $($result.MarkdownReportPath)"
}

Write-Host "Runtime release gate completed. Report: $($result.MarkdownReportPath)"
$result
