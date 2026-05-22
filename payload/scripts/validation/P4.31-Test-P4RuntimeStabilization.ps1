[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host ("[P4.31] {0}" -f $Message)
}

function Assert-File {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("Expected file not found: {0}" -f $Path)
    }
}

function Assert-Contains {
    param(
        [string]$Path,
        [string]$Text
    )

    Assert-File -Path $Path

    $content = Get-Content -LiteralPath $Path -Raw
    if (-not $content.Contains($Text)) {
        throw ("Expected text not found in {0}: {1}" -f $Path, $Text)
    }
}

function Assert-OccursOnce {
    param(
        [string]$Path,
        [string]$Text
    )

    Assert-File -Path $Path

    $content = Get-Content -LiteralPath $Path -Raw
    $count = ([regex]::Matches($content, [regex]::Escape($Text))).Count

    if ($count -ne 1) {
        throw ("Expected text to occur once in {0}; found {1}: {2}" -f $Path, $count, $Text)
    }
}

function Assert-NotContains {
    param(
        [string]$Path,
        [string]$Text
    )

    Assert-File -Path $Path

    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.Contains($Text)) {
        throw ("Unexpected text found in {0}: {1}" -f $Path, $Text)
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path

Write-Step ("Repo root: {0}" -f $repoRoot)

$appPath = Join-Path $repoRoot "apps/migration-admin-ui/src/App.tsx"
$uiCompositionPath = Join-Path $repoRoot "apps/migration-admin-ui/src/features/operational/OperationalWorkspaceComposition.tsx"
$apiCompositionPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/MigrationOperationalEndpointCompositionExtensions.cs"
$programPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Program.cs"
$packagesPath = Join-Path $repoRoot "Directory.Packages.props"

Write-Step "Checking UI composition"
Assert-OccursOnce -Path $appPath -Text "import { OperationalWorkspaceComposition } from './features/operational/OperationalWorkspaceComposition';"
Assert-OccursOnce -Path $appPath -Text "<OperationalWorkspaceComposition />"
Assert-NotContains -Path $appPath -Text "<WorkerTelemetryWorkspace />"
Assert-NotContains -Path $appPath -Text "<ConnectorConfigurationWorkspace />"
Assert-Contains -Path $uiCompositionPath -Text "export function OperationalWorkspaceComposition()"
Assert-Contains -Path $uiCompositionPath -Text "<CommandCenterSummaryWorkspace />"

Write-Step "Checking Admin API operational endpoint composition"
Assert-OccursOnce -Path $programPath -Text "app.MapMigrationOperationalEndpoints();"
Assert-Contains -Path $apiCompositionPath -Text "MapMigrationOperationalEndpoints"
Assert-Contains -Path $apiCompositionPath -Text "endpoints.MapOperationalCommandCenterEndpoints();"
Assert-Contains -Path $apiCompositionPath -Text "endpoints.MapOperationalCostAnalyticsEndpoints();"

Write-Step "Checking central package hygiene"
Assert-File -Path $packagesPath
$csprojFiles = Get-ChildItem -Path $repoRoot -Filter "*.csproj" -Recurse -File |
    Where-Object {
        $_.FullName -notmatch "\\bin\\" -and
        $_.FullName -notmatch "\\obj\\"
    }

foreach ($csproj in $csprojFiles) {
    [xml]$project = Get-Content -LiteralPath $csproj.FullName -Raw
    foreach ($itemGroup in @($project.Project.ItemGroup)) {
        if ($itemGroup.PSObject.Properties.Name -contains "PackageReference") {
            foreach ($reference in @($itemGroup.PackageReference)) {
                if ($reference.PSObject.Properties.Name -contains "Version") {
                    throw ("Inline PackageReference Version found in {0}" -f $csproj.FullName)
                }
            }
        }
    }
}

Write-Step "Checking payload residue"
$payloadPath = Join-Path $repoRoot "payload"
if (Test-Path -LiteralPath $payloadPath) {
    throw ("Payload folder should not remain in repo root after applying drop-ins: {0}" -f $payloadPath)
}

Write-Step "Checking P4.30 local profile"
Assert-Contains -Path (Join-Path $repoRoot "scripts/dev/Start-MigrationAdminLocal.ps1") -Text "VITE_ADMIN_API_BASE_URL"
Assert-Contains -Path (Join-Path $repoRoot "docs/development/P4.30-local-developer-run-profile.md") -Text "http://localhost:5174"

Write-Step "P4 runtime stabilization checks passed."
