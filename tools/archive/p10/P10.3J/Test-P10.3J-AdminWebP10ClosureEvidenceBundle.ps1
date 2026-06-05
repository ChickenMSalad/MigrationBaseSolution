Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$repoRoot = $repoRoot.ProviderPath

$applyScript = Join-Path $scriptRoot 'Apply-P10.3J-AdminWebP10ClosureEvidenceBundle.ps1'
$runScript = Join-Path $scriptRoot 'Run-P10.3J-AdminWebP10ClosureEvidenceBundle.ps1'
$testScript = Join-Path $scriptRoot 'Test-P10.3J-AdminWebP10ClosureEvidenceBundle.ps1'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.3J-AdminWebP10ClosureEvidenceBundle.md'

$requiredFiles = New-Object 'System.Collections.Generic.List[string]'
[void]$requiredFiles.Add($applyScript)
[void]$requiredFiles.Add($runScript)
[void]$requiredFiles.Add($testScript)
[void]$requiredFiles.Add($reportPath)

foreach ($path in $requiredFiles) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw ('Required P10.3J file was not found: {0}' -f $path)
    }
}

$scriptFiles = New-Object 'System.Collections.Generic.List[string]'
[void]$scriptFiles.Add($applyScript)
[void]$scriptFiles.Add($runScript)
[void]$scriptFiles.Add($testScript)

foreach ($path in $scriptFiles) {
    $content = Get-Content -LiteralPath $path -Raw
    [void][scriptblock]::Create($content)
}

$reportContent = Get-Content -LiteralPath $reportPath -Raw
if ($reportContent.IndexOf('P10.3J - Admin Web P10 Closure Evidence Bundle') -lt 0) {
    throw 'Closure evidence report is missing the expected title.'
}
if ($reportContent.IndexOf('Deferred items') -lt 0) {
    throw 'Closure evidence report is missing deferred items guidance.'
}

Write-Host 'P10.3J Admin Web P10 closure evidence bundle validation passed.'
