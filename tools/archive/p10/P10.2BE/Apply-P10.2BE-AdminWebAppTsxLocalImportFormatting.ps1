Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$repoRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($scriptRoot, '..', '..', '..'))
$sourceRoot = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$appPath = [System.IO.Path]::Combine($sourceRoot, 'App.tsx')
$docsPath = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10')
$reportPath = [System.IO.Path]::Combine($docsPath, 'P10.2BE-AdminWebAppTsxLocalImportFormatting.Report.md')

if (-not (Test-Path -Path $appPath -PathType Leaf)) {
    throw ('Required App.tsx file was not found: {0}' -f $appPath)
}

if (-not (Test-Path -Path $docsPath -PathType Container)) {
    New-Item -ItemType Directory -Path $docsPath -Force | Out-Null
}

$content = [System.IO.File]::ReadAllText($appPath)
$content = $content.Replace("`r`n", "`n").Replace("`r", "`n")

$importPattern = 'import\s+[^;]+;'
$matches = @([System.Text.RegularExpressions.Regex]::Matches($content, $importPattern))
if ($matches.Length -eq 0) {
    throw ('No import statements were found in {0}' -f $appPath)
}

$lastImportEnd = -1
$imports = New-Object System.Collections.Generic.List[string]
$seen = New-Object 'System.Collections.Generic.HashSet[string]'

foreach ($match in $matches) {
    $statement = [string]$match.Value
    $statement = $statement.Trim()
    if ($statement.Length -eq 0) {
        continue
    }

    if ($statement -eq 'import { Navigate, Route, Routes } from "react-router-dom";') {
        $bodyStart = $match.Index + $match.Length
        $bodyProbe = ''
        if ($bodyStart -lt $content.Length) {
            $bodyProbe = $content.Substring($bodyStart)
        }

        if (($bodyProbe.IndexOf('Navigate', [System.StringComparison]::Ordinal) -lt 0) -and ($bodyProbe.IndexOf('<Navigate', [System.StringComparison]::Ordinal) -lt 0)) {
            $statement = 'import { Route, Routes } from "react-router-dom";'
        }
    }

    if ($seen.Add($statement)) {
        $imports.Add($statement) | Out-Null
    }

    $candidateEnd = $match.Index + $match.Length
    if ($candidateEnd -gt $lastImportEnd) {
        $lastImportEnd = $candidateEnd
    }
}

if ($imports.Count -eq 0) {
    throw ('No non-empty import statements were captured from {0}' -f $appPath)
}

$body = ''
if ($lastImportEnd -ge 0 -and $lastImportEnd -lt $content.Length) {
    $body = $content.Substring($lastImportEnd)
}
$body = $body.TrimStart("`n", " ", "`t")

$newContent = ([string]::Join("`n", $imports.ToArray())) + "`n`n" + $body.TrimEnd() + "`n"

if ($newContent -ne $content) {
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($appPath, $newContent, $utf8NoBom)
    Write-Host ('Updated App.tsx import block: {0}' -f $appPath)
} else {
    Write-Host ('No App.tsx import formatting changes were needed: {0}' -f $appPath)
}

$report = New-Object System.Collections.Generic.List[string]
$report.Add('# P10.2BE - Admin Web App.tsx Local Import Formatting Report') | Out-Null
$report.Add('') | Out-Null
$report.Add(('App.tsx: {0}' -f $appPath)) | Out-Null
$report.Add(('Captured unique imports: {0}' -f $imports.Count)) | Out-Null
$report.Add('') | Out-Null
$report.Add('## Import statements') | Out-Null
foreach ($line in $imports) {
    $report.Add(('- {0}' -f $line)) | Out-Null
}

[System.IO.File]::WriteAllLines($reportPath, $report.ToArray(), (New-Object System.Text.UTF8Encoding($false)))
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2BE Admin Web App.tsx local import formatting applied.'
