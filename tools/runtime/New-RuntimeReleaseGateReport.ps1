[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string] $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,

    [Parameter(Mandatory = $false)]
    [string] $ConfigurationPath = (Join-Path $RepoRoot 'config-samples\runtime-release-gates.sample.json'),

    [Parameter(Mandatory = $false)]
    [string] $OutputPath = (Join-Path $RepoRoot 'artifacts\runtime-release-gate')
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function ConvertTo-JsonDepth {
    param([Parameter(Mandatory = $true)] $InputObject)
    return $InputObject | ConvertTo-Json -Depth 12
}

if (-not (Test-Path -LiteralPath $OutputPath)) {
    New-Item -ItemType Directory -Force -Path $OutputPath | Out-Null
}

$prereqScript = Join-Path $PSScriptRoot 'Test-RuntimeReleaseGatePrerequisites.ps1'
if (-not (Test-Path -LiteralPath $prereqScript)) {
    throw "Prerequisite script not found: $prereqScript"
}

$prereqOutput = @(& $prereqScript -RepoRoot $RepoRoot -ConfigurationPath $ConfigurationPath)
$prerequisites = $prereqOutput | Where-Object { $_ -ne $null } | Select-Object -First 1
if ($null -eq $prerequisites) {
    throw 'Prerequisite script did not return a result object.'
}

$issues = @()
if ($prerequisites.PSObject.Properties.Name -contains 'Issues') {
    $issues = @($prerequisites.Issues)
}

$errorIssues = @($issues | Where-Object { $_.Severity -eq 'Error' })
$warningIssues = @($issues | Where-Object { $_.Severity -eq 'Warning' })

$summary = [ordered]@{
    reportName = 'Runtime Release Gate Report'
    generatedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
    repoRoot = $RepoRoot
    configurationPath = $ConfigurationPath
    prerequisiteIssueCount = $issues.Count
    errorCount = $errorIssues.Count
    warningCount = $warningIssues.Count
    prerequisites = $prerequisites
}

$jsonPath = Join-Path $OutputPath 'runtime-release-gate-report.json'
$mdPath = Join-Path $OutputPath 'runtime-release-gate-report.md'

ConvertTo-JsonDepth -InputObject $summary | Set-Content -LiteralPath $jsonPath -Encoding UTF8

$lines = New-Object 'System.Collections.Generic.List[string]'
$lines.Add('# Runtime Release Gate Report') | Out-Null
$lines.Add('') | Out-Null
$lines.Add("Generated UTC: $($summary.generatedAtUtc)") | Out-Null
$lines.Add('') | Out-Null
$lines.Add("Errors: $($summary.errorCount)") | Out-Null
$lines.Add("Warnings: $($summary.warningCount)") | Out-Null
$lines.Add('') | Out-Null
$lines.Add('## Issues') | Out-Null
$lines.Add('') | Out-Null

if ($issues.Count -eq 0) {
    $lines.Add('No prerequisite issues were found.') | Out-Null
} else {
    foreach ($issue in $issues) {
        $lines.Add("- [$($issue.Severity)] $($issue.Code): $($issue.Message)") | Out-Null
    }
}

$lines | Set-Content -LiteralPath $mdPath -Encoding UTF8

[pscustomobject]@{
    JsonReportPath = $jsonPath
    MarkdownReportPath = $mdPath
    ErrorCount = $summary.errorCount
    WarningCount = $summary.warningCount
}
