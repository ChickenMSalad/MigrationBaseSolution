Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$workflowDir = Join-Path $repoRoot '.github\workflows'
$workflowPath = Join-Path $workflowDir 'admin-web-build-validation.yml'
$docsDir = Join-Path $repoRoot 'docs\P10'
$docPath = Join-Path $docsDir 'P10.2CJ-AdminWebCiBuildValidationWorkflow.md'

if (-not (Test-Path -Path $workflowDir -PathType Container)) {
    New-Item -Path $workflowDir -ItemType Directory -Force | Out-Null
}
if (-not (Test-Path -Path $docsDir -PathType Container)) {
    New-Item -Path $docsDir -ItemType Directory -Force | Out-Null
}

$workflowLines = New-Object System.Collections.Generic.List[string]
[void]$workflowLines.Add('name: Admin Web Build Validation')
[void]$workflowLines.Add('')
[void]$workflowLines.Add('on:')
[void]$workflowLines.Add('  workflow_dispatch:')
[void]$workflowLines.Add('  pull_request:')
[void]$workflowLines.Add('    paths:')
[void]$workflowLines.Add("      - 'src/Admin/Migration.Admin.Web/**'")
[void]$workflowLines.Add("      - '.github/workflows/admin-web-build-validation.yml'")
[void]$workflowLines.Add('  push:')
[void]$workflowLines.Add('    branches:')
[void]$workflowLines.Add('      - main')
[void]$workflowLines.Add('    paths:')
[void]$workflowLines.Add("      - 'src/Admin/Migration.Admin.Web/**'")
[void]$workflowLines.Add("      - '.github/workflows/admin-web-build-validation.yml'")
[void]$workflowLines.Add('')
[void]$workflowLines.Add('jobs:')
[void]$workflowLines.Add('  admin-web-build:')
[void]$workflowLines.Add('    name: Build canonical Admin Web')
[void]$workflowLines.Add('    runs-on: ubuntu-latest')
[void]$workflowLines.Add('    defaults:')
[void]$workflowLines.Add('      run:')
[void]$workflowLines.Add('        working-directory: src/Admin/Migration.Admin.Web')
[void]$workflowLines.Add('    steps:')
[void]$workflowLines.Add('      - name: Checkout')
[void]$workflowLines.Add('        uses: actions/checkout@v4')
[void]$workflowLines.Add('')
[void]$workflowLines.Add('      - name: Setup Node.js')
[void]$workflowLines.Add('        uses: actions/setup-node@v4')
[void]$workflowLines.Add('        with:')
[void]$workflowLines.Add("          node-version: '22'")
[void]$workflowLines.Add('          cache: npm')
[void]$workflowLines.Add('          cache-dependency-path: src/Admin/Migration.Admin.Web/package-lock.json')
[void]$workflowLines.Add('')
[void]$workflowLines.Add('      - name: Restore Admin Web dependencies')
[void]$workflowLines.Add('        run: npm ci')
[void]$workflowLines.Add('')
[void]$workflowLines.Add('      - name: Build Admin Web')
[void]$workflowLines.Add('        run: npm run build')

$workflowContent = [string]::Join([Environment]::NewLine, $workflowLines.ToArray()) + [Environment]::NewLine
if (Test-Path -Path $workflowPath -PathType Leaf) {
    $existing = Get-Content -Path $workflowPath -Raw
    if ($existing -ne $workflowContent) {
        throw ('Workflow already exists with different content: {0}' -f $workflowPath)
    }
    Write-Host ('Workflow already present: {0}' -f $workflowPath)
} else {
    Set-Content -Path $workflowPath -Value $workflowContent -Encoding UTF8
    Write-Host ('Wrote workflow: {0}' -f $workflowPath)
}

$docLines = New-Object System.Collections.Generic.List[string]
[void]$docLines.Add('# P10.2CJ - Admin Web CI Build Validation Workflow')
[void]$docLines.Add('')
[void]$docLines.Add('## Purpose')
[void]$docLines.Add('')
[void]$docLines.Add('Adds a path-scoped CI build check for the canonical Admin Web package.')
[void]$docLines.Add('')
[void]$docLines.Add('## Workflow')
[void]$docLines.Add('')
[void]$docLines.Add('- `.github/workflows/admin-web-build-validation.yml`')
[void]$docLines.Add('- Uses Node.js 22.')
[void]$docLines.Add('- Runs from `src/Admin/Migration.Admin.Web`.')
[void]$docLines.Add('- Executes `npm ci` followed by `npm run build`.')
[void]$docLines.Add('')
[void]$docLines.Add('## Notes')
[void]$docLines.Add('')
[void]$docLines.Add('This set does not deploy anything and does not modify Admin Web source files.')
$docContent = [string]::Join([Environment]::NewLine, $docLines.ToArray()) + [Environment]::NewLine
Set-Content -Path $docPath -Value $docContent -Encoding UTF8
Write-Host ('Wrote documentation: {0}' -f $docPath)
Write-Host 'P10.2CJ Admin Web CI build validation workflow applied.'
