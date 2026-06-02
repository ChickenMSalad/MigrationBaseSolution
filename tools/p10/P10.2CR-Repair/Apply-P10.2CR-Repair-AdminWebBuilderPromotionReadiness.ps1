Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $current = (Get-Location).Path
    while (-not [string]::IsNullOrWhiteSpace($current)) {
        if ((Test-Path (Join-Path $current '.git')) -or (Test-Path (Join-Path $current 'MigrationBase.sln'))) {
            return $current
        }
        $parent = Split-Path -Parent $current
        if ($parent -eq $current) { break }
        $current = $parent
    }
    if ($PSScriptRoot) {
        $fromScript = Resolve-Path (Join-Path $PSScriptRoot '..\..\..')
        return $fromScript.Path
    }
    throw 'Unable to locate repository root.'
}

function Convert-ToRepoRelativePath {
    param(
        [Parameter(Mandatory=$true)][string]$RepoRoot,
        [Parameter(Mandatory=$true)][string]$Path
    )
    $fullRoot = [System.IO.Path]::GetFullPath($RepoRoot).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if ($fullPath.StartsWith($fullRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $fullPath.Substring($fullRoot.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar).Replace('\\','/')
    }
    return $fullPath.Replace('\\','/')
}

function Add-CandidateRows {
    param(
        [Parameter(Mandatory=$true)][System.Collections.Generic.List[string]]$Rows,
        [Parameter(Mandatory=$true)][string]$RepoRoot,
        [Parameter(Mandatory=$true)][string]$RootPath,
        [Parameter(Mandatory=$true)][string]$RootLabel,
        [Parameter(Mandatory=$true)][string[]]$Tokens
    )

    if (-not (Test-Path $RootPath)) {
        [void]$Rows.Add(('| {0} | missing root | | | | |' -f $RootLabel))
        return
    }

    $files = @(Get-ChildItem -Path $RootPath -Recurse -File -Include '*.ts','*.tsx' -ErrorAction SilentlyContinue)
    foreach ($file in $files) {
        $name = $file.Name.ToLowerInvariant()
        $pathLower = $file.FullName.ToLowerInvariant()
        $matched = $false
        foreach ($token in $Tokens) {
            $tokenLower = $token.ToLowerInvariant()
            if ($name.Contains($tokenLower) -or $pathLower.Contains($tokenLower)) {
                $matched = $true
                break
            }
        }
        if (-not $matched) { continue }

        $relativePath = Convert-ToRepoRelativePath -RepoRoot $RepoRoot -Path $file.FullName
        $lineCount = 0
        try {
            $lineCount = @([System.IO.File]::ReadAllLines($file.FullName)).Length
        } catch {
            $lineCount = -1
        }

        $kind = 'source'
        if ($file.Extension -eq '.tsx') { $kind = 'page/component' }
        elseif ($file.Extension -eq '.ts') { $kind = 'api/type/module' }

        [void]$Rows.Add(('| {0} | {1} | {2} | {3} | {4} | {5} |' -f $RootLabel, $kind, $file.Name, $relativePath, $lineCount, 'candidate'))
    }
}

$repoRoot = Get-RepoRoot
$adminRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$sourceRoot = Join-Path $adminRoot 'src'
$canonicalFeatureRoot = Join-Path $sourceRoot 'features'
$referenceFeatureRoot = Join-Path $adminRoot 'reference\apps-migration-admin-ui\src\features'
$legacyAppsRoot = Join-Path $repoRoot 'apps\migration-admin-ui\src'
$legacyFeatureRoot = Join-Path $legacyAppsRoot 'features'
$docsRoot = Join-Path $repoRoot 'docs\P10'
$reportPath = Join-Path $docsRoot 'P10.2CR-Repair-AdminWebBuilderPromotionReadiness.md'
New-Item -ItemType Directory -Force -Path $docsRoot | Out-Null

$tokens = @('manifest','taxonomy','mapping','builder')
$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2CR Repair - Admin Web Builder Promotion Readiness')
[void]$report.Add('')
[void]$report.Add(('Generated UTC: `{0}`' -f ([DateTime]::UtcNow.ToString('o'))))
[void]$report.Add('')
[void]$report.Add('This repair is report-only. It does not move files, change routes, or modify Admin Web source.')
[void]$report.Add('')
[void]$report.Add('## Roots')
[void]$report.Add('')
[void]$report.Add(('- Admin Web root: `{0}`' -f (Convert-ToRepoRelativePath -RepoRoot $repoRoot -Path $adminRoot)))
[void]$report.Add(('- Canonical feature root: `{0}`' -f (Convert-ToRepoRelativePath -RepoRoot $repoRoot -Path $canonicalFeatureRoot)))
[void]$report.Add(('- Reference feature root: `{0}`' -f (Convert-ToRepoRelativePath -RepoRoot $repoRoot -Path $referenceFeatureRoot)))
[void]$report.Add(('- Legacy apps feature root: `{0}`' -f (Convert-ToRepoRelativePath -RepoRoot $repoRoot -Path $legacyFeatureRoot)))
[void]$report.Add('')
[void]$report.Add('## Builder Candidate Inventory')
[void]$report.Add('')
[void]$report.Add('| Root | Kind | File | Path | Lines | Classification |')
[void]$report.Add('| --- | --- | --- | --- | ---: | --- |')

Add-CandidateRows -Rows $report -RepoRoot $repoRoot -RootPath $canonicalFeatureRoot -RootLabel 'canonical features' -Tokens $tokens
Add-CandidateRows -Rows $report -RepoRoot $repoRoot -RootPath $referenceFeatureRoot -RootLabel 'reference features' -Tokens $tokens
Add-CandidateRows -Rows $report -RepoRoot $repoRoot -RootPath $legacyFeatureRoot -RootLabel 'legacy apps features' -Tokens $tokens

[void]$report.Add('')
[void]$report.Add('## Promotion Guidance')
[void]$report.Add('')
[void]$report.Add('- Promote only a single unambiguous builder page candidate at a time.')
[void]$report.Add('- Prefer canonical feature pages already under `src/Admin/Migration.Admin.Web/src/features`.')
[void]$report.Add('- Treat `reference/apps-migration-admin-ui` as preservation/reference material, not compiled source.')
[void]$report.Add('- Do not infer that an API/type/module file is a routable page unless there is a matching `.tsx` page/component.')
[void]$report.Add('- The next restoration set should add routes/navigation only for confirmed canonical pages, and should build after each promotion batch.')
[void]$report.Add('')

[System.IO.File]::WriteAllLines($reportPath, $report, [System.Text.UTF8Encoding]::new($false))
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2CR Repair builder promotion readiness applied.'
