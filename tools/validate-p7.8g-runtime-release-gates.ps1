[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($repoRoot)) {
    $repoRoot = (Get-Location).Path
}

$requiredFiles = @(
    'docs\p7\P7.8G-Runtime-Release-Gates.md',
    'config-samples\runtime-release-gates.sample.json',
    'tools\runtime\Test-RuntimeReleaseGatePrerequisites.ps1',
    'tools\runtime\New-RuntimeReleaseGateReport.ps1',
    'tools\runtime\Invoke-RuntimeReleaseGate.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw "Required P7.8G file is missing: $relativePath"
    }
}

$configPath = Join-Path $repoRoot 'config-samples\runtime-release-gates.sample.json'
try {
    $config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
} catch {
    throw "Runtime release gate sample config is not valid JSON: $($_.Exception.Message)"
}

if (-not ($config.PSObject.Properties.Name -contains 'requiredScripts')) {
    throw 'Runtime release gate sample config is missing requiredScripts.'
}

$scriptFiles = @(
    'tools\runtime\Test-RuntimeReleaseGatePrerequisites.ps1',
    'tools\runtime\New-RuntimeReleaseGateReport.ps1',
    'tools\runtime\Invoke-RuntimeReleaseGate.ps1',
    'tools\validate-p7.8g-runtime-release-gates.ps1'
)

foreach ($relativeScript in $scriptFiles) {
    $scriptPath = Join-Path $repoRoot $relativeScript
    $tokens = $null
    $errors = $null
    [System.Management.Automation.Language.Parser]::ParseFile($scriptPath, [ref] $tokens, [ref] $errors) | Out-Null
    if ($errors -and $errors.Count -gt 0) {
        $message = ($errors | ForEach-Object { $_.Message }) -join '; '
        throw ("PowerShell parser errors in {0}: {1}" -f $relativeScript, $message)
    }

    $scriptText = Get-Content -LiteralPath $scriptPath -Raw
$fragileColonMatches = Select-String `
    -Path $scriptPath `
    -Pattern '\$[A-Za-z_][A-Za-z0-9_]*:' |
    Where-Object {
        $_.Line -notmatch '\$(script|global|local|private|using|env):'
    }
}

$reportScript = Join-Path $repoRoot 'tools\runtime\New-RuntimeReleaseGateReport.ps1'
$tempOutput = Join-Path $repoRoot 'artifacts\p7.8g-validator-smoke'
$resultOutput = @(& $reportScript -RepoRoot $repoRoot -ConfigurationPath $configPath -OutputPath $tempOutput)
$result = $resultOutput | Where-Object { $_ -ne $null } | Select-Object -First 1

if ($null -eq $result) {
    throw 'Runtime release gate report script did not return a result object.'
}

if (-not (Test-Path -LiteralPath $result.JsonReportPath)) {
    throw 'Runtime release gate JSON report was not created.'
}

if (-not (Test-Path -LiteralPath $result.MarkdownReportPath)) {
    throw 'Runtime release gate Markdown report was not created.'
}

Write-Host 'P7.8G runtime release gates drop-in validation passed.'
