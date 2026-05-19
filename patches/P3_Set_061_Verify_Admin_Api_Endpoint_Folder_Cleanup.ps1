$repoRoot = (Resolve-Path ".").Path
$endpointRoot = Join-Path $repoRoot "src\Migration.Admin.Api\Endpoints"

if (-not (Test-Path $endpointRoot)) {
    throw "Could not find $endpointRoot"
}

Write-Host "Checking for operational endpoint files still sitting directly under Endpoints..."

$rootOperationalEndpointFiles = Get-ChildItem -Path $endpointRoot -File -Filter "Operational*EndpointExtensions.cs" |
    Sort-Object Name

if ($rootOperationalEndpointFiles.Count -gt 0) {
    Write-Host ""
    Write-Host "These operational endpoint files should be moved under Endpoints\Operational\..."
    $rootOperationalEndpointFiles | ForEach-Object {
        Write-Host " - $($_.FullName)"
    }

    throw "Operational endpoint folder cleanup verification failed."
}

Write-Host "No root-level operational endpoint extension files found."

Write-Host ""
Write-Host "Current operational endpoint layout:"
Get-ChildItem -Path (Join-Path $endpointRoot "Operational") -Recurse -File -Filter "*.cs" |
    Sort-Object FullName |
    ForEach-Object {
        $relative = $_.FullName.Substring($repoRoot.Length + 1)
        Write-Host " - $relative"
    }
