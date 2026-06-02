Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $current = $PSScriptRoot
    while ($true) {
        if ([string]::IsNullOrWhiteSpace($current)) {
            throw 'Unable to locate repository root from script location.'
        }

        $solution = Join-Path $current 'MigrationBaseSolution.sln'
        if (Test-Path -LiteralPath $solution) {
            return $current
        }

        $parent = Split-Path -Parent $current
        if ($parent -eq $current) {
            throw 'Unable to locate repository root containing MigrationBaseSolution.sln.'
        }

        $current = $parent
    }
}

function Test-IgnoredPath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) { return $true }
    if ($Path -match '\\bin\\') { return $true }
    if ($Path -match '\\obj\\') { return $true }
    if ($Path -match '\\node_modules\\') { return $true }
    if ($Path -match '\\.git\\') { return $true }
    if ($Path -match '\\dist\\') { return $true }
    if ($Path -match '\\tools\\p10\\P10\.2CX') { return $true }
    return $false
}

function Get-RelativePathSafe {
    param(
        [string]$Root,
        [string]$Path
    )

    $rootFull = [System.IO.Path]::GetFullPath($Root).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $pathFull = [System.IO.Path]::GetFullPath($Path)
    if ($pathFull.StartsWith($rootFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $pathFull.Substring($rootFull.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    }

    return $pathFull
}

function Read-TextFileSafe {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return ''
    }

    return [System.IO.File]::ReadAllText($Path)
}

function Get-NamespaceFromContent {
    param([string]$Content)

    $lines = $Content -split "`r?`n"
    foreach ($line in $lines) {
        $trimmed = $line.Trim()
        if ($trimmed.StartsWith('namespace ') -and $trimmed.EndsWith(';')) {
            return $trimmed.Substring(10).TrimEnd(';').Trim()
        }
        if ($trimmed.StartsWith('namespace ') -and $trimmed.Contains('{')) {
            $withoutPrefix = $trimmed.Substring(10)
            $braceIndex = $withoutPrefix.IndexOf('{')
            if ($braceIndex -ge 0) {
                return $withoutPrefix.Substring(0, $braceIndex).Trim()
            }
        }
    }

    return ''
}

function Find-EndpointFile {
    param(
        [string]$RepoRoot,
        [string]$ClassName,
        [string]$MethodName
    )

    $results = New-Object 'System.Collections.Generic.List[object]'
    $files = Get-ChildItem -LiteralPath $RepoRoot -Recurse -File -Filter '*.cs' | Where-Object { -not (Test-IgnoredPath -Path $_.FullName) }
    foreach ($file in $files) {
        $content = Read-TextFileSafe -Path $file.FullName
        if ($content.Contains($ClassName) -and $content.Contains($MethodName)) {
            $item = [pscustomobject]@{
                Path = $file.FullName
                Namespace = Get-NamespaceFromContent -Content $content
                ClassName = $ClassName
                MethodName = $MethodName
            }
            [void]$results.Add($item)
        }
    }

    return $results
}

function Find-AdminApiProgram {
    param([string]$RepoRoot)

    $results = New-Object 'System.Collections.Generic.List[object]'
    $programFiles = Get-ChildItem -LiteralPath $RepoRoot -Recurse -File -Filter 'Program.cs' | Where-Object { -not (Test-IgnoredPath -Path $_.FullName) }
    foreach ($program in $programFiles) {
        $content = Read-TextFileSafe -Path $program.FullName
        if (-not $content.Contains('WebApplication')) { continue }
        if (-not $content.Contains('app.Run')) { continue }

        $score = 0
        if ($program.FullName -match '\\Admin\\') { $score += 4 }
        if ($program.FullName -match '\\Api\\') { $score += 4 }
        if ($program.FullName -match '\\Hosts\\') { $score += 2 }
        if ($content.Contains('MapProjects')) { $score += 3 }
        if ($content.Contains('MapRuns')) { $score += 3 }
        if ($content.Contains('MapArtifacts')) { $score += 3 }
        if ($content.Contains('/api/')) { $score += 2 }

        $item = [pscustomobject]@{
            Path = $program.FullName
            Score = $score
        }
        [void]$results.Add($item)
    }

    return $results
}

function Add-UsingIfNeeded {
    param(
        [string]$Content,
        [string]$Namespace
    )

    if ([string]::IsNullOrWhiteSpace($Namespace)) { return $Content }
    $usingLine = 'using {0};' -f $Namespace
    if ($Content.Contains($usingLine)) { return $Content }

    $lines = New-Object 'System.Collections.Generic.List[string]'
    $sourceLines = $Content -split "`r?`n", -1
    $inserted = $false
    $lastUsingIndex = -1
    for ($index = 0; $index -lt $sourceLines.Length; $index++) {
        if ($sourceLines[$index].TrimStart().StartsWith('using ')) {
            $lastUsingIndex = $index
        }
    }

    for ($index = 0; $index -lt $sourceLines.Length; $index++) {
        [void]$lines.Add($sourceLines[$index])
        if ($index -eq $lastUsingIndex) {
            [void]$lines.Add($usingLine)
            $inserted = $true
        }
    }

    if (-not $inserted) {
        $lines.Insert(0, $usingLine)
    }

    return [string]::Join([Environment]::NewLine, $lines.ToArray())
}

function Add-EndpointCallBeforeRun {
    param(
        [string]$Content,
        [string]$MethodName
    )

    $call = 'app.{0}();' -f $MethodName
    if ($Content.Contains($call)) { return $Content }

    $lines = New-Object 'System.Collections.Generic.List[string]'
    $sourceLines = $Content -split "`r?`n", -1
    $inserted = $false
    foreach ($line in $sourceLines) {
        if (-not $inserted -and $line.Trim() -eq 'app.Run();') {
            [void]$lines.Add('')
            [void]$lines.Add($call)
            $inserted = $true
        }
        [void]$lines.Add($line)
    }

    if (-not $inserted) {
        throw ('Unable to insert {0}; app.Run(); was not found.' -f $call)
    }

    return [string]::Join([Environment]::NewLine, $lines.ToArray())
}

$repoRoot = Get-RepoRoot
$docsRoot = Join-Path $repoRoot 'docs\P10'
if (-not (Test-Path -LiteralPath $docsRoot)) {
    New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
}

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2CX Repair - Admin Web Builder Backend Endpoint Registration')
[void]$report.Add('')
[void]$report.Add(('Repository root: `{0}`' -f $repoRoot))
[void]$report.Add('')
[void]$report.Add('## Discovery')
[void]$report.Add('')

$endpointSpecs = New-Object 'System.Collections.Generic.List[object]'
[void]$endpointSpecs.Add([pscustomobject]@{ Label = 'Taxonomy Builder'; ClassName = 'TaxonomyBuilderEndpoints'; MethodName = 'MapTaxonomyBuilderEndpoints' })
[void]$endpointSpecs.Add([pscustomobject]@{ Label = 'Mapping Builder'; ClassName = 'MappingBuilderEndpoints'; MethodName = 'MapMappingBuilderEndpoints' })

$foundEndpoints = New-Object 'System.Collections.Generic.List[object]'
foreach ($spec in $endpointSpecs) {
    $matches = Find-EndpointFile -RepoRoot $repoRoot -ClassName $spec.ClassName -MethodName $spec.MethodName
    [void]$report.Add(('### {0}' -f $spec.Label))
    if ($matches.Count -eq 0) {
        [void]$report.Add(('No `{0}` class with `{1}` method was found. No registration added.' -f $spec.ClassName, $spec.MethodName))
        [void]$report.Add('')
        continue
    }
    if ($matches.Count -gt 1) {
        [void]$report.Add(('Multiple `{0}` candidates were found. No registration added.' -f $spec.ClassName))
        foreach ($match in $matches) {
            [void]$report.Add(('- `{0}`' -f (Get-RelativePathSafe -Root $repoRoot -Path $match.Path)))
        }
        [void]$report.Add('')
        continue
    }

    $selected = $matches[0]
    [void]$foundEndpoints.Add($selected)
    [void]$report.Add(('Selected: `{0}`' -f (Get-RelativePathSafe -Root $repoRoot -Path $selected.Path)))
    if (-not [string]::IsNullOrWhiteSpace($selected.Namespace)) {
        [void]$report.Add(('Namespace: `{0}`' -f $selected.Namespace))
    }
    [void]$report.Add('')
}

$programCandidates = Find-AdminApiProgram -RepoRoot $repoRoot
[void]$report.Add('## Admin API host discovery')
[void]$report.Add('')
foreach ($candidate in $programCandidates) {
    [void]$report.Add(('- Score {0}: `{1}`' -f $candidate.Score, (Get-RelativePathSafe -Root $repoRoot -Path $candidate.Path)))
}
[void]$report.Add('')

if ($foundEndpoints.Count -eq 0) {
    [void]$report.Add('No unambiguous builder endpoint classes were found. No Program.cs changes were made.')
} else {
    if ($programCandidates.Count -eq 0) {
        throw 'No Program.cs WebApplication host candidates were found.'
    }

    $orderedPrograms = $programCandidates | Sort-Object -Property Score -Descending
    $topScore = $orderedPrograms[0].Score
    $topPrograms = New-Object 'System.Collections.Generic.List[object]'
    foreach ($candidate in $orderedPrograms) {
        if ($candidate.Score -eq $topScore) {
            [void]$topPrograms.Add($candidate)
        }
    }

    if ($topPrograms.Count -ne 1) {
        throw ('Unable to select a unique Admin API Program.cs candidate. Top score {0} has {1} candidates.' -f $topScore, $topPrograms.Count)
    }

    $programPath = $topPrograms[0].Path
    $programContent = Read-TextFileSafe -Path $programPath
    $updatedContent = $programContent
    foreach ($endpoint in $foundEndpoints) {
        if (-not [string]::IsNullOrWhiteSpace($endpoint.Namespace)) {
            $updatedContent = Add-UsingIfNeeded -Content $updatedContent -Namespace $endpoint.Namespace
        }
        $updatedContent = Add-EndpointCallBeforeRun -Content $updatedContent -MethodName $endpoint.MethodName
    }

    if ($updatedContent -ne $programContent) {
        [System.IO.File]::WriteAllText($programPath, $updatedContent, [System.Text.UTF8Encoding]::new($false))
        [void]$report.Add(('Updated Program.cs: `{0}`' -f (Get-RelativePathSafe -Root $repoRoot -Path $programPath)))
    } else {
        [void]$report.Add(('No Program.cs update was required: `{0}`' -f (Get-RelativePathSafe -Root $repoRoot -Path $programPath)))
    }
}

$reportPath = Join-Path $docsRoot 'P10.2CX-Repair-AdminWebBuilderBackendEndpointRegistration.md'
Set-Content -LiteralPath $reportPath -Value $report.ToArray() -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2CX Repair Admin Web builder backend endpoint registration applied.'
