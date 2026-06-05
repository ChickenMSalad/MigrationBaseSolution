Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($scriptRoot, '..', '..', '..'))
$adminWebRoot = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web')
$docsRoot = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10')
$reportPath = [System.IO.Path]::Combine($docsRoot, 'P10.2BT-AdminWebLocalBuildVerification.Report.md')
$runnerPath = [System.IO.Path]::Combine($scriptRoot, 'Run-P10.2BT-AdminWebNpmBuild.ps1')

if (-not (Test-Path -Path $adminWebRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}

if (-not (Test-Path -Path $docsRoot -PathType Container)) {
    New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
}

$packageJsonPath = [System.IO.Path]::Combine($adminWebRoot, 'package.json')
$tsconfigPath = [System.IO.Path]::Combine($adminWebRoot, 'tsconfig.json')
$viteConfigPath = [System.IO.Path]::Combine($adminWebRoot, 'vite.config.ts')

$requiredFiles = @(
    [pscustomobject]@{ Label = 'package.json'; Path = $packageJsonPath },
    [pscustomobject]@{ Label = 'tsconfig.json'; Path = $tsconfigPath },
    [pscustomobject]@{ Label = 'vite.config.ts'; Path = $viteConfigPath }
)

foreach ($item in $requiredFiles) {
    if (-not (Test-Path -Path $item.Path -PathType Leaf)) {
        throw ('Required Admin Web file was not found ({0}): {1}' -f $item.Label, $item.Path)
    }
}

$packageRaw = Get-Content -Path $packageJsonPath -Raw
$package = $packageRaw | ConvertFrom-Json

if ($null -eq $package.PSObject.Properties['scripts']) {
    throw 'package.json does not contain a scripts object.'
}

$scripts = $package.scripts
if ($null -eq $scripts.PSObject.Properties['build']) {
    throw 'package.json does not contain a build script.'
}

$buildScript = [string]$scripts.build
if ([string]::IsNullOrWhiteSpace($buildScript)) {
    throw 'package.json build script is empty.'
}

$nodeVersion = 'not found'
$npmVersion = 'not found'
try {
    $nodeOutput = & node --version 2>$null
    if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($nodeOutput)) {
        $nodeVersion = [string]$nodeOutput
    }
} catch {
    $nodeVersion = 'not found'
}

try {
    $npmOutput = & npm --version 2>$null
    if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($npmOutput)) {
        $npmVersion = [string]$npmOutput
    }
} catch {
    $npmVersion = 'not found'
}

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2BT - Admin Web Local Build Verification Report')
[void]$report.Add('')
[void]$report.Add(('Generated: {0:u}' -f (Get-Date)))
[void]$report.Add('')
[void]$report.Add('## Canonical Admin Web')
[void]$report.Add('')
[void]$report.Add(('Root: `{0}`' -f $adminWebRoot))
[void]$report.Add('')
[void]$report.Add('## Required files')
[void]$report.Add('')
foreach ($item in $requiredFiles) {
    [void]$report.Add(('- `{0}`: present' -f $item.Label))
}
[void]$report.Add('')
[void]$report.Add('## npm build script')
[void]$report.Add('')
[void]$report.Add(('`npm run build` resolves to: `{0}`' -f $buildScript))
[void]$report.Add('')
[void]$report.Add('## Local tool availability')
[void]$report.Add('')
[void]$report.Add(('- node: `{0}`' -f $nodeVersion))
[void]$report.Add(('- npm: `{0}`' -f $npmVersion))
[void]$report.Add('')
[void]$report.Add('## Build runner')
[void]$report.Add('')
[void]$report.Add('Run the dedicated build runner when you want a captured Admin Web build log:')
[void]$report.Add('')
[void]$report.Add('```powershell')
[void]$report.Add('powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.2BT\Run-P10.2BT-AdminWebNpmBuild.ps1')
[void]$report.Add('```')

Set-Content -Path $reportPath -Value $report -Encoding UTF8

$runner = New-Object 'System.Collections.Generic.List[string]'
[void]$runner.Add('Set-StrictMode -Version 2.0')
[void]$runner.Add('$ErrorActionPreference = ''Stop''')
[void]$runner.Add('')
[void]$runner.Add('$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path')
[void]$runner.Add('$repoRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($scriptRoot, ''..'', ''..'', ''..''))')
[void]$runner.Add('$adminWebRoot = [System.IO.Path]::Combine($repoRoot, ''src'', ''Admin'', ''Migration.Admin.Web'')')
[void]$runner.Add('$docsRoot = [System.IO.Path]::Combine($repoRoot, ''docs'', ''P10'')')
[void]$runner.Add('$logPath = [System.IO.Path]::Combine($docsRoot, ''P10.2BT-AdminWebNpmBuild.log'')')
[void]$runner.Add('')
[void]$runner.Add('if (-not (Test-Path -Path $adminWebRoot -PathType Container)) {')
[void]$runner.Add('    throw (''Admin Web root was not found: {0}'' -f $adminWebRoot)')
[void]$runner.Add('}')
[void]$runner.Add('')
[void]$runner.Add('if (-not (Test-Path -Path $docsRoot -PathType Container)) {')
[void]$runner.Add('    New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null')
[void]$runner.Add('}')
[void]$runner.Add('')
[void]$runner.Add('$previousLocation = Get-Location')
[void]$runner.Add('try {')
[void]$runner.Add('    Set-Location -Path $adminWebRoot')
[void]$runner.Add('    $output = & npm run build 2>&1')
[void]$runner.Add('    $exitCode = $LASTEXITCODE')
[void]$runner.Add('    Set-Content -Path $logPath -Value @($output) -Encoding UTF8')
[void]$runner.Add('    if ($exitCode -ne 0) {')
[void]$runner.Add('        throw (''Admin Web npm build failed with exit code {0}. See {1}'' -f $exitCode, $logPath)')
[void]$runner.Add('    }')
[void]$runner.Add('    Write-Host (''Admin Web npm build passed. Log: {0}'' -f $logPath)')
[void]$runner.Add('}')
[void]$runner.Add('finally {')
[void]$runner.Add('    Set-Location -Path $previousLocation')
[void]$runner.Add('}')

Set-Content -Path $runnerPath -Value $runner -Encoding UTF8

Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host ('Wrote build runner: {0}' -f $runnerPath)
Write-Host 'P10.2BT Admin Web local build verification applied.'
