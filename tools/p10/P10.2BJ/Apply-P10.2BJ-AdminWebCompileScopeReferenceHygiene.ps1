Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$repoRoot = Resolve-Path -Path (Join-Path $scriptRoot '..\..\..')
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$sourceRoot = Join-Path $adminWebRoot 'src'
$referenceRoot = Join-Path $adminWebRoot 'reference'
$tsconfigPath = Join-Path $adminWebRoot 'tsconfig.json'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2BJ-AdminWebCompileScopeReferenceHygiene.Report.md'

if (-not (Test-Path -Path $adminWebRoot -PathType Container)) {
    throw ('Admin Web root not found: {0}' -f $adminWebRoot)
}
if (-not (Test-Path -Path $sourceRoot -PathType Container)) {
    throw ('Admin Web source root not found: {0}' -f $sourceRoot)
}
if (-not (Test-Path -Path $tsconfigPath -PathType Leaf)) {
    throw ('tsconfig.json not found: {0}' -f $tsconfigPath)
}

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2BJ - Admin Web Compile Scope Reference Hygiene')
[void]$report.Add('')
[void]$report.Add(('Admin Web root: `{0}`' -f $adminWebRoot))
[void]$report.Add(('Source root: `{0}`' -f $sourceRoot))
[void]$report.Add(('Reference root: `{0}`' -f $referenceRoot))
[void]$report.Add('')

$raw = Get-Content -Path $tsconfigPath -Raw
if ([string]::IsNullOrWhiteSpace($raw)) {
    throw ('tsconfig.json is empty: {0}' -f $tsconfigPath)
}

$tsconfig = $raw | ConvertFrom-Json

$includeValues = @()
if ($tsconfig.PSObject.Properties.Name -contains 'include') {
    $includeValues = @($tsconfig.include)
}
if ($includeValues.Length -eq 0) {
    $tsconfig | Add-Member -MemberType NoteProperty -Name 'include' -Value @('src')
    [void]$report.Add('Added missing include entry for `src`.')
} elseif (-not ($includeValues -contains 'src')) {
    $newInclude = New-Object 'System.Collections.Generic.List[string]'
    foreach ($item in $includeValues) {
        if ($null -ne $item -and -not [string]::IsNullOrWhiteSpace([string]$item)) {
            [void]$newInclude.Add([string]$item)
        }
    }
    [void]$newInclude.Add('src')
    $tsconfig.include = @($newInclude.ToArray())
    [void]$report.Add('Added `src` to existing include entries.')
} else {
    [void]$report.Add('Include already contains `src`.')
}

$requiredExcludes = @('reference', 'apps', 'node_modules', 'dist')
$currentExcludes = @()
if ($tsconfig.PSObject.Properties.Name -contains 'exclude') {
    $currentExcludes = @($tsconfig.exclude)
}
$excludeList = New-Object 'System.Collections.Generic.List[string]'
foreach ($item in $currentExcludes) {
    if ($null -ne $item -and -not [string]::IsNullOrWhiteSpace([string]$item)) {
        if (-not $excludeList.Contains([string]$item)) {
            [void]$excludeList.Add([string]$item)
        }
    }
}
foreach ($required in $requiredExcludes) {
    if (-not $excludeList.Contains($required)) {
        [void]$excludeList.Add($required)
        [void]$report.Add(('Added exclude entry `{0}`.' -f $required))
    } else {
        [void]$report.Add(('Exclude already contains `{0}`.' -f $required))
    }
}

if ($tsconfig.PSObject.Properties.Name -contains 'exclude') {
    $tsconfig.exclude = @($excludeList.ToArray())
} else {
    $tsconfig | Add-Member -MemberType NoteProperty -Name 'exclude' -Value @($excludeList.ToArray())
}

$updatedJson = $tsconfig | ConvertTo-Json -Depth 20
Set-Content -Path $tsconfigPath -Value $updatedJson -Encoding UTF8
[void]$report.Add('')
[void]$report.Add(('Updated tsconfig: `{0}`' -f $tsconfigPath))

[void]$report.Add('')
[void]$report.Add('## Source imports referencing reference material')
$sourceFiles = @(Get-ChildItem -Path $sourceRoot -Recurse -File -Include '*.ts','*.tsx')
$badImports = New-Object 'System.Collections.Generic.List[string]'
foreach ($sourceFile in $sourceFiles) {
    $text = Get-Content -Path $sourceFile.FullName -Raw
    if ($null -eq $text) { continue }
    if ($text.IndexOf('/reference/', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $text.IndexOf('..\reference\', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $text.IndexOf('../reference/', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $text.IndexOf('reference/apps-migration-admin-ui', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        [void]$badImports.Add($sourceFile.FullName)
    }
}
if ($badImports.Count -eq 0) {
    [void]$report.Add('No canonical source imports into reference material were found.')
} else {
    foreach ($badImport in $badImports) {
        [void]$report.Add(('- `{0}`' -f $badImport))
    }
}

$reportDirectory = Split-Path -Parent $reportPath
if (-not (Test-Path -Path $reportDirectory -PathType Container)) {
    New-Item -Path $reportDirectory -ItemType Directory -Force | Out-Null
}
Set-Content -Path $reportPath -Value $report.ToArray() -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2BJ Admin Web compile scope reference hygiene applied.'
