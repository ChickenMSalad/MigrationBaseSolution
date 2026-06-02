Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$adminWebSrc = Join-Path $adminWebRoot 'src'
$docsRoot = Join-Path $repoRoot 'docs\P10'
$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.2CV'
$reportPath = Join-Path $docsRoot 'P10.2CV-AdminWebBuilderBackendContractReconciliation.md'
$detailsPath = Join-Path $artifactRoot 'builder-backend-contract-reconciliation.details.csv'

if (!(Test-Path $adminWebRoot)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}

New-Item -ItemType Directory -Force -Path $docsRoot | Out-Null
New-Item -ItemType Directory -Force -Path $artifactRoot | Out-Null

$terms = New-Object 'System.Collections.Generic.List[string]'
[void]$terms.Add('manifest')
[void]$terms.Add('manifest-builder')
[void]$terms.Add('taxonomy')
[void]$terms.Add('taxonomy-builder')
[void]$terms.Add('mapping')
[void]$terms.Add('mapping-builder')
[void]$terms.Add('mapping-profiles')

$detailRows = New-Object 'System.Collections.Generic.List[string]'
[void]$detailRows.Add('Category,Term,RelativePath,LineNumber,Text')

$uiHits = New-Object 'System.Collections.Generic.List[string]'
$backendHits = New-Object 'System.Collections.Generic.List[string]'
$adminApiCandidates = New-Object 'System.Collections.Generic.List[string]'

$sourceRoots = @(
    (Join-Path $repoRoot 'Admin'),
    (Join-Path $repoRoot 'src')
)

foreach ($candidateRoot in $sourceRoots) {
    if (!(Test-Path $candidateRoot)) { continue }
    $candidateFiles = Get-ChildItem -Path $candidateRoot -Recurse -File -Filter '*.cs' -ErrorAction SilentlyContinue |
        Where-Object {
            $_.FullName -notmatch '\\bin\\' -and
            $_.FullName -notmatch '\\obj\\' -and
            $_.FullName -notmatch '\\node_modules\\' -and
            $_.FullName -notmatch '\\reference\\'
        }
    foreach ($file in $candidateFiles) {
        $relative = $file.FullName.Substring($repoRoot.Length).TrimStart('\')
        $pathLower = $relative.ToLowerInvariant()
        if ($pathLower.Contains('admin') -or $pathLower.Contains('api') -or $pathLower.Contains('host')) {
            [void]$adminApiCandidates.Add($file.FullName)
        }
    }
}

$webFiles = Get-ChildItem -Path $adminWebSrc -Recurse -File -Include '*.ts','*.tsx' -ErrorAction SilentlyContinue |
    Where-Object {
        $_.FullName -notmatch '\\node_modules\\' -and
        $_.FullName -notmatch '\\dist\\' -and
        $_.FullName -notmatch '\\reference\\'
    }

foreach ($file in $webFiles) {
    $relative = $file.FullName.Substring($repoRoot.Length).TrimStart('\')
    $lines = Get-Content -Path $file.FullName
    $lineNumber = 0
    foreach ($line in $lines) {
        $lineNumber = $lineNumber + 1
        if ($null -eq $line) { continue }
        $lower = $line.ToLowerInvariant()
        if ($lower.Contains('/api/') -or $lower.Contains('adminapiclient') -or $lower.Contains('apiget') -or $lower.Contains('apipost')) {
            foreach ($term in $terms) {
                if ($lower.Contains($term)) {
                    $text = $line.Replace('"','""').Trim()
                    [void]$detailRows.Add(('UI,{0},"{1}",{2},"{3}"' -f $term, $relative, $lineNumber, $text))
                    [void]$uiHits.Add(('{0}:{1}' -f $relative, $lineNumber))
                    break
                }
            }
        }
    }
}

foreach ($filePath in $adminApiCandidates) {
    $relative = $filePath.Substring($repoRoot.Length).TrimStart('\')
    $lines = Get-Content -Path $filePath
    $lineNumber = 0
    foreach ($line in $lines) {
        $lineNumber = $lineNumber + 1
        if ($null -eq $line) { continue }
        $lower = $line.ToLowerInvariant()
        $looksLikeRoute = $lower.Contains('mapget') -or $lower.Contains('mappost') -or $lower.Contains('mapput') -or $lower.Contains('mapdelete') -or $lower.Contains('mapmethods') -or $lower.Contains('[http') -or $lower.Contains('[route') -or $lower.Contains('/api/')
        if (!$looksLikeRoute) { continue }
        foreach ($term in $terms) {
            if ($lower.Contains($term)) {
                $text = $line.Replace('"','""').Trim()
                [void]$detailRows.Add(('Backend,{0},"{1}",{2},"{3}"' -f $term, $relative, $lineNumber, $text))
                [void]$backendHits.Add(('{0}:{1}' -f $relative, $lineNumber))
                break
            }
        }
    }
}

Set-Content -Path $detailsPath -Value $detailRows.ToArray() -Encoding UTF8

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2CV - Admin Web Builder Backend Contract Reconciliation')
[void]$report.Add('')
[void]$report.Add('This report reconciles the restored Admin Web builder pages with local Admin API route definitions without guessing or adding fake endpoints.')
[void]$report.Add('')
[void]$report.Add(('Admin Web root: `{0}`' -f $adminWebRoot))
[void]$report.Add(('Details CSV: `{0}`' -f $detailsPath))
[void]$report.Add('')
[void]$report.Add('## Summary')
[void]$report.Add('')
[void]$report.Add(('- UI builder-related API/client hits: `{0}`' -f $uiHits.Count))
[void]$report.Add(('- Backend builder-related route hits: `{0}`' -f $backendHits.Count))
[void]$report.Add(('- Admin/backend C# files scanned: `{0}`' -f $adminApiCandidates.Count))
[void]$report.Add('')
[void]$report.Add('## Builder areas checked')
[void]$report.Add('')
[void]$report.Add('- Manifest Builder')
[void]$report.Add('- Taxonomy Builder')
[void]$report.Add('- Mapping Builder')
[void]$report.Add('- Mapping Profiles')
[void]$report.Add('')
[void]$report.Add('## Interpretation')
[void]$report.Add('')
if ($backendHits.Count -eq 0) {
    [void]$report.Add('No builder-specific backend route definitions were found by this local scan. The next set should not add frontend routes; it should add or restore backend endpoints from real services/controllers only.')
} else {
    [void]$report.Add('Backend route candidates were found. The next set can add method-aware smoke coverage and only then add aliases if a real route exists under a different URL.')
}
[void]$report.Add('')
[void]$report.Add('## Next action')
[void]$report.Add('')
[void]$report.Add('Use the details CSV to decide whether each 404 from the builder smoke is a missing backend implementation or a UI/API route alias mismatch. Do not create placeholder endpoints that return fake success.')
[void]$report.Add('')
[void]$report.Add('## Safety notes')
[void]$report.Add('')
[void]$report.Add('- This set does not modify Admin Web source.')
[void]$report.Add('- This set does not modify Admin API source.')
[void]$report.Add('- It only writes documentation and artifact evidence.')

Set-Content -Path $reportPath -Value $report.ToArray() -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host ('Wrote details: {0}' -f $detailsPath)
Write-Host 'P10.2CV Admin Web builder/backend contract reconciliation applied.'
