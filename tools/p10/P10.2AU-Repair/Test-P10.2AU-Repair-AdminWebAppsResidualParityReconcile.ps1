Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    $current = $PSScriptRoot
    while (-not [string]::IsNullOrWhiteSpace($current)) {
        $srcPath = [System.IO.Path]::Combine($current, 'src')
        $appsPath = [System.IO.Path]::Combine($current, 'apps')
        if ((Test-Path -Path $srcPath -PathType Container) -and (Test-Path -Path $appsPath -PathType Container)) {
            return $current
        }

        $parent = [System.IO.Directory]::GetParent($current)
        if ($null -eq $parent) {
            break
        }

        $current = $parent.FullName
    }

    throw 'Unable to locate repository root from script location.'
}

$repoRoot = Get-RepositoryRoot
$adminSrcRoot = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$originalTestPath = [System.IO.Path]::Combine($repoRoot, 'tools', 'p10', 'P10.2AU', 'Test-P10.2AU-AdminWebAppsResidualParityReconcile.ps1')
$repairReportPath = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10', 'P10.2AU-Repair-AdminWebAppsResidualParityReconcile.md')

if (-not (Test-Path -Path $adminSrcRoot -PathType Container)) {
    throw ('Canonical Admin Web src folder missing: {0}' -f $adminSrcRoot)
}

if (-not (Test-Path -Path $originalTestPath -PathType Leaf)) {
    throw ('Original P10.2AU test script missing after repair: {0}' -f $originalTestPath)
}

if (-not (Test-Path -Path $repairReportPath -PathType Leaf)) {
    throw ('Repair report missing: {0}' -f $repairReportPath)
}

$originalTestContent = Get-Content -Path $originalTestPath -Raw
if ($originalTestContent -match 'src`t\|src`a') {
    throw ('Original P10.2AU test still contains self-flagging escaped pattern text: {0}' -f $originalTestPath)
}
if (-not $originalTestContent.Contains('[char]9')) {
    throw ('Original P10.2AU test was not updated to control-character-safe validation: {0}' -f $originalTestPath)
}

$scanRoots = @(
    [System.IO.Path]::Combine($repoRoot, 'tools', 'p10', 'P10.2AU'),
    [System.IO.Path]::Combine($repoRoot, 'tools', 'p10', 'P10.2AU-Repair')
)

foreach ($scanRoot in $scanRoots) {
    if (-not (Test-Path -Path $scanRoot -PathType Container)) {
        continue
    }

    $scriptFiles = @(Get-ChildItem -Path $scanRoot -Filter '*.ps1' -File)
    foreach ($scriptFile in $scriptFiles) {
        $content = Get-Content -Path $scriptFile.FullName -Raw
        if ($content -match '\$[A-Za-z_][A-Za-z0-9_]*:') {
            throw ('Unsafe colon variable interpolation pattern found in {0}' -f $scriptFile.FullName)
        }
        if ($content -match '@\(\s*@\(') {
            throw ('Nested array literal pattern found in {0}' -f $scriptFile.FullName)
        }
        if ($content.Contains(('src' + [char]9)) -or $content.Contains(('src' + [char]7))) {
            throw ('Corrupted control character pattern found in {0}' -f $scriptFile.FullName)
        }
    }
}

$adminSourceFiles = @(Get-ChildItem -Path $adminSrcRoot -Recurse -File -Include '*.ts','*.tsx' | Where-Object {
    $segments = @($_.FullName -split '[\\/]')
    -not ($segments -contains 'bin') -and -not ($segments -contains 'obj') -and -not ($segments -contains 'node_modules')
})

foreach ($sourceFile in $adminSourceFiles) {
    $content = Get-Content -Path $sourceFile.FullName -Raw
    if ($content -match 'apps/migration-admin-ui') {
        throw ('Canonical Admin Web source references apps/migration-admin-ui: {0}' -f $sourceFile.FullName)
    }
}

Write-Host 'P10.2AU Repair validation passed.'
