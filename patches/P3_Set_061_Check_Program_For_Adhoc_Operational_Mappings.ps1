$repoRoot = (Resolve-Path ".").Path
$programPath = Join-Path $repoRoot "src\Migration.Admin.Api\Program.cs"

if (-not (Test-Path $programPath)) {
    throw "Could not find $programPath"
}

$content = Get-Content $programPath -Raw

Write-Host "Checking Program.cs for direct operational endpoint mappings..."

$matches = Select-String `
    -Path $programPath `
    -Pattern "MapOperational[A-Za-z0-9_]*Endpoints\(" `
    -AllMatches

if (-not $matches) {
    Write-Host "No direct operational endpoint mappings found in Program.cs."
    exit 0
}

$allowed = @(
    "MapOperationalHealthEndpoints"
)

$violations = @()

foreach ($match in $matches.Matches) {
    $methodName = $match.Value.TrimEnd("(")

    if ($allowed -notcontains $methodName) {
        $violations += $methodName
    }
}

if ($violations.Count -eq 0) {
    Write-Host "Only allowed app-level operational mappings found in Program.cs."
    Write-Host "Allowed:"
    $allowed | ForEach-Object { Write-Host " - $_" }
    exit 0
}

Write-Host ""
Write-Host "Unexpected direct operational endpoint mappings found in Program.cs:"
$violations | Sort-Object -Unique | ForEach-Object {
    Write-Host " - $_"
}

throw "Program.cs has ad hoc operational endpoint mappings that should be moved into AdminApiEndpointStartupExtensions."
