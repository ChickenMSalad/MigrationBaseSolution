Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    if ($PSScriptRoot) {
        return (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
    }

    return (Get-Location).Path
}

$repoRoot = Get-RepoRoot
$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.2CG-Repair'
$reportPath = Join-Path $artifactRoot 'P10.2CG-Repair-AdminWebRouteSmokeHarness.Apply.md'
$docPath = Join-Path $repoRoot 'docs\P10\P10.2CG-Repair-AdminWebRouteSmokeHarness.md'

if (-not (Test-Path -Path $artifactRoot -PathType Container)) {
    New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
}

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2CG Repair - Admin Web Route Smoke Harness')
[void]$report.Add('')
[void]$report.Add('This repair corrects the P10.2CG test validator only.')
[void]$report.Add('')
[void]$report.Add('The original P10.2CG test treated route/page path text inside a PowerShell runner as if it were a TypeScript import and failed with a false positive.')
[void]$report.Add('')
[void]$report.Add('No Admin Web source files are changed by this repair.')

$report | Set-Content -Path $reportPath -Encoding UTF8

if (-not (Test-Path -Path (Split-Path -Path $docPath -Parent) -PathType Container)) {
    New-Item -ItemType Directory -Path (Split-Path -Path $docPath -Parent) -Force | Out-Null
}

$doc = New-Object 'System.Collections.Generic.List[string]'
[void]$doc.Add('# P10.2CG Repair - Admin Web Route Smoke Harness')
[void]$doc.Add('')
[void]$doc.Add('Repairs the P10.2CG validator false positive around extension-bearing TSX-like path text inside the PowerShell route smoke runner.')
[void]$doc.Add('')
[void]$doc.Add('## Scope')
[void]$doc.Add('')
[void]$doc.Add('- No Admin Web source changes')
[void]$doc.Add('- No route changes')
[void]$doc.Add('- No runner rewrite')
[void]$doc.Add('- Validates the existing P10.2CG route smoke harness deliverables')

$doc | Set-Content -Path $docPath -Encoding UTF8

Write-Host ('Wrote repair report: {0}' -f $reportPath)
Write-Host 'P10.2CG Repair applied.'
