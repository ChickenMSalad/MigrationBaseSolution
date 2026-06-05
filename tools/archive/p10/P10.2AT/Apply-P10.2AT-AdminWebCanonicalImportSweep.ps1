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

function Get-RelativePathFromDirectory {
    param(
        [Parameter(Mandatory=$true)][string]$FromDirectory,
        [Parameter(Mandatory=$true)][string]$ToFile
    )
    $fromUri = New-Object System.Uri((Join-Path -Path $FromDirectory -ChildPath ([System.IO.Path]::DirectorySeparatorChar)))
    $toUri = New-Object System.Uri($ToFile)
    $relative = $fromUri.MakeRelativeUri($toUri).ToString()
    $relative = [System.Uri]::UnescapeDataString($relative)
    $relative = $relative -replace '\\','/'
    if (-not $relative.StartsWith('.')) {
        $relative = './' + $relative
    }
    $relative = $relative -replace '\.(tsx|ts)$',''
    return $relative
}

function Resolve-ImportTarget {
    param(
        [Parameter(Mandatory=$true)][string]$ImporterPath,
        [Parameter(Mandatory=$true)][string]$Source
    )
    if (-not $Source.StartsWith('.')) { return $true }
    $importerDirectory = [System.IO.Path]::GetDirectoryName($ImporterPath)
    $combined = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($importerDirectory, ($Source -replace '/', [System.IO.Path]::DirectorySeparatorChar)))
    $candidatePaths = New-Object System.Collections.Generic.List[string]
    [void]$candidatePaths.Add($combined)
    [void]$candidatePaths.Add($combined + '.ts')
    [void]$candidatePaths.Add($combined + '.tsx')
    [void]$candidatePaths.Add([System.IO.Path]::Combine($combined, 'index.ts'))
    [void]$candidatePaths.Add([System.IO.Path]::Combine($combined, 'index.tsx'))
    foreach ($candidate in $candidatePaths) {
        if (Test-Path -Path $candidate -PathType Leaf) { return $true }
    }
    return $false
}

function Get-ImportMatches {
    param([Parameter(Mandatory=$true)][string]$Content)
    $pattern = '(?m)^(?<prefix>\s*import(?:\s+type)?\s+(?:[^''";]+?\s+from\s+)?[\''"])(?<source>\.[^\''"]+)(?<suffix>[\''"]\s*;?\s*)$'
    $regex = New-Object System.Text.RegularExpressions.Regex($pattern)
    return $regex.Matches($Content)
}

function Get-CanonicalSourceFiles {
    param([Parameter(Mandatory=$true)][string]$AdminSrc)
    $files = Get-ChildItem -Path $AdminSrc -Recurse -File -Include '*.ts','*.tsx' |
        Where-Object {
            $segments = $_.FullName.Split([System.IO.Path]::DirectorySeparatorChar)
            -not ($segments -contains 'node_modules') -and
            -not ($segments -contains 'dist') -and
            -not ($segments -contains 'build')
        }
    return @($files)
}

$repoRoot = Get-RepositoryRoot
$adminSrc = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
if (-not (Test-Path -Path $adminSrc -PathType Container)) {
    throw ('Canonical Admin Web source folder not found: {0}' -f $adminSrc)
}

$reportPath = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10', 'P10.2AT-AdminWebCanonicalImportSweep.Report.md')
$reportDirectory = [System.IO.Path]::GetDirectoryName($reportPath)
if (-not (Test-Path -Path $reportDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $reportDirectory | Out-Null
}

$allSourceFiles = Get-CanonicalSourceFiles -AdminSrc $adminSrc
$sourceByBaseName = @{}
foreach ($file in $allSourceFiles) {
    $baseName = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
    if (-not $sourceByBaseName.ContainsKey($baseName)) {
        $sourceByBaseName[$baseName] = New-Object System.Collections.Generic.List[string]
    }
    [void]$sourceByBaseName[$baseName].Add($file.FullName)
}

$updates = New-Object System.Collections.Generic.List[object]
$unresolved = New-Object System.Collections.Generic.List[object]
$ambiguous = New-Object System.Collections.Generic.List[object]

foreach ($file in $allSourceFiles) {
    $content = Get-Content -Path $file.FullName -Raw
    $matches = Get-ImportMatches -Content $content
    if ($matches.Count -eq 0) { continue }

    $newContent = $content
    foreach ($match in $matches) {
        $source = $match.Groups['source'].Value
        if (Resolve-ImportTarget -ImporterPath $file.FullName -Source $source) { continue }

        $leaf = [System.IO.Path]::GetFileName(($source -replace '/', [System.IO.Path]::DirectorySeparatorChar))
        if ([string]::IsNullOrWhiteSpace($leaf)) {
            [void]$unresolved.Add([pscustomobject]@{ File = $file.FullName; Import = $source; Reason = 'Empty import leaf' })
            continue
        }

        if (-not $sourceByBaseName.ContainsKey($leaf)) {
            [void]$unresolved.Add([pscustomobject]@{ File = $file.FullName; Import = $source; Reason = 'No canonical basename match' })
            continue
        }

        $candidates = @($sourceByBaseName[$leaf] | Where-Object { $_ -ne $file.FullName })
        if ($candidates.Count -ne 1) {
            [void]$ambiguous.Add([pscustomobject]@{ File = $file.FullName; Import = $source; CandidateCount = $candidates.Count })
            continue
        }

        $replacementSource = Get-RelativePathFromDirectory -FromDirectory ([System.IO.Path]::GetDirectoryName($file.FullName)) -ToFile $candidates[0]
        $oldLine = $match.Value
        $newLine = $match.Groups['prefix'].Value + $replacementSource + $match.Groups['suffix'].Value
        $newContent = $newContent.Replace($oldLine, $newLine)
        [void]$updates.Add([pscustomobject]@{ File = $file.FullName; From = $source; To = $replacementSource })
    }

    if ($newContent -ne $content) {
        Set-Content -Path $file.FullName -Value $newContent -NoNewline -Encoding UTF8
    }
}

$remainingBroken = New-Object System.Collections.Generic.List[object]
$allSourceFilesAfter = Get-CanonicalSourceFiles -AdminSrc $adminSrc
foreach ($file in $allSourceFilesAfter) {
    $content = Get-Content -Path $file.FullName -Raw
    $matches = Get-ImportMatches -Content $content
    foreach ($match in $matches) {
        $source = $match.Groups['source'].Value
        if (-not (Resolve-ImportTarget -ImporterPath $file.FullName -Source $source)) {
            [void]$remainingBroken.Add([pscustomobject]@{ File = $file.FullName; Import = $source })
        }
    }
}

$lines = New-Object System.Collections.Generic.List[string]
[void]$lines.Add('# P10.2AT Admin Web Canonical Import Sweep Report')
[void]$lines.Add('')
[void]$lines.Add(('Generated from local repository root: `{0}`' -f $repoRoot))
[void]$lines.Add('')
[void]$lines.Add(('Updated imports: {0}' -f $updates.Count))
[void]$lines.Add(('Ambiguous unresolved imports skipped: {0}' -f $ambiguous.Count))
[void]$lines.Add(('Unresolved imports without a unique canonical match: {0}' -f $unresolved.Count))
[void]$lines.Add(('Remaining broken relative imports after sweep: {0}' -f $remainingBroken.Count))
[void]$lines.Add('')
[void]$lines.Add('## Updated Imports')
if ($updates.Count -eq 0) {
    [void]$lines.Add('')
    [void]$lines.Add('None.')
} else {
    foreach ($item in $updates) {
        $relativeFile = $item.File.Substring($repoRoot.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar) -replace '\\','/'
        [void]$lines.Add(('- `{0}`: `{1}` -> `{2}`' -f $relativeFile, $item.From, $item.To))
    }
}
[void]$lines.Add('')
[void]$lines.Add('## Remaining Broken Relative Imports')
if ($remainingBroken.Count -eq 0) {
    [void]$lines.Add('')
    [void]$lines.Add('None detected by the local resolver.')
} else {
    foreach ($item in $remainingBroken) {
        $relativeFile = $item.File.Substring($repoRoot.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar) -replace '\\','/'
        [void]$lines.Add(('- `{0}` imports `{1}`' -f $relativeFile, $item.Import))
    }
}
[void]$lines.Add('')
[void]$lines.Add('## Ambiguous Skips')
if ($ambiguous.Count -eq 0) {
    [void]$lines.Add('')
    [void]$lines.Add('None.')
} else {
    foreach ($item in $ambiguous) {
        $relativeFile = $item.File.Substring($repoRoot.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar) -replace '\\','/'
        [void]$lines.Add(('- `{0}` imports `{1}`; candidates: {2}' -f $relativeFile, $item.Import, $item.CandidateCount))
    }
}
Set-Content -Path $reportPath -Value $lines -Encoding UTF8

Write-Host ('P10.2AT import sweep complete. Report: {0}' -f $reportPath)
if ($remainingBroken.Count -gt 0) {
    Write-Host ('Remaining broken imports were reported but not guessed: {0}' -f $remainingBroken.Count)
}
