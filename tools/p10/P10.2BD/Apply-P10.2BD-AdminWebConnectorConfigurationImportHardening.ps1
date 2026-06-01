Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$repoRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($scriptRoot, '..', '..', '..'))
$sourceRoot = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$appPath = [System.IO.Path]::Combine($sourceRoot, 'App.tsx')
$docsDir = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10')
$reportPath = [System.IO.Path]::Combine($docsDir, 'P10.2BD-AdminWebConnectorConfigurationImportHardening.md')

if (-not (Test-Path -Path $appPath -PathType Leaf)) {
    throw ('Required App.tsx was not found: {0}' -f $appPath)
}

if (-not (Test-Path -Path $docsDir -PathType Container)) {
    New-Item -Path $docsDir -ItemType Directory -Force | Out-Null
}

$correctImport = 'import { ConnectorConfiguration } from "./features/connectors/configuration/pages/ConnectorConfiguration";'
$malformedImport = 'import { ConnectorConfiguration } from "./features/connectors/configuration/pages/ConnectorConfiguration"";'

$content = Get-Content -Path $appPath -Raw
$originalContent = $content
$report = New-Object System.Collections.Generic.List[string]
[void]$report.Add('# P10.2BD - Admin Web Connector Configuration Import Hardening')
[void]$report.Add('')
[void]$report.Add(('App.tsx: `{0}`' -f $appPath))
[void]$report.Add('')

if ($content.Contains($malformedImport)) {
    $content = $content.Replace($malformedImport, $correctImport)
    [void]$report.Add('- Replaced malformed `ConnectorConfiguration` import quote sequence.')
}
else {
    [void]$report.Add('- Malformed `ConnectorConfiguration` import quote sequence was not present.')
}

$parts = $content.Split([string[]]@($correctImport), [System.StringSplitOptions]::None)
$occurrences = $parts.Length - 1

if ($occurrences -gt 1) {
    $tail = ''
    if ($parts.Length -gt 1) {
        $tailParts = @()
        for ($index = 1; $index -lt $parts.Length; $index++) {
            $tailParts += $parts[$index]
        }
        $tail = ($tailParts -join '')
    }
    $content = $parts[0] + $correctImport + $tail
    [void]$report.Add(('- Removed duplicate `ConnectorConfiguration` imports. Before count: {0}; after count: 1.' -f $occurrences))
}
elseif ($occurrences -eq 1) {
    [void]$report.Add('- `ConnectorConfiguration` import already appears exactly once.')
}
else {
    if ($content.Contains('ConnectorConfiguration')) {
        $exportIndex = $content.IndexOf('export default')
        if ($exportIndex -lt 0) {
            throw 'App.tsx references ConnectorConfiguration but no export default marker was found for safe import insertion.'
        }
        $prefix = $content.Substring(0, $exportIndex)
        $suffix = $content.Substring($exportIndex)
        $content = $prefix + $correctImport + ' ' + $suffix
        [void]$report.Add('- Added missing `ConnectorConfiguration` import before `export default`.')
    }
    else {
        [void]$report.Add('- `ConnectorConfiguration` is not referenced in App.tsx; no import was added.')
    }
}

if ($content -ne $originalContent) {
    Set-Content -Path $appPath -Value $content -NoNewline -Encoding UTF8
    [void]$report.Add('- Wrote updated App.tsx.')
}
else {
    [void]$report.Add('- No App.tsx changes were required.')
}

$finalContent = Get-Content -Path $appPath -Raw
$finalParts = $finalContent.Split([string[]]@($correctImport), [System.StringSplitOptions]::None)
$finalOccurrences = $finalParts.Length - 1

[void]$report.Add('')
[void]$report.Add('## Final validation')
[void]$report.Add(('- Correct import count: {0}' -f $finalOccurrences))
[void]$report.Add(('- Malformed import present: {0}' -f $finalContent.Contains($malformedImport)))

Set-Content -Path $reportPath -Value $report -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2BD Admin Web ConnectorConfiguration import hardening applied.'
