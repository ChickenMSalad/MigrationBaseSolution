$repoRoot = (Resolve-Path ".").Path
$operationalEndpointRoot = Join-Path $repoRoot "src\Migration.Admin.Api\Endpoints\Operational"

if (-not (Test-Path $operationalEndpointRoot)) {
    throw "Could not find $operationalEndpointRoot"
}

Write-Host "Scanning operational endpoint files for hardcoded /api route prefixes..."
Write-Host "Root: $operationalEndpointRoot"
Write-Host ""

$files = Get-ChildItem -Path $operationalEndpointRoot -Recurse -File -Filter "*.cs" |
    Sort-Object FullName

$changedFiles = New-Object System.Collections.Generic.List[string]
$totalReplacements = 0

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $original = $content

    $matches = [regex]::Matches(
        $content,
        'Map(Get|Post|Put|Delete|Patch)\(\s*"/api/operational/')

    if ($matches.Count -eq 0) {
        continue
    }

    $content = [regex]::Replace(
        $content,
        '(Map(?:Get|Post|Put|Delete|Patch)\(\s*)"/api/operational/',
        '$1"/operational/')

    if ($content -ne $original) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        $changedFiles.Add($file.FullName)
        $totalReplacements += $matches.Count

        $relativePath = Resolve-Path -Path $file.FullName -Relative
        Write-Host "Fixed $($matches.Count) route prefix(es): $relativePath"
    }
}

Write-Host ""
Write-Host "Operational route prefix repair complete."
Write-Host "Files changed: $($changedFiles.Count)"
Write-Host "Total route prefixes fixed: $totalReplacements"

Write-Host ""
Write-Host "Verifying no hardcoded /api/operational route prefixes remain under Endpoints\Operational..."

$remaining = Get-ChildItem -Path $operationalEndpointRoot -Recurse -File -Filter "*.cs" |
    Select-String -Pattern 'Map(Get|Post|Put|Delete|Patch)\(\s*"/api/operational/'

if ($remaining) {
    Write-Host ""
    Write-Host "Remaining hardcoded /api operational routes:"
    $remaining | ForEach-Object {
        Write-Host " - $($_.Path):$($_.LineNumber): $($_.Line.Trim())"
    }

    throw "Operational endpoint route prefix repair failed; hardcoded /api prefixes remain."
}

Write-Host "No hardcoded /api/operational route prefixes remain under Endpoints\Operational."
Write-Host ""
Write-Host "Next:"
Write-Host "  dotnet build"
Write-Host "  Restart Admin API"
Write-Host "  ./scripts/operational-route-prefix-source-check.ps1 -BaseUrl `"https://localhost:55436`""
Write-Host "  ./scripts/operational-p3-milestone-100-full-smoke-test.ps1 -BaseUrl `"https://localhost:55436`""
