[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string] $RepoRoot,

    [Parameter(Mandatory = $false)]
    [string] $OutputPath
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-ScriptRootPath {
    $root = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($root)) {
        if (-not [string]::IsNullOrWhiteSpace($PSCommandPath)) {
            $root = Split-Path -Parent $PSCommandPath
        }
    }

    if ([string]::IsNullOrWhiteSpace($root)) {
        throw 'Unable to resolve script root.'
    }

    return $root
}

function Test-PathHasSegment {
    param(
        [Parameter(Mandatory = $true)]
        [string] $PathValue,

        [Parameter(Mandatory = $true)]
        [string] $Segment
    )

    $parts = @($PathValue -split '[\\/]')
    foreach ($part in $parts) {
        if ([string]::Equals($part, $Segment, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    return $false
}

function Test-PathContainsFragment {
    param(
        [Parameter(Mandatory = $true)]
        [string] $PathValue,

        [Parameter(Mandatory = $true)]
        [string] $Fragment
    )

    return ($PathValue.IndexOf($Fragment, [System.StringComparison]::OrdinalIgnoreCase) -ge 0)
}

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $scriptRoot = Get-ScriptRootPath
    $RepoRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)
}

$repoFullPath = (Resolve-Path -LiteralPath $RepoRoot).Path

$activeRoots = @(
    'tools\runtime',
    'config-samples',
    'database\sql\p7',
    'profiles\jobs'
)

$extensions = @('*.ps1', '*.json', '*.sql')
$files = @()

foreach ($activeRoot in $activeRoots) {
    $rootPath = [System.IO.Path]::Combine($repoFullPath, $activeRoot)
    if (-not (Test-Path -LiteralPath $rootPath)) {
        continue
    }

    foreach ($extension in $extensions) {
        $files += Get-ChildItem -LiteralPath $rootPath -Recurse -File -Filter $extension
    }
}

$files = $files |
    Where-Object {
        $fullName = $_.FullName
        if (Test-PathHasSegment -PathValue $fullName -Segment 'bin') { return $false }
        if (Test-PathHasSegment -PathValue $fullName -Segment 'obj') { return $false }
        if (Test-PathHasSegment -PathValue $fullName -Segment '.git') { return $false }
        if (Test-PathContainsFragment -PathValue $fullName -Fragment '.migration-control-plane') { return $false }
        if (Test-PathContainsFragment -PathValue $fullName -Fragment 'Migration.Admin.Api\Runtime') { return $false }
        if ($_.Name -like 'validate-p7.10a-*') { return $false }
        if ($_.Name -eq 'Test-RuntimeSmokePathReferences.ps1') { return $false }

        $lowerName = $_.Name.ToLowerInvariant()
        $lowerPath = $fullName.ToLowerInvariant()
        if ($lowerName.Contains('smoke')) { return $true }
        if ($lowerPath.Contains('runtime-smoke')) { return $true }
        if ($lowerPath.Contains('noop-smoke')) { return $true }

        return $false
    } |
    Sort-Object FullName -Unique

$findings = @()

foreach ($file in $files) {
    $text = Get-Content -LiteralPath $file.FullName -Raw
    $relativePath = $file.FullName.Substring($repoFullPath.Length).TrimStart('\', '/')
    $issues = @()

    if ($text.IndexOf('"manifestType":"Csv"', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $text.IndexOf('"manifestType": "Csv"', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        $issues += 'active smoke manifestType uses Csv'
    }

    $looseSmokeFileName = 'smoke' + '.json'
    if ($text.IndexOf($looseSmokeFileName, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        $issues += 'active smoke references loose smoke.json'
    }

    if (@($issues).Count -gt 0) {
        $findings += [pscustomobject]@{
            Path = $relativePath
            Issues = ($issues -join '; ')
        }
    }
}

if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    $outputFullPath = $OutputPath
    if (-not [System.IO.Path]::IsPathRooted($outputFullPath)) {
        $outputFullPath = [System.IO.Path]::Combine($repoFullPath, $OutputPath)
    }

    $outputParent = Split-Path -Parent $outputFullPath
    if (-not [string]::IsNullOrWhiteSpace($outputParent) -and -not (Test-Path -LiteralPath $outputParent)) {
        New-Item -ItemType Directory -Path $outputParent -Force | Out-Null
    }

    $findings | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $outputFullPath -Encoding UTF8
}

if (@($findings).Count -gt 0) {
    $lines = @('Deprecated active smoke references found:')
    foreach ($finding in $findings) {
        $lines += ('{0}: {1}' -f $finding.Path, $finding.Issues)
    }

    throw ($lines -join [Environment]::NewLine)
}

Write-Host ('Runtime smoke path reference validation passed. Files checked: {0}' -f @($files).Count)
