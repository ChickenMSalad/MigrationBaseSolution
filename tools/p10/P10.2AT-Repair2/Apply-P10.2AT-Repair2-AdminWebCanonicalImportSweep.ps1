Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $scriptRoot = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
        $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    }

    $candidate = Resolve-Path -Path (Join-Path -Path $scriptRoot -ChildPath '..\..\..')
    return $candidate.Path
}

function Convert-ToRepoRelativePath {
    param(
        [Parameter(Mandatory = $true)][string]$RootPath,
        [Parameter(Mandatory = $true)][string]$FullPath
    )

    $rootFull = [System.IO.Path]::GetFullPath($RootPath).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $pathFull = [System.IO.Path]::GetFullPath($FullPath)

    if ($pathFull.StartsWith($rootFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        $relative = $pathFull.Substring($rootFull.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
        return ($relative -replace '\\', '/')
    }

    return ($pathFull -replace '\\', '/')
}

function Test-PathSegment {
    param(
        [Parameter(Mandatory = $true)][string]$PathValue,
        [Parameter(Mandatory = $true)][string]$Segment
    )

    $parts = @($PathValue -split '[\\/]')
    foreach ($part in $parts) {
        if ([string]::Equals($part, $Segment, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    return $false
}

function Resolve-RelativeImport {
    param(
        [Parameter(Mandatory = $true)][string]$SourceFile,
        [Parameter(Mandatory = $true)][string]$ImportSource
    )

    $sourceDirectory = Split-Path -Parent $SourceFile
    $basePath = [System.IO.Path]::GetFullPath((Join-Path -Path $sourceDirectory -ChildPath $ImportSource))

    $candidatePaths = New-Object System.Collections.Generic.List[string]
    [void]$candidatePaths.Add($basePath)
    [void]$candidatePaths.Add($basePath + '.ts')
    [void]$candidatePaths.Add($basePath + '.tsx')
    [void]$candidatePaths.Add($basePath + '.js')
    [void]$candidatePaths.Add($basePath + '.jsx')
    [void]$candidatePaths.Add((Join-Path -Path $basePath -ChildPath 'index.ts'))
    [void]$candidatePaths.Add((Join-Path -Path $basePath -ChildPath 'index.tsx'))

    foreach ($candidatePath in $candidatePaths) {
        if (Test-Path -Path $candidatePath -PathType Leaf) {
            return $true
        }
        if (Test-Path -Path $candidatePath -PathType Container) {
            return $true
        }
    }

    return $false
}

function Add-ReportLine {
    param(
        [Parameter(Mandatory = $true)][System.Collections.ArrayList]$ReportLines,
        [Parameter(Mandatory = $true)][string]$Line
    )

    [void]$ReportLines.Add($Line)
}

$repoRoot = Get-RepoRoot
$adminSrc = Join-Path -Path $repoRoot -ChildPath 'src\Admin\Migration.Admin.Web\src'
$reportPath = Join-Path -Path $repoRoot -ChildPath 'docs\P10\P10.2AT-Repair2-CanonicalImportSweepReport.md'

if (-not (Test-Path -Path $adminSrc -PathType Container)) {
    throw ('Canonical Admin Web source directory was not found: {0}' -f $adminSrc)
}

$reportLines = New-Object System.Collections.ArrayList
Add-ReportLine -ReportLines $reportLines -Line '# P10.2AT Repair2 - Canonical Admin Web Import Sweep Report'
Add-ReportLine -ReportLines $reportLines -Line ''
Add-ReportLine -ReportLines $reportLines -Line ('Generated from: `{0}`' -f (Convert-ToRepoRelativePath -RootPath $repoRoot -FullPath $adminSrc))
Add-ReportLine -ReportLines $reportLines -Line ''
Add-ReportLine -ReportLines $reportLines -Line 'This repair is intentionally non-mutating for source files. It scans relative imports and reports unresolved local paths.'
Add-ReportLine -ReportLines $reportLines -Line ''

$sourceFiles = @(
    Get-ChildItem -Path $adminSrc -Recurse -File -Include '*.ts','*.tsx' |
        Where-Object {
            -not (Test-PathSegment -PathValue $_.FullName -Segment 'node_modules') -and
            -not (Test-PathSegment -PathValue $_.FullName -Segment 'dist') -and
            -not (Test-PathSegment -PathValue $_.FullName -Segment 'bin') -and
            -not (Test-PathSegment -PathValue $_.FullName -Segment 'obj') -and
            ($_.Name -notlike '*.d.ts')
        } |
        Sort-Object -Property FullName
)

$unresolved = New-Object System.Collections.ArrayList
$checkedImportTotal = 0
$importPattern = '(?m)^\s*import(?:\s+type)?(?:[\s\S]*?)\s+from\s+[''\"](?<source>\.[^''\"]+)[''\"]\s*;?\s*$|^\s*import\s*\(\s*[''\"](?<source>\.[^''\"]+)[''\"]\s*\)'

foreach ($sourceFile in $sourceFiles) {
    if (-not (Test-Path -Path $sourceFile.FullName -PathType Leaf)) {
        continue
    }

    $content = Get-Content -Path $sourceFile.FullName -Raw
    $importMatches = @([regex]::Matches($content, $importPattern))

    if ($importMatches.Length -eq 0) {
        continue
    }

    foreach ($importMatch in $importMatches) {
        $sourceGroup = $importMatch.Groups['source']
        if ($null -eq $sourceGroup -or -not $sourceGroup.Success) {
            continue
        }

        $importSource = [string]$sourceGroup.Value
        if ([string]::IsNullOrWhiteSpace($importSource)) {
            continue
        }

        $checkedImportTotal++
        $resolved = Resolve-RelativeImport -SourceFile $sourceFile.FullName -ImportSource $importSource
        if (-not $resolved) {
            $relativeSourceFile = Convert-ToRepoRelativePath -RootPath $repoRoot -FullPath $sourceFile.FullName
            [void]$unresolved.Add([pscustomobject]@{
                File = $relativeSourceFile
                Import = $importSource
            })
        }
    }
}

Add-ReportLine -ReportLines $reportLines -Line ('Scanned source files: {0}' -f $sourceFiles.Length)
Add-ReportLine -ReportLines $reportLines -Line ('Checked relative imports: {0}' -f $checkedImportTotal)
Add-ReportLine -ReportLines $reportLines -Line ('Unresolved relative imports: {0}' -f $unresolved.Count)
Add-ReportLine -ReportLines $reportLines -Line ''

if ($unresolved.Count -eq 0) {
    Add-ReportLine -ReportLines $reportLines -Line 'No unresolved relative imports were found by this sweep.'
} else {
    Add-ReportLine -ReportLines $reportLines -Line '## Unresolved Relative Imports'
    Add-ReportLine -ReportLines $reportLines -Line ''
    foreach ($item in $unresolved) {
        Add-ReportLine -ReportLines $reportLines -Line ('- `{0}` imports `{1}`' -f $item.File, $item.Import)
    }
}

$reportDirectory = Split-Path -Parent $reportPath
if (-not (Test-Path -Path $reportDirectory -PathType Container)) {
    New-Item -Path $reportDirectory -ItemType Directory -Force | Out-Null
}

Set-Content -Path $reportPath -Value ([string[]]$reportLines) -Encoding UTF8
Write-Host ('P10.2AT Repair2 canonical import sweep report written: {0}' -f $reportPath)
