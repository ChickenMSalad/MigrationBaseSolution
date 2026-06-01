Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    $current = (Get-Location).Path
    while ($null -ne $current -and $current.Length -gt 0) {
        $candidate = [System.IO.Path]::Combine($current, 'MigrationBaseSolution.sln')
        if (Test-Path -Path $candidate -PathType Leaf) {
            return $current
        }
        $parent = [System.IO.Directory]::GetParent($current)
        if ($null -eq $parent) { break }
        $current = $parent.FullName
    }
    throw 'Could not locate repository root containing MigrationBaseSolution.sln.'
}

function Test-PathSegmentsContain {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$Segment
    )
    $segments = $Path.Split([System.IO.Path]::DirectorySeparatorChar)
    return ($segments -contains $Segment)
}

$repoRoot = Get-RepositoryRoot
$adminSrc = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$reportPath = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10', 'P10.2AT-AdminWebCanonicalImportSweep.Report.md')
if (-not (Test-Path -Path $adminSrc -PathType Container)) {
    throw ('Canonical Admin Web source folder not found: {0}' -f $adminSrc)
}
if (-not (Test-Path -Path $reportPath -PathType Leaf)) {
    throw ('Expected P10.2AT report not found: {0}' -f $reportPath)
}

$toolRoot = [System.IO.Path]::Combine($repoRoot, 'tools', 'p10', 'P10.2AT')
$scripts = @(Get-ChildItem -Path $toolRoot -File -Filter '*.ps1')
foreach ($script in $scripts) {
    $tokens = $null
    $parseErrors = $null
    [System.Management.Automation.Language.Parser]::ParseFile($script.FullName, [ref]$tokens, [ref]$parseErrors) | Out-Null
    if ($null -ne $parseErrors -and $parseErrors.Count -gt 0) {
        throw ('PowerShell parse errors found in {0}: {1}' -f $script.FullName, $parseErrors[0].Message)
    }
}

$allScripts = @(Get-ChildItem -Path $toolRoot -Recurse -File -Filter 'Apply-*.ps1')
foreach ($script in $allScripts) {
    if (Test-PathSegmentsContain -Path $script.FullName -Segment 'bin') { continue }
    if (Test-PathSegmentsContain -Path $script.FullName -Segment 'obj') { continue }
    $text = Get-Content -Path $script.FullName -Raw
    if ($text -match '\$[A-Za-z_][A-Za-z0-9_]*:') {
        throw ('Unsafe PowerShell interpolation pattern found in {0}' -f $script.FullName)
    }
    if ($text -match '@\(\s*@\(') {
        throw ('Nested array literal pattern found in {0}' -f $script.FullName)
    }
    if ($text.Contains([string][char]9)) {
        throw ('Tab character found in script: {0}' -f $script.FullName)
    }
    if ($text.Contains([string][char]7)) {
        throw ('Alert character found in script: {0}' -f $script.FullName)
    }
}

$report = Get-Content -Path $reportPath -Raw
if ($report -notmatch '# P10\.2AT Admin Web Canonical Import Sweep Report') {
    throw ('P10.2AT report header was not found in {0}' -f $reportPath)
}

$packageMentionCount = 0
$featureFiles = Get-ChildItem -Path ([System.IO.Path]::Combine($adminSrc, 'features')) -Recurse -File -Include '*.ts','*.tsx' -ErrorAction SilentlyContinue
foreach ($file in $featureFiles) {
    $content = Get-Content -Path $file.FullName -Raw
    if ($content -match 'apps/migration-admin-ui') {
        throw ('Canonical feature file references apps/migration-admin-ui: {0}' -f $file.FullName)
    }
    if ($content -match '\.\./\.\./\.\./\.\./\.\./\.\./') {
        $packageMentionCount = $packageMentionCount + 1
    }
}
if ($packageMentionCount -gt 0) {
    Write-Host ('Warning: very deep relative imports detected: {0}' -f $packageMentionCount)
}

Write-Host 'P10.2AT validation passed.'
