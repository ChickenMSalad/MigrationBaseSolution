$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path ".").Path
$operationalEndpointRoot = Join-Path $repoRoot "src\Migration.Admin.Api\Endpoints\Operational"

if (-not (Test-Path $operationalEndpointRoot)) {
    throw "Could not find $operationalEndpointRoot"
}

Write-Host "Operational endpoint route inventory from source:"
Write-Host ""

Get-ChildItem -Path $operationalEndpointRoot -Recurse -File -Filter "*.cs" |
    Sort-Object FullName |
    ForEach-Object {
        $relative = Resolve-Path -Path $_.FullName -Relative
        $content = Get-Content $_.FullName -Raw
        $matches = [regex]::Matches(
            $content,
            'Map(Get|Post|Put|Delete|Patch)\(\s*"([^"]+)"')

        if ($matches.Count -gt 0) {
            Write-Host $relative

            foreach ($match in $matches) {
                $method = $match.Groups[1].Value.ToUpperInvariant()
                $route = $match.Groups[2].Value
                Write-Host "  $method $route"
            }

            Write-Host ""
        }
    }
