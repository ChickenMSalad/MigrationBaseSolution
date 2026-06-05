Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$repoRoot = $repoRoot.Path
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$sourceRoot = Join-Path $adminWebRoot 'src'

if (-not (Test-Path -LiteralPath $sourceRoot)) {
    throw ('Admin Web source root was not found: {0}' -f $sourceRoot)
}

$outDir = Join-Path $repoRoot 'artifacts\p10\P10.2CL'
if (-not (Test-Path -LiteralPath $outDir)) {
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null
}
$outPath = Join-Path $outDir 'admin-web-endpoint-contract-inventory.md'

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2CL - Admin Web Endpoint Contract Inventory')
[void]$report.Add('')
[void]$report.Add(('Generated: {0:yyyy-MM-dd HH:mm:ss} local' -f (Get-Date)))
[void]$report.Add('')
[void]$report.Add(('Admin Web root: `{0}`' -f $adminWebRoot))
[void]$report.Add('')

$apiFiles = New-Object 'System.Collections.Generic.List[System.IO.FileInfo]'
$roots = @(
    (Join-Path $sourceRoot 'api'),
    (Join-Path $sourceRoot 'features')
)
foreach ($root in $roots) {
    if (Test-Path -LiteralPath $root) {
        $found = @(Get-ChildItem -LiteralPath $root -Recurse -File -Include '*.ts' | Where-Object {
            $_.FullName -notmatch '\\reference\\' -and
            $_.FullName -notmatch '\\node_modules\\' -and
            $_.FullName -notmatch '\\dist\\' -and
            $_.FullName -notmatch '\\bin\\' -and
            $_.FullName -notmatch '\\obj\\'
        })
        foreach ($item in $found) {
            [void]$apiFiles.Add($item)
        }
    }
}

[void]$report.Add(('API files scanned: {0}' -f $apiFiles.Count))
[void]$report.Add('')
[void]$report.Add('## Endpoint-like string references')
[void]$report.Add('')

$endpointRows = New-Object 'System.Collections.Generic.List[string]'
foreach ($file in $apiFiles) {
    $relative = $file.FullName.Substring($repoRoot.Length).TrimStart('\')
    $lines = @(Get-Content -LiteralPath $file.FullName)
    $lineNumber = 0
    foreach ($line in $lines) {
        $lineNumber++
        if ($null -eq $line) { continue }
        $text = [string]$line
        if ($text -match '(/[A-Za-z0-9_./{}?=&:-]+)') {
            $matches = @([regex]::Matches($text, '(/[A-Za-z0-9_./{}?=&:-]+)'))
            foreach ($match in $matches) {
                $value = $match.Groups[1].Value
                if ([string]::IsNullOrWhiteSpace($value)) { continue }
                if ($value -eq '/') { continue }
                if ($value.StartsWith('//')) { continue }
                if ($value.Contains('../')) { continue }
                if ($value.Contains('./')) { continue }
                [void]$endpointRows.Add(('| `{0}` | {1} | `{2}` |' -f $relative, $lineNumber, $value))
            }
        }
    }
}

if ($endpointRows.Count -eq 0) {
    [void]$report.Add('No endpoint-like string references were found.')
} else {
    [void]$report.Add('| File | Line | Endpoint-like string |')
    [void]$report.Add('| --- | ---: | --- |')
    foreach ($row in $endpointRows) {
        [void]$report.Add($row)
    }
}

[void]$report.Add('')
[void]$report.Add('## Follow-up use')
[void]$report.Add('')
[void]$report.Add('- Use this inventory to compare Admin Web calls against Admin API routes.')
[void]$report.Add('- Treat this as evidence for site-up connectivity work, not as a generated API contract.')
[void]$report.Add('- This runner is intentionally read-only.')

Set-Content -LiteralPath $outPath -Value $report -Encoding UTF8
Write-Host ('Wrote endpoint contract inventory: {0}' -f $outPath)
