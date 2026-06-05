Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$repoRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($scriptRoot, '..', '..', '..'))
$sourceRoot = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$appPath = [System.IO.Path]::Combine($sourceRoot, 'App.tsx')
$reportPath = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10', 'P10.2BE-AdminWebAppTsxLocalImportFormatting.Report.md')

if (-not (Test-Path -Path $appPath -PathType Leaf)) {
    throw ('Required App.tsx file was not found: {0}' -f $appPath)
}
if (-not (Test-Path -Path $reportPath -PathType Leaf)) {
    throw ('Expected report was not found: {0}' -f $reportPath)
}

$content = [System.IO.File]::ReadAllText($appPath)
$content = $content.Replace("`r`n", "`n").Replace("`r", "`n")
$lines = @($content -split "`n")

if ($content.IndexOf('export default function App', [System.StringComparison]::Ordinal) -lt 0) {
    throw 'App.tsx is missing export default function App.'
}
if ($content.IndexOf('<Routes>', [System.StringComparison]::Ordinal) -lt 0) {
    throw 'App.tsx is missing the Routes container.'
}
if ($content.IndexOf('; import ', [System.StringComparison]::Ordinal) -ge 0) {
    throw 'App.tsx still has multiple import statements on one line.'
}
if ($content.IndexOf('ConnectorConfiguration""', [System.StringComparison]::Ordinal) -ge 0) {
    throw 'App.tsx still has malformed ConnectorConfiguration quote text.'
}
if ($content.IndexOf("ConnectorConfiguration''", [System.StringComparison]::Ordinal) -ge 0) {
    throw 'App.tsx still has malformed ConnectorConfiguration quote text.'
}

$importLines = New-Object System.Collections.Generic.List[string]
foreach ($line in $lines) {
    $trimmed = $line.Trim()
    if ($trimmed.StartsWith('import ', [System.StringComparison]::Ordinal)) {
        if (-not $trimmed.EndsWith(';', [System.StringComparison]::Ordinal)) {
            throw ('Import line does not end with a semicolon: {0}' -f $trimmed)
        }
        $importLines.Add($trimmed) | Out-Null
    }
}

if ($importLines.Count -eq 0) {
    throw 'App.tsx has no import lines after formatting.'
}

$seen = New-Object 'System.Collections.Generic.HashSet[string]'
foreach ($importLine in $importLines) {
    if (-not $seen.Add($importLine)) {
        throw ('Duplicate App.tsx import line found: {0}' -f $importLine)
    }
}

$navigateImport = 'import { Navigate, Route, Routes } from "react-router-dom";'
if ($importLines.Contains($navigateImport)) {
    $bodyAfterImports = $content.Substring($content.IndexOf('export default function App', [System.StringComparison]::Ordinal))
    if (($bodyAfterImports.IndexOf('Navigate', [System.StringComparison]::Ordinal) -lt 0) -and ($bodyAfterImports.IndexOf('<Navigate', [System.StringComparison]::Ordinal) -lt 0)) {
        throw 'Navigate is still imported but is not used in the App body.'
    }
}

Write-Host 'P10.2BE Admin Web App.tsx local import formatting validation passed.'
