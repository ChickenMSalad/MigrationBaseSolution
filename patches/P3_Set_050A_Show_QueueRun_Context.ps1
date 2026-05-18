$repoRoot = (Resolve-Path ".").Path
$runEndpointPath = Join-Path $repoRoot "src\Migration.Admin.Api\Endpoints\Runs\RunEndpointExtensions.cs"

if (-not (Test-Path $runEndpointPath)) {
    throw "Could not find $runEndpointPath"
}

$lines = Get-Content $runEndpointPath

for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match "queue\.EnqueueAsync\(run") {
        $start = [Math]::Max(0, $i - 12)
        $end = [Math]::Min($lines.Count - 1, $i + 12)

        Write-Host "Found queue.EnqueueAsync at line $($i + 1):"
        for ($j = $start; $j -le $end; $j++) {
            "{0,5}: {1}" -f ($j + 1), $lines[$j]
        }
    }
}
