Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$workflowDir = Join-Path $repoRoot '.github\workflows'
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$workflowPath = Join-Path $workflowDir 'admin-web-build-validation.yml'
$samplePath = Join-Path $workflowDir 'admin-web-build-validation.p10.sample.yml'
$docsDir = Join-Path $repoRoot 'docs\P10'
$reportPath = Join-Path $docsDir 'P10.2CJ-Repair-AdminWebCiBuildValidationWorkflow.md'

if (-not (Test-Path -Path $adminWebRoot -PathType Container)) {
    throw ('Admin Web root not found: {0}' -f $adminWebRoot)
}

if (-not (Test-Path -Path $workflowDir -PathType Container)) {
    New-Item -ItemType Directory -Path $workflowDir -Force | Out-Null
}
if (-not (Test-Path -Path $docsDir -PathType Container)) {
    New-Item -ItemType Directory -Path $docsDir -Force | Out-Null
}

$expectedWorkflow = @'
name: Admin Web Build Validation

on:
  pull_request:
    paths:
      - 'src/Admin/Migration.Admin.Web/**'
      - '.github/workflows/admin-web-build-validation.yml'
  push:
    branches:
      - main
    paths:
      - 'src/Admin/Migration.Admin.Web/**'
      - '.github/workflows/admin-web-build-validation.yml'

jobs:
  admin-web-build:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup Node
        uses: actions/setup-node@v4
        with:
          node-version: '22'
          cache: npm
          cache-dependency-path: src/Admin/Migration.Admin.Web/package-lock.json

      - name: Install Admin Web dependencies
        working-directory: src/Admin/Migration.Admin.Web
        run: npm ci

      - name: Build Admin Web
        working-directory: src/Admin/Migration.Admin.Web
        run: npm run build
'@

$workflowExists = Test-Path -Path $workflowPath -PathType Leaf
$sampleCreated = $false
$workflowText = ''
if ($workflowExists) {
    $workflowText = Get-Content -Path $workflowPath -Raw
} else {
    Set-Content -Path $samplePath -Value $expectedWorkflow -Encoding UTF8
    $sampleCreated = $true
}

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2CJ Repair - Admin Web CI Build Validation Workflow')
[void]$report.Add('')
[void]$report.Add(('Repo root: `{0}`' -f $repoRoot))
[void]$report.Add(('Admin Web root: `{0}`' -f $adminWebRoot))
[void]$report.Add(('Existing workflow path: `{0}`' -f $workflowPath))
[void]$report.Add(('Workflow exists: `{0}`' -f $workflowExists))
[void]$report.Add(('Sample created: `{0}`' -f $sampleCreated))
[void]$report.Add('')
[void]$report.Add('## Validation Signals')

if ($workflowExists) {
    $signals = New-Object 'System.Collections.Generic.List[string]'
    if ($workflowText -like '*src/Admin/Migration.Admin.Web*') { [void]$signals.Add('references canonical Admin Web path') }
    if ($workflowText -like '*npm ci*') { [void]$signals.Add('runs npm ci') }
    if ($workflowText -like '*npm run build*') { [void]$signals.Add('runs npm run build') }
    if ($workflowText -like '*actions/setup-node*') { [void]$signals.Add('uses setup-node') }

    if ($signals.Count -eq 0) {
        [void]$report.Add('- Existing workflow was preserved. No expected Admin Web build-validation signals were found.')
    } else {
        foreach ($signal in $signals) {
            [void]$report.Add(('- Existing workflow {0}.' -f $signal))
        }
    }
    [void]$report.Add('')
    [void]$report.Add('Existing workflow was not overwritten. If it intentionally differs, keep it. If it lacks required Admin Web validation, copy the companion sample pattern manually or update the existing workflow in a dedicated CI change.')
} else {
    [void]$report.Add('- No existing workflow was found, so a companion sample workflow was created.')
    [void]$report.Add(('- Sample path: `{0}`' -f $samplePath))
}

Set-Content -Path $reportPath -Value $report -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
if ($sampleCreated) { Write-Host ('Created sample workflow: {0}' -f $samplePath) }
Write-Host 'P10.2CJ Repair applied.'
