Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..')
$programPath = Join-Path $repoRoot 'src\Core\Migration.Admin.Api\Program.cs'

if (-not (Test-Path -LiteralPath $programPath)) {
    throw "Program.cs was not found at: $programPath"
}

$content = Get-Content -LiteralPath $programPath -Raw

$mappingBlock = @'
app.MapManifestBuilderEndpoints();
app.MapMappingBuilderEndpoints();
app.MapTaxonomyBuilderEndpoints();
'@

$changed = $false

if ($content -notmatch 'MapManifestBuilderEndpoints\s*\(') {
    $anchor = 'app.MapMigrationOperationalEndpoints();'
    if ($content.Contains($anchor)) {
        $content = $content.Replace($anchor, $anchor + [Environment]::NewLine + $mappingBlock.TrimEnd())
        $changed = $true
    }
    elseif ($content.Contains('app.Run();')) {
        $content = $content.Replace('app.Run();', $mappingBlock.TrimEnd() + [Environment]::NewLine + 'app.Run();')
        $changed = $true
    }
    else {
        throw 'Could not find app.MapMigrationOperationalEndpoints(); or app.Run(); anchor in Program.cs.'
    }
}

if ($changed) {
    Set-Content -LiteralPath $programPath -Value $content -Encoding UTF8
    Write-Host 'Updated Program.cs with Manifest/Mapping/Taxonomy builder endpoint mappings.'
}
else {
    Write-Host 'Program.cs already contains builder endpoint mappings; no change made.'
}
