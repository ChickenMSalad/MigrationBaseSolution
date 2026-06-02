param(
    [string]$BaseUrl = 'http://localhost:5000',
    [int]$TimeoutSeconds = 5
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$sourceRoot = Join-Path $adminWebRoot 'src'
$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.2CN'

if (-not (Test-Path -LiteralPath $sourceRoot -PathType Container)) {
    throw ('Admin Web source root was not found: {0}' -f $sourceRoot)
}

New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null

$normalizedBaseUrl = $BaseUrl.TrimEnd('/')
$apiFiles = @(Get-ChildItem -LiteralPath $sourceRoot -Recurse -File -Include '*.ts','*.tsx' | Where-Object { $_.FullName -notmatch '\\reference\\' } | Sort-Object FullName)
$paths = New-Object 'System.Collections.Generic.SortedSet[string]'

foreach ($file in $apiFiles) {
    $lines = @(Get-Content -LiteralPath $file.FullName)
    foreach ($line in $lines) {
        if ($null -eq $line) { continue }
        $text = [string]$line
        if ($text.IndexOf('/api/', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) { continue }
        $matches = [System.Text.RegularExpressions.Regex]::Matches($text, '[`''"](/api/[^`''"\s)]+)[`''"]')
        foreach ($match in $matches) {
            if ($match.Groups.Count -gt 1) {
                $candidate = [string]$match.Groups[1].Value
                if (-not [string]::IsNullOrWhiteSpace($candidate)) {
                    [void]$paths.Add($candidate)
                }
            }
        }
    }
}

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2CN - Admin Web Runtime API Smoke Matrix')
[void]$report.Add('')
[void]$report.Add(('Generated: {0:O}' -f (Get-Date)))
[void]$report.Add(('Base URL: `{0}`' -f $normalizedBaseUrl))
[void]$report.Add('')
[void]$report.Add('## Discovered static API paths')
[void]$report.Add('')

if ($paths.Count -eq 0) {
    [void]$report.Add('- No static `/api/` paths were discovered in Admin Web source.')
} else {
    foreach ($path in $paths) {
        [void]$report.Add(('- `{0}`' -f $path))
    }
}

[void]$report.Add('')
[void]$report.Add('## Conservative GET probes')
[void]$report.Add('')
[void]$report.Add('Only paths without obvious route parameters are probed. Write-oriented or templated paths are listed but skipped.')
[void]$report.Add('')

foreach ($path in $paths) {
    $shouldSkip = $false
    if ($path.Contains('{') -or $path.Contains(':') -or $path.Contains('$')) { $shouldSkip = $true }
    if ($path -match '/(upload|delete|create|update|validate|execute|dispatch|retry|run)($|/)') { $shouldSkip = $true }

    if ($shouldSkip) {
        [void]$report.Add(('- SKIP `{0}`' -f $path))
        continue
    }

    $url = $normalizedBaseUrl + $path
    try {
        $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec $TimeoutSeconds -Method GET
        [void]$report.Add(('- GET `{0}` => HTTP {1}' -f $path, [int]$response.StatusCode))
    } catch {
        $message = $_.Exception.Message
        [void]$report.Add(('- GET `{0}` => FAILED: {1}' -f $path, $message))
    }
}

$reportPath = Join-Path $artifactRoot 'runtime-api-smoke-matrix.md'
[System.IO.File]::WriteAllLines($reportPath, $report.ToArray(), [System.Text.Encoding]::UTF8)
Write-Host ('Wrote runtime API smoke matrix: {0}' -f $reportPath)
