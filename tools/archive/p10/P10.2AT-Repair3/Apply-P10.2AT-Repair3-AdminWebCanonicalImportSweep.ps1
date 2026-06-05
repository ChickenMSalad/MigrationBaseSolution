Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($scriptRoot, '..', '..', '..'))
$adminSrc = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$reportDir = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10')
$reportPath = [System.IO.Path]::Combine($reportDir, 'P10.2AT-Repair3-CanonicalImportSweepReport.md')

if (-not (Test-Path -Path $adminSrc -PathType Container)) {
    throw ('Canonical Admin Web src folder was not found: {0}' -f $adminSrc)
}

if (-not (Test-Path -Path $reportDir -PathType Container)) {
    New-Item -Path $reportDir -ItemType Directory -Force | Out-Null
}

$reportLines = New-Object 'System.Collections.Generic.List[string]'
[void]$reportLines.Add('# P10.2AT Repair3 - Canonical Import Sweep Report')
[void]$reportLines.Add('')
[void]$reportLines.Add(('Generated: {0:yyyy-MM-dd HH:mm:ss}' -f (Get-Date)))
[void]$reportLines.Add('')
[void]$reportLines.Add('This report is intentionally read-only. No source files were moved or rewritten.')
[void]$reportLines.Add('')

$sourceFiles = @(
    Get-ChildItem -Path $adminSrc -Recurse -File -Include '*.ts','*.tsx' |
        Where-Object {
            $fullName = $_.FullName
            $segments = @($fullName.Split([System.IO.Path]::DirectorySeparatorChar, [System.StringSplitOptions]::RemoveEmptyEntries))
            -not ($segments -contains 'bin') -and
            -not ($segments -contains 'obj') -and
            -not ($segments -contains 'node_modules') -and
            -not ($segments -contains 'dist')
        }
)

$unresolvedRows = New-Object 'System.Collections.Generic.List[string]'
$checkedImportCount = 0
$relativeImportCount = 0
$importPattern = 'from\s+[''\"]([^''\"]+)[''\"]|import\s*\(\s*[''\"]([^''\"]+)[''\"]\s*\)'
$extensions = @('.ts', '.tsx', '.js', '.jsx', '.json')

foreach ($sourceFile in $sourceFiles) {
    $content = [System.IO.File]::ReadAllText($sourceFile.FullName)
    $matches = @([regex]::Matches($content, $importPattern))
    if ($matches.Length -eq 0) {
        continue
    }

    foreach ($match in $matches) {
        $importSource = $null
        if ($match.Groups[1].Success) {
            $importSource = $match.Groups[1].Value
        }
        elseif ($match.Groups[2].Success) {
            $importSource = $match.Groups[2].Value
        }

        if ([System.String]::IsNullOrWhiteSpace($importSource)) {
            continue
        }

        $checkedImportCount++

        if (-not ($importSource.StartsWith('./') -or $importSource.StartsWith('../'))) {
            continue
        }

        $relativeImportCount++
        $sourceDirectory = Split-Path -Parent $sourceFile.FullName
        $candidateBase = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($sourceDirectory, $importSource.Replace('/', [System.IO.Path]::DirectorySeparatorChar)))

        $resolved = $false
        if (Test-Path -Path $candidateBase -PathType Leaf) {
            $resolved = $true
        }
        elseif (Test-Path -Path $candidateBase -PathType Container) {
            $resolved = $true
        }
        else {
            foreach ($extension in $extensions) {
                $candidateFile = $candidateBase + $extension
                if (Test-Path -Path $candidateFile -PathType Leaf) {
                    $resolved = $true
                    break
                }
            }
        }

        if (-not $resolved) {
            foreach ($extension in $extensions) {
                $indexCandidate = [System.IO.Path]::Combine($candidateBase, 'index' + $extension)
                if (Test-Path -Path $indexCandidate -PathType Leaf) {
                    $resolved = $true
                    break
                }
            }
        }

        if (-not $resolved) {
            $relativeFile = $sourceFile.FullName.Substring($adminSrc.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar)
            $relativeFile = $relativeFile.Replace([System.IO.Path]::DirectorySeparatorChar, '/')
            [void]$unresolvedRows.Add(('| `{0}` | `{1}` |' -f $relativeFile, $importSource))
        }
    }
}

[void]$reportLines.Add(('Scanned source files: {0}' -f $sourceFiles.Length))
[void]$reportLines.Add(('Checked import statements: {0}' -f $checkedImportCount))
[void]$reportLines.Add(('Checked relative import statements: {0}' -f $relativeImportCount))
[void]$reportLines.Add(('Unresolved relative import statements: {0}' -f $unresolvedRows.Count))
[void]$reportLines.Add('')

if ($unresolvedRows.Count -eq 0) {
    [void]$reportLines.Add('No unresolved relative imports were detected by this sweep.')
}
else {
    [void]$reportLines.Add('| File | Import source |')
    [void]$reportLines.Add('| --- | --- |')
    foreach ($row in $unresolvedRows) {
        [void]$reportLines.Add($row)
    }
}

[System.IO.File]::WriteAllLines($reportPath, [string[]]$reportLines, [System.Text.Encoding]::UTF8)
Write-Host ('P10.2AT Repair3 import sweep report written: {0}' -f $reportPath)
