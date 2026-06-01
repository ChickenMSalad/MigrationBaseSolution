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
$appsSrcRoot = [System.IO.Path]::Combine($repoRoot, 'apps', 'migration-admin-ui', 'src')
$adminSrcRoot = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$reportPath = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10', 'P10.2AU-AdminWebAppsResidualParityReconcile.Report.md')

$requiredDirectories = @(
    $appsSrcRoot,
    $adminSrcRoot,
    [System.IO.Path]::Combine($adminSrcRoot, 'features'),
    [System.IO.Path]::Combine($adminSrcRoot, 'components')
)

foreach ($directory in $requiredDirectories) {
    if (-not (Test-Path -Path $directory -PathType Container)) {
        throw ('Required directory missing: {0}' -f $directory)
    }
}

if (-not (Test-Path -Path $reportPath -PathType Leaf)) {
    throw ('Expected report missing: {0}' -f $reportPath)
}

$reportText = Get-Content -Path $reportPath -Raw
if ($reportText -notmatch 'P10\.2AU - Admin Web Apps Residual Parity Reconcile Report') {
    throw ('Report header missing or invalid: {0}' -f $reportPath)
}

$scanRoots = @(
    [System.IO.Path]::Combine($repoRoot, 'tools', 'p10', 'P10.2AU')
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

Write-Host 'P10.2AU Admin Web apps residual parity reconcile validation passed.'
